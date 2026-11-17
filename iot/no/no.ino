// Nó do corredor do Corredor Aceso (ESP32).
//
// A pessoa passa no corredor, o PIR acende a luz de caminho e o nó manda MOVIMENTO.
// Esse movimento é o "estou bem" que ninguém precisa lembrar de dar.
// Contrato MQTT: seção 6.2 de docs/arquitetura/03-modelo-de-dominio.md.
//
// - A luz acende sem depender da rede.
// - Todo evento entra num buffer. Com rede, sai na hora. Sem rede, espera e sai depois com o
//   seq e o ts originais e "bufferizado": true.
// - HEARTBEAT a cada 20 s, só com rede: ele diz que o aparelho está ligado agora.
// - TELEMETRIA ao ligar, a cada 5 min e na hora em que a tensão cruza 100 V.
// - Botão: um toque "estou bem" (BOTAO_BEM), dois toques "hoje vou sair" (BOTAO_SAIR).
// - Comandos: DIA_CRITICO acende o LED âmbar. PEDIR_TOQUE toca o buzzer até o botão.
// - O pacote leva só o token da casa. Nunca nome.

#include <WiFi.h>
#include <PubSubClient.h>
#include <DHTesp.h>
#include <time.h>
#include "config.h"

// Pinos (ver diagram.json).
const int PINO_PIR = 27;
const int PINO_LUZ = 26;       // LED branco: luz de caminho
const int PINO_DHT = 15;       // DHT22: temperatura dentro da casa
const int PINO_TENSAO = 34;    // potenciômetro no lugar do sensor de tensão da tomada
const int PINO_BOTAO = 25;     // botão grande, com pull-up interno
const int PINO_AMBAR = 33;     // LED âmbar: dia crítico e pedido de toque
const int PINO_BUZZER = 32;
const int PINO_WIFI = 14;      // chave da demo: desligada corta o Wi-Fi de verdade

// Tempos, em milissegundos.
const unsigned long INTERVALO_HEARTBEAT = 20000;
const unsigned long INTERVALO_TELEMETRIA = 5UL * 60 * 1000;
const unsigned long LUZ_ACESA = 30000;
const unsigned long MOVIMENTO_MINIMO = 30000;  // um MOVIMENTO por passagem, não um por segundo
const unsigned long DUPLO_TOQUE = 500;
const unsigned long DEBOUNCE = 30;
const unsigned long LEITURA_DHT = 2500;
const unsigned long RECONEXAO = 5000;
const unsigned long DURACAO_DIA_CRITICO = 12UL * 60 * 60 * 1000;
const unsigned long DURACAO_TOQUE = 10UL * 60 * 1000;

const float TENSAO_MINIMA = 100.0;
const float TENSAO_MAXIMA_ESCALA = 250.0;  // potenciômetro no fim da escala
const time_t HORA_VALIDA = 1700000000;     // antes disso o NTP ainda não respondeu

// Buffer de eventos. Cheio, descarta o mais antigo e avisa no serial.
struct Evento {
  uint32_t seq;
  time_t ts;           // 0 enquanto o NTP não respondeu
  unsigned long ms;    // millis() do fato, para calcular o ts depois
  const char *tipo;
  float tempC;
  float tensaoV;
  bool guardado;       // ficou no nó sem rede: sai com "bufferizado": true
};
const int CAPACIDADE = 64;
Evento buffer[CAPACIDADE];
int inicioBuffer = 0;
int tamanhoBuffer = 0;

WiFiClient rede;
PubSubClient mqtt(rede);
DHTesp dht;

char topicoEvento[96];
char topicoComando[96];
uint32_t seq = 0;
float tempC = NAN;
float tensaoV = 0;
bool tensaoBaixa = false;
bool wifiLigado = true;

unsigned long ultimoHeartbeat = 0;
unsigned long ultimaTelemetria = 0;
bool telemetriaInicial = false;
unsigned long ultimaLeituraDht = 0;
unsigned long ultimaTentativa = 0;
unsigned long luzAte = 0;
unsigned long ultimoMovimento = 0;
bool pirAntes = false;
bool houveMovimento = false;

bool botaoAntes = HIGH;
unsigned long botaoMudouEm = 0;
bool botaoEstavel = HIGH;
int toques = 0;
unsigned long ultimoToqueSoltoEm = 0;

bool diaCritico = false;
unsigned long diaCriticoEm = 0;
bool pedindoToque = false;
unsigned long toquePedidoEm = 0;

bool horaValida() { return time(nullptr) > HORA_VALIDA; }

void bipar(int vezes) {
  for (int i = 0; i < vezes; i++) {
    tone(PINO_BUZZER, 1760, 80);
    delay(140);
  }
}

// ---------- buffer ----------

void guardar(const char *tipo) {
  if (tamanhoBuffer == CAPACIDADE) {
    Serial.printf("[buffer] cheio: descartado seq=%lu\n", (unsigned long)buffer[inicioBuffer].seq);
    inicioBuffer = (inicioBuffer + 1) % CAPACIDADE;
    tamanhoBuffer--;
  }
  Evento &e = buffer[(inicioBuffer + tamanhoBuffer) % CAPACIDADE];
  e.seq = ++seq;
  e.ts = horaValida() ? time(nullptr) : 0;
  e.ms = millis();
  e.tipo = tipo;
  e.tempC = tempC;
  e.tensaoV = tensaoV;
  e.guardado = !mqtt.connected();
  tamanhoBuffer++;
  if (e.guardado) {
    Serial.printf("[evento] seq=%lu %s sem rede: guardado (%d no buffer)\n", (unsigned long)e.seq, tipo, tamanhoBuffer);
  }
}

void numero(char *destino, size_t tamanho, float valor) {
  if (isnan(valor)) snprintf(destino, tamanho, "null");
  else snprintf(destino, tamanho, "%.1f", valor);
}

bool publicar(uint32_t s, time_t ts, const char *tipo, float t, float v, bool bufferizado) {
  char quando[24], temp[12], tensao[12], corpo[200];
  strftime(quando, sizeof quando, "%Y-%m-%dT%H:%M:%SZ", gmtime(&ts));
  numero(temp, sizeof temp, t);
  numero(tensao, sizeof tensao, v);
  snprintf(corpo, sizeof corpo,
           "{\"seq\":%lu,\"ts\":\"%s\",\"tipo\":\"%s\",\"tempC\":%s,\"tensaoV\":%s,\"bufferizado\":%s}",
           (unsigned long)s, quando, tipo, temp, tensao, bufferizado ? "true" : "false");
  bool ok = mqtt.publish(topicoEvento, corpo);
  if (ok && strcmp(tipo, "HEARTBEAT") != 0) Serial.printf("[mqtt] %s\n", corpo);
  return ok;
}

// Manda o buffer em ordem. Para no primeiro envio que falha e marca o resto como guardado.
void esvaziarBuffer() {
  if (tamanhoBuffer > 0 && !horaValida()) return;  // sem hora, nenhum pacote sai
  int reenviados = 0;
  while (tamanhoBuffer > 0 && mqtt.connected()) {
    Evento &e = buffer[inicioBuffer];
    if (e.ts == 0) e.ts = time(nullptr) - (millis() - e.ms) / 1000;
    if (!publicar(e.seq, e.ts, e.tipo, e.tempC, e.tensaoV, e.guardado)) {
      e.guardado = true;
      break;
    }
    if (e.guardado) reenviados++;
    inicioBuffer = (inicioBuffer + 1) % CAPACIDADE;
    tamanhoBuffer--;
  }
  if (reenviados > 0) Serial.printf("[buffer] %d evento(s) reenviados com a hora original\n", reenviados);
  for (int i = 0; i < tamanhoBuffer; i++) buffer[(inicioBuffer + i) % CAPACIDADE].guardado = true;
}

// ---------- rede ----------

void aoReceber(char *topico, byte *corpo, unsigned int tamanho) {
  char texto[64];
  unsigned int n = tamanho < sizeof texto - 1 ? tamanho : sizeof texto - 1;
  memcpy(texto, corpo, n);
  texto[n] = '\0';
  if (strstr(texto, "\"DIA_CRITICO\"")) {
    diaCritico = true;
    diaCriticoEm = millis();
    Serial.println("[comando] DIA_CRITICO: LED âmbar aceso");
  } else if (strstr(texto, "\"PEDIR_TOQUE\"")) {
    pedindoToque = true;
    toquePedidoEm = millis();
    Serial.println("[comando] PEDIR_TOQUE: buzzer até alguém apertar o botão");
  }
}

void cuidarDaRede() {
  bool chave = digitalRead(PINO_WIFI) == LOW;  // chave ligada fecha para o GND
  if (chave != wifiLigado) {
    wifiLigado = chave;
    if (!wifiLigado) {
      Serial.println("[wifi] chave desligada: Wi-Fi cortado. Eventos vão para o buffer.");
      mqtt.disconnect();
      WiFi.disconnect(true);
      WiFi.mode(WIFI_OFF);
    } else {
      Serial.println("[wifi] chave ligada: reconectando");
      ultimaTentativa = 0;
    }
  }
  if (!wifiLigado) return;

  unsigned long agora = millis();
  if (WiFi.status() != WL_CONNECTED) {
    if (agora - ultimaTentativa >= RECONEXAO || ultimaTentativa == 0) {
      ultimaTentativa = agora;
      WiFi.mode(WIFI_STA);
      WiFi.begin(WIFI_SSID, WIFI_SENHA, WIFI_CANAL);
      Serial.println("[wifi] conectando");
    }
    return;
  }
  if (!mqtt.connected()) {
    if (agora - ultimaTentativa < RECONEXAO && ultimaTentativa != 0) return;
    ultimaTentativa = agora;
    char cliente[40];
    snprintf(cliente, sizeof cliente, "corredor-no-%08lx", (unsigned long)(uint32_t)ESP.getEfuseMac());
    if (mqtt.connect(cliente)) {
      mqtt.subscribe(topicoComando);
      Serial.printf("[mqtt] conectado em %s, %d evento(s) no buffer\n", MQTT_HOST, tamanhoBuffer);
    } else {
      Serial.printf("[mqtt] falhou (%d)\n", mqtt.state());
      return;
    }
  }
  mqtt.loop();
  esvaziarBuffer();
  if (horaValida() && agora - ultimoHeartbeat >= INTERVALO_HEARTBEAT) {
    ultimoHeartbeat = agora;
    publicar(++seq, time(nullptr), "HEARTBEAT", tempC, tensaoV, false);
  }
}

// ---------- sensores e atuadores ----------

void cuidarDoCorredor() {
  unsigned long agora = millis();
  bool pir = digitalRead(PINO_PIR) == HIGH;
  if (pir) luzAte = agora + LUZ_ACESA;
  if (pir && !pirAntes && (!houveMovimento || agora - ultimoMovimento >= MOVIMENTO_MINIMO)) {
    houveMovimento = true;
    ultimoMovimento = agora;
    guardar("MOVIMENTO");
  }
  pirAntes = pir;
  digitalWrite(PINO_LUZ, (long)(luzAte - agora) > 0 ? HIGH : LOW);
}

void cuidarDoBotao() {
  unsigned long agora = millis();
  bool leitura = digitalRead(PINO_BOTAO);
  if (leitura != botaoAntes) botaoMudouEm = agora;
  botaoAntes = leitura;
  if (agora - botaoMudouEm > DEBOUNCE && leitura != botaoEstavel) {
    botaoEstavel = leitura;
    if (botaoEstavel == HIGH) {  // soltou
      toques++;
      ultimoToqueSoltoEm = agora;
      if (pedindoToque) {
        pedindoToque = false;
        noTone(PINO_BUZZER);
      }
    }
  }
  if (toques >= 2) {
    toques = 0;
    guardar("BOTAO_SAIR");
    bipar(2);
  } else if (toques == 1 && agora - ultimoToqueSoltoEm > DUPLO_TOQUE) {
    toques = 0;
    guardar("BOTAO_BEM");
    bipar(1);
  }
}

void cuidarDaTelemetria() {
  unsigned long agora = millis();
  if (agora - ultimaLeituraDht >= LEITURA_DHT || ultimaLeituraDht == 0) {
    ultimaLeituraDht = agora;
    TempAndHumidity leitura = dht.getTempAndHumidity();
    if (dht.getStatus() == DHTesp::ERROR_NONE) tempC = leitura.temperature;
  }
  tensaoV = analogRead(PINO_TENSAO) * TENSAO_MAXIMA_ESCALA / 4095.0;
  bool baixa = tensaoV < TENSAO_MINIMA;
  bool cruzou = baixa != tensaoBaixa;
  tensaoBaixa = baixa;
  bool vencida = agora - ultimaTelemetria >= INTERVALO_TELEMETRIA;
  // A primeira espera a rede subir, ou 30 s, para não sair marcada como guardada a cada boot.
  bool primeira = !telemetriaInicial && !isnan(tempC) && (mqtt.connected() || agora > 30000);
  if (primeira || cruzou || (telemetriaInicial && vencida)) {
    if (cruzou) Serial.printf("[energia] tensão cruzou 100 V: %.1f V\n", tensaoV);
    telemetriaInicial = true;
    ultimaTelemetria = agora;
    guardar("TELEMETRIA");
  }
}

void cuidarDosAvisos() {
  unsigned long agora = millis();
  if (diaCritico && agora - diaCriticoEm >= DURACAO_DIA_CRITICO) diaCritico = false;
  if (pedindoToque && agora - toquePedidoEm >= DURACAO_TOQUE) {
    pedindoToque = false;
    noTone(PINO_BUZZER);
  }
  bool piscar = (agora / 500) % 2 == 0;
  if (pedindoToque) {
    digitalWrite(PINO_AMBAR, piscar ? HIGH : LOW);
    if (agora % 2000 < 20) tone(PINO_BUZZER, 880, 300);
  } else {
    digitalWrite(PINO_AMBAR, diaCritico ? HIGH : LOW);
  }
}

void setup() {
  Serial.begin(115200);
  pinMode(PINO_PIR, INPUT);
  pinMode(PINO_LUZ, OUTPUT);
  pinMode(PINO_BOTAO, INPUT_PULLUP);
  pinMode(PINO_AMBAR, OUTPUT);
  pinMode(PINO_BUZZER, OUTPUT);
  pinMode(PINO_WIFI, INPUT_PULLUP);
  dht.setup(PINO_DHT, DHTesp::DHT22);
  snprintf(topicoEvento, sizeof topicoEvento, "%s/corredor/v1/%s/evento", MQTT_PREFIXO, CASA_TOKEN);
  snprintf(topicoComando, sizeof topicoComando, "%s/corredor/v1/%s/comando", MQTT_PREFIXO, CASA_TOKEN);
  mqtt.setServer(MQTT_HOST, MQTT_PORTA);
  mqtt.setCallback(aoReceber);
  mqtt.setBufferSize(512);
  mqtt.setSocketTimeout(5);
  configTime(0, 0, NTP_1, NTP_2);
  wifiLigado = digitalRead(PINO_WIFI) == LOW;
  Serial.printf("Corredor Aceso: nó da casa %.6s... publica em %s\n", CASA_TOKEN, topicoEvento);
}

void loop() {
  cuidarDoCorredor();
  cuidarDoBotao();
  cuidarDaTelemetria();
  cuidarDosAvisos();
  cuidarDaRede();
  delay(10);
}
