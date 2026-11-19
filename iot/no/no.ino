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
// - Botão: cada toque é um "estou bem" (BOTAO_BEM) e silencia o buzzer.
// - Comandos: DIA_CRITICO acende o LED âmbar. PEDIR_TOQUE toca o buzzer até o botão.
// - Reações locais, sem servidor: 34 °C ou mais acende o âmbar; tensão abaixo de 100 V dispara o
//   alarme até a tensão voltar ou o botão; às 14h30, sem sinal de vida desde as 10h, o buzzer chama.
// - Queda: impacto no pingente (MPU6050) ou alguém deitado no chão sob o sensor de teto (HC-SR04).
//   A tela mostra "QUEDA?" e o buzzer bipa. Sem toque no botão, sai um evento QUEDA. O Motor
//   da API não conhece QUEDA e recusa o pacote; o painel web mostra.
// - Tela OLED com o recado do momento, em letras grandes.
// - Tópico estado: uma foto dos sensores a cada 2 s, retida no broker, para o painel web.
// - O pacote leva só o token da casa. Nunca nome.

#include <WiFi.h>
#include <Wire.h>
#include <PubSubClient.h>
#include <DHTesp.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <time.h>
#include "config.h"

// Padrões para um config.h antigo, sem as reações locais.
#ifndef FUSO_SEGUNDOS
#define FUSO_SEGUNDOS (-3 * 3600)
#endif
#ifndef PEDIDO_LOCAL_DE_TOQUE
#define PEDIDO_LOCAL_DE_TOQUE 1
#endif
#ifndef INICIO_DA_JANELA_MIN
#define INICIO_DA_JANELA_MIN (10 * 60)
#endif
#ifndef HORA_DO_PEDIDO_MIN
#define HORA_DO_PEDIDO_MIN (14 * 60 + 30)
#endif
#ifndef SILENCIO_DEMO_S
#define SILENCIO_DEMO_S 0
#endif
#ifndef IMPACTO_G
#define IMPACTO_G 2.0
#endif
#ifndef ALTURA_DO_TETO_CM
#define ALTURA_DO_TETO_CM 250
#endif
#ifndef ALTURA_DEITADO_CM
#define ALTURA_DEITADO_CM 50
#endif
#ifndef TEMPO_NO_CHAO_S
#define TEMPO_NO_CHAO_S 30
#endif
#ifndef CONTAGEM_DA_QUEDA_S
#define CONTAGEM_DA_QUEDA_S 15
#endif

// Pinos (ver diagram.json).
const int PINO_PIR = 27;
const int PINO_LUZ = 26;       // LED branco: luz de caminho
const int PINO_DHT = 15;       // DHT22: temperatura dentro da casa
const int PINO_TENSAO = 34;    // potenciômetro no lugar do sensor de tensão da tomada
const int PINO_BOTAO = 25;     // botão grande, com pull-up interno
const int PINO_AMBAR = 33;     // LED âmbar: dia crítico e pedido de toque
const int PINO_BUZZER = 32;
const int PINO_WIFI = 14;      // chave da demo: desligada corta o Wi-Fi de verdade
const int PINO_TRIG = 5;       // HC-SR04 no teto
const int PINO_ECHO = 18;      // no aparelho real, com divisor de tensão: o eco sai em 5 V
// OLED (0x3C) e MPU6050 (0x68) no I2C padrão: SDA 21, SCL 22.
const uint8_t ENDERECO_TELA = 0x3C;
const uint8_t ENDERECO_MPU = 0x68;

// Tempos, em milissegundos.
const unsigned long INTERVALO_HEARTBEAT = 20000;
const unsigned long INTERVALO_TELEMETRIA = 5UL * 60 * 1000;
const unsigned long LUZ_ACESA = 30000;
const unsigned long MOVIMENTO_MINIMO = 30000;  // um MOVIMENTO por passagem, não um por segundo
const unsigned long DEBOUNCE = 30;
const unsigned long LEITURA_DHT = 2500;
const unsigned long RECONEXAO = 5000;
const unsigned long ESPERA_MAXIMA_MQTT = 60000;
const unsigned long DURACAO_DIA_CRITICO = 12UL * 60 * 60 * 1000;
const unsigned long DURACAO_TOQUE = 10UL * 60 * 1000;
const unsigned long INTERVALO_ESTADO = 2000;
const unsigned long INTERVALO_TELA = 500;
const unsigned long INTERVALO_DISTANCIA = 300;
const unsigned long INTERVALO_MPU = 100;

const float TEMPERATURA_DE_CALOR = 34.0;  // a mesma do Motor
const float FIM_DO_CALOR = 33.5;          // histerese: o âmbar não pisca na beira dos 34 °C
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
Adafruit_SSD1306 tela(128, 64, &Wire, -1);
bool telaOk = false;
bool mpuOk = false;

char topicoEvento[96];
char topicoComando[96];
char topicoEstado[96];
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
unsigned long esperaMqtt = RECONEXAO;
unsigned long luzAte = 0;
unsigned long ultimoMovimento = 0;
bool pirAntes = false;
bool houveMovimento = false;

bool botaoAntes = HIGH;
unsigned long botaoMudouEm = 0;
bool botaoEstavel = HIGH;

bool diaCritico = false;
unsigned long diaCriticoEm = 0;
bool pedindoToque = false;
unsigned long toquePedidoEm = 0;

// Reações locais.
bool calor = false;
bool alarmeDeEnergia = false;   // tensão baixa e ninguém apertou o botão ainda
bool houveSinal = false;        // MOVIMENTO ou BOTAO_BEM desde o boot
unsigned long ultimoSinalEm = 0;
int diaDoPedidoLocal = -1;      // tm_yday do último pedido local: um por dia
bool pedidoDeDemoFeito = false;

// Queda.
enum EstadoDaQueda { SEM_QUEDA, PERGUNTANDO, QUEDA_AVISADA };
EstadoDaQueda queda = SEM_QUEDA;
unsigned long perguntaEm = 0;
const char *origemDaQueda = "";
float aceleracaoG = NAN;
bool impactoAntes = false;
unsigned long ultimaLeituraMpu = 0;
int alturaCm = -1;              // altura do que está sob o sensor de teto; -1 sem leitura
unsigned long ultimaDistancia = 0;
bool noChao = false;
unsigned long noChaoDesde = 0;

unsigned long ultimoEstado = 0;
unsigned long ultimaTela = 0;

bool horaValida() { return time(nullptr) > HORA_VALIDA; }

void registrarSinal() {
  houveSinal = true;
  ultimoSinalEm = millis();
  pedidoDeDemoFeito = false;
}

// O buzzer anda pelo LEDC: frequência para tocar, frequência 0 para calar. O noTone() da Arduino
// não deixa o pino quieto no Wokwi, e o bipe virava um som contínuo.
void somLigar(unsigned int frequencia) { ledcWriteTone(PINO_BUZZER, frequencia); }
void somDesligar() { ledcWriteTone(PINO_BUZZER, 0); }

void bipar(int vezes) {
  for (int i = 0; i < vezes; i++) {
    somLigar(1760);
    delay(80);
    somDesligar();
    delay(60);
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
      esperaMqtt = RECONEXAO;
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
    if (agora - ultimaTentativa < esperaMqtt && ultimaTentativa != 0) return;
    ultimaTentativa = agora;
    char cliente[40];
    snprintf(cliente, sizeof cliente, "corredor-no-%08lx", (unsigned long)(uint32_t)ESP.getEfuseMac());
    if (mqtt.connect(cliente)) {
      esperaMqtt = RECONEXAO;
      mqtt.subscribe(topicoComando);
      Serial.printf("[mqtt] conectado em %s, %d evento(s) no buffer\n", MQTT_HOST, tamanhoBuffer);
    } else {
      // Cada tentativa trava o loop alguns segundos. Espaçar as tentativas mantém o botão e os
      // sensores atendidos enquanto o broker não responde.
      esperaMqtt = esperaMqtt * 2 > ESPERA_MAXIMA_MQTT ? ESPERA_MAXIMA_MQTT : esperaMqtt * 2;
      Serial.printf("[mqtt] falhou (%d), nova tentativa em %lu s\n", mqtt.state(), esperaMqtt / 1000);
      return;
    }
  }
  mqtt.loop();
  esvaziarBuffer();
  if (horaValida() && agora - ultimoHeartbeat >= INTERVALO_HEARTBEAT) {
    ultimoHeartbeat = agora;
    publicar(++seq, time(nullptr), "HEARTBEAT", tempC, tensaoV, false);
  }
  publicarEstado();
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
    registrarSinal();
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
      if (pedindoToque) {
        pedindoToque = false;
        silenciar();
      }
      if (alarmeDeEnergia) {
        alarmeDeEnergia = false;
        silenciar();
        Serial.println("[energia] alarme silenciado pelo botão");
      }
      if (queda != SEM_QUEDA) {
        Serial.println(queda == PERGUNTANDO ? "[queda] cancelada pelo botão" : "[queda] encerrada pelo botão");
        queda = SEM_QUEDA;
        noChaoDesde = agora;  // se continuar no chão, a contagem recomeça
        silenciar();
      }
      guardar("BOTAO_BEM");
      registrarSinal();
      bipar(1);
    }
  }
}

void cuidarDaTelemetria() {
  unsigned long agora = millis();
  if (agora - ultimaLeituraDht >= LEITURA_DHT || ultimaLeituraDht == 0) {
    ultimaLeituraDht = agora;
    TempAndHumidity leitura = dht.getTempAndHumidity();
    if (dht.getStatus() == DHTesp::ERROR_NONE) tempC = leitura.temperature;
    if (!isnan(tempC) && !calor && tempC >= TEMPERATURA_DE_CALOR) {
      calor = true;
      Serial.printf("[calor] %.1f °C dentro de casa: muito quente, âmbar aceso\n", tempC);
      bipar(3);
    } else if (!isnan(tempC) && calor && tempC < FIM_DO_CALOR) {
      calor = false;
      Serial.printf("[calor] %.1f °C: âmbar do calor apagado\n", tempC);
    }
  }
  tensaoV = analogRead(PINO_TENSAO) * TENSAO_MAXIMA_ESCALA / 4095.0;
  bool baixa = tensaoV < TENSAO_MINIMA;
  bool cruzou = baixa != tensaoBaixa;
  tensaoBaixa = baixa;
  bool vencida = agora - ultimaTelemetria >= INTERVALO_TELEMETRIA;
  // A primeira espera a rede subir, ou 30 s, para não sair marcada como guardada a cada boot.
  bool primeira = !telemetriaInicial && !isnan(tempC) && (mqtt.connected() || agora > 30000);
  if (primeira || cruzou || (telemetriaInicial && vencida)) {
    if (cruzou) {
      Serial.printf("[energia] tensão cruzou 100 V: %.1f V\n", tensaoV);
      alarmeDeEnergia = baixa;
      if (baixa) Serial.println("[energia] alarme local até a tensão voltar ou alguém apertar o botão");
      else silenciar();
    }
    telemetriaInicial = true;
    ultimaTelemetria = agora;
    guardar("TELEMETRIA");
  }
}

// Bipes curtos, "bip bip bip", nunca um som contínuo. O relógio decide se o buzzer está ligado em cada
// instante, então o padrão não depende do tempo de cada volta do loop.
bool buzzerLigado = false;

// O buzzer do Wokwi em modo "smooth" soa limpo, mas segura o som um pouco depois que o sinal para.
// Por isso o bipe é curto e o silêncio entre bipes é longo.
const unsigned long BIPE_MS = 150;
const unsigned long ENTRE_BIPES_MS = 650;

void bipes(unsigned long agora, unsigned long periodo, int quantos, unsigned int frequencia) {
  const unsigned long passo = BIPE_MS + ENTRE_BIPES_MS;
  unsigned long fase = agora % periodo;
  bool ligar = fase < (unsigned long)quantos * passo && fase % passo < BIPE_MS;
  if (ligar && !buzzerLigado) somLigar(frequencia);
  if (!ligar && buzzerLigado) somDesligar();
  buzzerLigado = ligar;
}

void silenciar() {
  if (buzzerLigado) somDesligar();
  buzzerLigado = false;
}

void cuidarDosAvisos() {
  unsigned long agora = millis();
  if (diaCritico && agora - diaCriticoEm >= DURACAO_DIA_CRITICO) diaCritico = false;
  if (pedindoToque && agora - toquePedidoEm >= DURACAO_TOQUE) pedindoToque = false;
  bool piscar = (agora / 500) % 2 == 0;
  if (queda == QUEDA_AVISADA) {
    digitalWrite(PINO_AMBAR, (agora / 150) % 2 == 0 ? HIGH : LOW);
    bipes(agora, 3200, 3, 2600);
  } else if (queda == PERGUNTANDO) {
    digitalWrite(PINO_AMBAR, piscar ? HIGH : LOW);
    bipes(agora, 1600, 1, 1500);
  } else if (alarmeDeEnergia) {
    digitalWrite(PINO_AMBAR, (agora / 150) % 2 == 0 ? HIGH : LOW);
    bipes(agora, 3200, 2, 2200);
  } else if (pedindoToque) {
    digitalWrite(PINO_AMBAR, piscar ? HIGH : LOW);
    bipes(agora, 3200, 2, 880);
  } else {
    silenciar();
    digitalWrite(PINO_AMBAR, diaCritico || calor ? HIGH : LOW);
  }
}

void pedirToqueLocal(const char *motivo) {
  if (pedindoToque) return;  // o servidor já pediu
  pedindoToque = true;
  toquePedidoEm = millis();
  Serial.printf("[silêncio] %s: buzzer até alguém apertar o botão\n", motivo);
}

// A mesma regra do VigiaDoRadar, decidida no aparelho. Só conclui "silêncio desde as 10h" se o
// aparelho estava ligado desde as 10h, e decide uma vez por dia, no começo da janela de 14h30.
void cuidarDoSilencio() {
  if (!PEDIDO_LOCAL_DE_TOQUE) return;
  unsigned long agora = millis();
  if (SILENCIO_DEMO_S > 0) {
    unsigned long desde = houveSinal ? ultimoSinalEm : 0;
    if (!pedidoDeDemoFeito && agora - desde >= SILENCIO_DEMO_S * 1000UL) {
      pedidoDeDemoFeito = true;
      char motivo[48];
      snprintf(motivo, sizeof motivo, "demo: %d s sem sinal de vida", (int)SILENCIO_DEMO_S);
      pedirToqueLocal(motivo);
    }
    return;
  }
  if (!horaValida()) return;
  time_t local = time(nullptr) + FUSO_SEGUNDOS;
  struct tm t;
  gmtime_r(&local, &t);
  long segundosNoDia = t.tm_hour * 3600L + t.tm_min * 60L + t.tm_sec;
  long inicioDoPedido = HORA_DO_PEDIDO_MIN * 60L;
  if (t.tm_yday == diaDoPedidoLocal) return;
  if (segundosNoDia < inicioDoPedido || segundosNoDia >= inicioDoPedido + (long)(DURACAO_TOQUE / 1000)) return;
  diaDoPedidoLocal = t.tm_yday;
  unsigned long desdeOInicio = (unsigned long)(segundosNoDia - INICIO_DA_JANELA_MIN * 60L) * 1000UL;
  if (agora < desdeOInicio) return;  // ligou depois das 10h: não sabe o que houve antes
  if (houveSinal && agora - ultimoSinalEm <= desdeOInicio) return;
  pedirToqueLocal("14h30 sem sinal de vida desde as 10h");
}

// ---------- queda ----------

void iniciarMpu() {
  Wire.beginTransmission(ENDERECO_MPU);
  Wire.write(0x6B);  // PWR_MGMT_1: acorda o sensor
  Wire.write(0);
  mpuOk = Wire.endTransmission() == 0;
  if (!mpuOk) return;
  Wire.beginTransmission(ENDERECO_MPU);
  Wire.write(0x1C);  // ACCEL_CONFIG: escala de 16 g, para enxergar o impacto
  Wire.write(0x18);
  Wire.endTransmission();
}

// Módulo da aceleração, em g. Parado, vale 1 g (a gravidade).
float lerAceleracaoG() {
  Wire.beginTransmission(ENDERECO_MPU);
  Wire.write(0x3B);
  if (Wire.endTransmission(false) != 0 || Wire.requestFrom(ENDERECO_MPU, (uint8_t)6) != 6) return NAN;
  float soma = 0;
  for (int i = 0; i < 3; i++) {
    int16_t bruto = (int16_t)((Wire.read() << 8) | Wire.read());
    float g = bruto / 2048.0;
    soma += g * g;
  }
  return sqrt(soma);
}

// Altura do que está sob o sensor de teto: o teto menos a distância medida. -1 sem eco.
int medirAlturaCm() {
  digitalWrite(PINO_TRIG, LOW);
  delayMicroseconds(2);
  digitalWrite(PINO_TRIG, HIGH);
  delayMicroseconds(10);
  digitalWrite(PINO_TRIG, LOW);
  unsigned long eco = pulseIn(PINO_ECHO, HIGH, 30000);
  if (eco == 0) return -1;
  int distancia = eco / 58;
  return distancia >= ALTURA_DO_TETO_CM ? 0 : ALTURA_DO_TETO_CM - distancia;
}

void perguntarSeCaiu(const char *origem) {
  if (queda != SEM_QUEDA) return;
  queda = PERGUNTANDO;
  perguntaEm = millis();
  origemDaQueda = origem;
  Serial.printf("[queda] %s: possível queda, aviso em %d s\n", origem, CONTAGEM_DA_QUEDA_S);
}

void cuidarDaQueda() {
  unsigned long agora = millis();
  if (mpuOk && agora - ultimaLeituraMpu >= INTERVALO_MPU) {
    ultimaLeituraMpu = agora;
    aceleracaoG = lerAceleracaoG();
    bool impacto = !isnan(aceleracaoG) && aceleracaoG >= IMPACTO_G;
    if (impacto && !impactoAntes) perguntarSeCaiu("impacto no pingente");
    impactoAntes = impacto;
  }
  if (agora - ultimaDistancia >= INTERVALO_DISTANCIA) {
    ultimaDistancia = agora;
    alturaCm = medirAlturaCm();
    bool deitado = alturaCm >= 10 && alturaCm <= ALTURA_DEITADO_CM;
    if (deitado && !noChao) noChaoDesde = agora;
    noChao = deitado;
    if (noChao && agora - noChaoDesde >= TEMPO_NO_CHAO_S * 1000UL) perguntarSeCaiu("alguém deitado no chão do corredor");
  }
  // millis() de novo: perguntaEm pode ter nascido depois do agora lido no começo da função.
  if (queda == PERGUNTANDO && millis() - perguntaEm >= CONTAGEM_DA_QUEDA_S * 1000UL) {
    queda = QUEDA_AVISADA;
    Serial.printf("[queda] sem resposta: QUEDA avisada (%s)\n", origemDaQueda);
    guardar("QUEDA");
  }
}

// ---------- tela e painel ----------

const char *alertaAtual() {
  if (queda == QUEDA_AVISADA) return "QUEDA";
  if (queda == PERGUNTANDO) return "PERGUNTANDO";
  if (tensaoBaixa) return "ENERGIA";
  if (pedindoToque) return "PEDIR_TOQUE";
  if (calor) return "CALOR";
  if (diaCritico) return "DIA_CRITICO";
  return "OK";
}

// A fonte da tela não tem acento, por isso os recados saem sem acento.
void escreverTela(const char *grande, const char *linha1, const char *linha2) {
  tela.clearDisplay();
  tela.setTextColor(SSD1306_WHITE);
  tela.setTextSize(1);
  tela.setCursor(0, 0);
  tela.print("Corredor Aceso");
  tela.setCursor(110, 0);
  tela.print(mqtt.connected() ? "on" : "--");
  tela.setTextSize(2);
  tela.setCursor(0, 14);
  tela.print(grande);
  tela.setTextSize(1);
  tela.setCursor(0, 38);
  tela.print(linha1);
  tela.setCursor(0, 52);
  tela.print(linha2);
  tela.display();
}

void cuidarDaTela() {
  unsigned long agora = millis();
  if (!telaOk || agora - ultimaTela < INTERVALO_TELA) return;
  ultimaTela = agora;
  char linha[24], sensores[24];
  snprintf(sensores, sizeof sensores, "%.1fC %.0fV", isnan(tempC) ? 0.0 : tempC, tensaoV);
  // Só o fato, sem ordem para a pessoa. Letra grande cabe 10 caracteres.
  if (queda == QUEDA_AVISADA) {
    escreverTela("QUEDA", "UBS avisada.", sensores);
  } else if (queda == PERGUNTANDO) {
    long resta = CONTAGEM_DA_QUEDA_S - (long)((agora - perguntaEm) / 1000);
    snprintf(linha, sizeof linha, "Aviso a UBS em %lds", resta < 0 ? 0 : resta);
    escreverTela("QUEDA?", "Possivel queda.", linha);
  } else if (tensaoBaixa) {
    snprintf(linha, sizeof linha, "Tomada em %.0f V", tensaoV);
    escreverTela("SEM LUZ", "Acabou a energia.", linha);
  } else if (pedindoToque) {
    escreverTela("UBS CHAMOU", "Pedido de toque.", sensores);
  } else if (calor) {
    snprintf(linha, sizeof linha, "Casa a %.1f C", tempC);
    escreverTela("CALOR", "Muito quente.", linha);
  } else if (diaCritico) {
    escreverTela("DIA QUENTE", "Dia critico na UBS.", sensores);
  } else {
    if (horaValida()) {
      time_t local = time(nullptr) + FUSO_SEGUNDOS;
      strftime(linha, sizeof linha, "Hora %H:%M", gmtime(&local));
    } else {
      snprintf(linha, sizeof linha, "Sem hora ainda");
    }
    escreverTela("TUDO BEM", linha, sensores);
  }
}

// Foto dos sensores para o painel web, retida no broker: quem abre o painel vê o último estado.
// Fica fora do buffer e do contrato do Motor, que só lê o tópico evento.
void publicarEstado() {
  unsigned long agora = millis();
  if (!mqtt.connected() || !horaValida() || agora - ultimoEstado < INTERVALO_ESTADO) return;
  ultimoEstado = agora;
  time_t ts = time(nullptr);
  char quando[24], temp[12], tensao[12], acel[12], corpo[360];
  strftime(quando, sizeof quando, "%Y-%m-%dT%H:%M:%SZ", gmtime(&ts));
  numero(temp, sizeof temp, tempC);
  numero(tensao, sizeof tensao, tensaoV);
  numero(acel, sizeof acel, aceleracaoG);
  bool buzzer = queda != SEM_QUEDA || alarmeDeEnergia || pedindoToque;
  bool ambar = buzzer || calor || diaCritico;
  bool luz = (long)(luzAte - agora) > 0;
  long sinalHa = houveSinal ? (long)((agora - ultimoSinalEm) / 1000) : -1;
  snprintf(corpo, sizeof corpo,
           "{\"ts\":\"%s\",\"tempC\":%s,\"tensaoV\":%s,\"alturaCm\":%d,\"aceleracaoG\":%s,\"luz\":%s,"
           "\"ambar\":%s,\"buzzer\":%s,\"alerta\":\"%s\",\"ultimoSinalHaS\":%ld,\"noBuffer\":%d}",
           quando, temp, tensao, alturaCm, acel, luz ? "true" : "false", ambar ? "true" : "false",
           buzzer ? "true" : "false", alertaAtual(), sinalHa, tamanhoBuffer);
  mqtt.publish(topicoEstado, corpo, true);
}

void setup() {
  Serial.begin(115200);
  pinMode(PINO_PIR, INPUT);
  pinMode(PINO_LUZ, OUTPUT);
  pinMode(PINO_BOTAO, INPUT_PULLUP);
  pinMode(PINO_AMBAR, OUTPUT);
  ledcAttach(PINO_BUZZER, 2000, 10);
  somDesligar();
  pinMode(PINO_WIFI, INPUT_PULLUP);
  pinMode(PINO_TRIG, OUTPUT);
  pinMode(PINO_ECHO, INPUT);
  dht.setup(PINO_DHT, DHTesp::DHT22);
  Wire.begin();
  Wire.setClock(400000);
  telaOk = tela.begin(SSD1306_SWITCHCAPVCC, ENDERECO_TELA);
  if (telaOk) escreverTela("OLA", "Ligando...", "");
  iniciarMpu();
  snprintf(topicoEvento, sizeof topicoEvento, "%s/corredor/v1/%s/evento", MQTT_PREFIXO, CASA_TOKEN);
  snprintf(topicoComando, sizeof topicoComando, "%s/corredor/v1/%s/comando", MQTT_PREFIXO, CASA_TOKEN);
  snprintf(topicoEstado, sizeof topicoEstado, "%s/corredor/v1/%s/estado", MQTT_PREFIXO, CASA_TOKEN);
  mqtt.setServer(MQTT_HOST, MQTT_PORTA);
  mqtt.setCallback(aoReceber);
  mqtt.setBufferSize(512);
  mqtt.setSocketTimeout(5);
  configTime(0, 0, NTP_1, NTP_2);
  wifiLigado = digitalRead(PINO_WIFI) == LOW;
  Serial.printf("Corredor Aceso: nó da casa %.6s... publica em %s\n", CASA_TOKEN, topicoEvento);
  Serial.printf("[pecas] tela %s, pingente %s\n", telaOk ? "ok" : "ausente", mpuOk ? "ok" : "ausente");
}

void loop() {
  cuidarDoCorredor();
  cuidarDoBotao();
  cuidarDaTelemetria();
  cuidarDaQueda();
  cuidarDosAvisos();
  cuidarDoSilencio();
  cuidarDaTela();
  cuidarDaRede();
  delay(10);
}
