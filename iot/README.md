# Nó do corredor e simulador das 39 casas

Esta pasta tem as duas fontes de pacote do Corredor Aceso:

- `no/`: o nó do corredor, um ESP32 simulado no Wokwi. É a entrega de Physical Computing.
- `simulador/`: um programa em Python que faz as outras 39 casas da microárea na demo.

Os dois falam o mesmo contrato MQTT, da seção 6.2 de
[03-modelo-de-dominio.md](../docs/arquitetura/03-modelo-de-dominio.md). O pacote leva só o token da casa.
Nenhum nome, endereço ou telefone sai do nó.

## O que Physical Computing avalia e onde está

| Item | Onde |
|---|---|
| Circuito ESP32 com sensores | `no/diagram.json` e `no/no.ino`: sensor de presença, DHT22, potenciômetro no papel do sensor de tensão, botão, dois LEDs e buzzer. Ver "Peças e por que cada uma existe". |
| Integrado à solução | O nó fala o contrato MQTT que o Motor da API consome, e cada movimento é o sinal de vida da casa no radar da ACS. O simulador, com o mesmo contrato, falou com o Motor real na demo integrada. Ver "O que já rodou no Wokwi" e o [relatório da integração](../docs/evidencias/integracao/RELATORIO.md). |
| Evidências | [docs/evidencias/iot/](../docs/evidencias/iot/README.md): o monitor serial, a captura do broker e os prints da rodada no wokwi.com. |
| Link do projeto no Wokwi | Ver "Publicar o projeto no wokwi.com". |
| Vídeo de até 4 minutos | Ver "Roteiro do vídeo de Physical Computing". |
| Nomes dos integrantes | `integrantes.txt`, na raiz do repositório. |

## O nó do corredor

A idosa mora sozinha. Um aparelho pequeno fica no corredor entre o quarto e o banheiro. Quando ela passa,
a luz de caminho acende e o aparelho manda um `MOVIMENTO`. Esse movimento é o "estou bem" que ninguém
precisa lembrar de dar. Num dia crítico de calor, a ACS vê no radar só as casas que ficaram em silêncio.

### Peças e por que cada uma existe

| Peça no Wokwi | Pino do ESP32 | Para a pessoa | Para o sistema |
|---|---|---|---|
| Sensor de presença PIR | GPIO 27 | Acende a luz quando ela passa, sem tocar em nada. | `MOVIMENTO`, o sinal de vida da casa `Sozinho`. Um por passagem, no máximo um a cada 30 s. |
| LED branco com resistor de 220 Ω | GPIO 26 | Luz de caminho por 30 s. Evita a queda no escuro, de madrugada. | Funciona sem rede. A luz nunca espera o Wi-Fi. |
| DHT22 | GPIO 15 | Nada para ela fazer. | `tempC` na `TELEMETRIA`. Casa a 34 °C ou mais por muito tempo vira `CalorInterno` no radar. |
| Potenciômetro | GPIO 34 (ADC) | Faz o papel do sensor de tensão da tomada. | `tensaoV` na `TELEMETRIA`. Escala de 0 a 250 V. Abaixo de 100 V numa casa eletrodependente, o concentrador de oxigênio parou: `EnergiaEletrodependente`. |
| Botão grande verde | GPIO 25, pull-up interno | Um toque: "estou bem". Dois toques: "hoje vou sair". Um bipe confirma o toque, dois bipes o duplo toque. | `BOTAO_BEM` e `BOTAO_SAIR`. A saída avisada suspende a regra de silêncio até o fim do turno. |
| LED laranja (âmbar) com resistor de 220 Ω | GPIO 33 | Aceso: hoje é dia crítico, beba água. Piscando: a UBS pediu um toque no botão. | Comandos `DIA_CRITICO` e `PEDIR_TOQUE`. |
| Buzzer | GPIO 32 | Às 14h30, se ela não deu sinal desde as 10h, o buzzer chama até ela apertar o botão. | Comando `PEDIR_TOQUE`. O toque no botão para o buzzer e manda `BOTAO_BEM`. |
| Chave deslizante | GPIO 14, pull-up interno | Não existe no aparelho real. | Chave da demo: desligada, o firmware desliga o Wi-Fi de verdade (`WiFi.mode(WIFI_OFF)`). Serve para mostrar o buffer no vídeo. |

### O que o firmware faz

- A luz e os sensores rodam no `loop()` antes da rede. Sem Wi-Fi, a luz continua acendendo.
- Todo evento (`MOVIMENTO`, `BOTAO_BEM`, `BOTAO_SAIR`, `TELEMETRIA`) entra num buffer de 64 eventos em RAM.
  Com rede, sai na hora com `"bufferizado": false`. Sem rede, fica guardado. Na volta, sai na ordem, com o
  `seq` e o `ts` do momento do fato e `"bufferizado": true`. O Motor grava cada evento uma vez, pela chave
  `(casaToken, seq, ts)`.
- O `HEARTBEAT` sai a cada 20 s, só com rede, e nunca entra no buffer. Ele diz que o aparelho está ligado
  agora, então um heartbeat atrasado não teria sentido.
- A `TELEMETRIA` sai ao ligar, a cada 5 min e na hora em que a tensão cruza 100 V, para cima ou para baixo.
- O `ts` vem do NTP, em UTC. Um evento que acontece antes do NTP responder guarda o `millis()` e ganha o
  `ts` certo quando a hora chega. Nenhum pacote sai sem hora.
- O `seq` começa em 1 a cada religamento e sobe em todo pacote. Depois de um reinício, o `seq` repete com
  outro `ts`, e o Motor aceita.
- Comandos chegam em `<prefixo>/corredor/v1/{token}/comando`. `DIA_CRITICO` acende o âmbar por 12 h.
  `PEDIR_TOQUE` pisca o âmbar e toca o buzzer até o botão ou por 10 min.

Limites conhecidos: o buffer vive na RAM e some se o ESP32 reiniciar sem rede. Com 64 eventos cheios, o
mais antigo é descartado e o serial avisa. O `mqtt.connect()` da PubSubClient bloqueia até 5 s quando o
broker não responde, e nesse tempo a luz não muda de estado.

### Rede: do Wokwi até a VM

O Wokwi do navegador não alcança a rede local da VM. O nó publica no broker público `broker.hivemq.com:1883`
(QoS 0, o único da PubSubClient), em `<prefixo>/corredor/v1/{casaToken}/evento`. O Mosquitto da VM abre
uma ponte de saída e traz o tópico para `corredor/v1/{casaToken}/evento`. Os comandos fazem o caminho
inverso. A seção 1.11 de [05-infra-e-entrega.md](../docs/arquitetura/05-infra-e-entrega.md) mede essa ponte.

O prefixo e o broker vêm de [corredor.conf.padrao](../infra/config/corredor.conf.padrao)
(`MQTT_PUBLICO_HOST`, `MQTT_PUBLICO_PORTA`, `MQTT_PREFIXO_PUBLICO`). Em [no/config.h](no/config.h),
`MQTT_PREFIXO` precisa ter o mesmo valor da VM. O padrão do repositório, `gs26-TROQUE-ISTO`, é um marcador:
troque nos dois lugares por um valor da equipe.

### Token da casa

`CASA_TOKEN` em [no/config.h](no/config.h) vem com `fa11fa11fa11fa11fa11fa11fa11fa11`, a casa de teste
(`CASA_TESTE_TOKEN`) que o seed do Motor ativa. Para a demo, troque pelo token da casa 1 de
`api/semente/tokens.local.json`, que o `Corredor.Api --semear` escreve na VM. Faça a troca só no projeto do
wokwi.com. O token real nunca entra no Git: o broker é público, e quem tem o token pode mandar um
`MOVIMENTO` falso.

### Compilar fora do Wokwi

Testado com `arduino-cli` 1.6.0, núcleo `esp32:esp32` 3.3.12, PubSubClient 2.8.0 e DHT sensor library
for ESPx 1.19.0:

```bash
arduino-cli core install esp32:esp32 --additional-urls https://espressif.github.io/arduino-esp32/package_esp32_index.json
```

```bash
arduino-cli lib install PubSubClient "DHT sensor library for ESPx"
```

```bash
arduino-cli compile --fqbn esp32:esp32:esp32 --warnings all --output-dir iot/no/build iot/no
```

A pasta `no/build/` fica fora do Git. O [no/wokwi.toml](no/wokwi.toml) aponta para ela, para quem usa a
extensão do Wokwi no VS Code ou o Wokwi CLI.

### Abrir e rodar no wokwi.com

1. Abra <https://wokwi.com/projects/new/esp32>.
2. Na aba `sketch.ino`, apague tudo e cole o conteúdo de [no/no.ino](no/no.ino). Troque a linha
   `#include "config.h"` pelo conteúdo de [no/config.h](no/config.h), ou crie um arquivo `config.h` pelo
   menu da seta ao lado das abas e cole nele.
3. Na aba `diagram.json`, apague tudo e cole [no/diagram.json](no/diagram.json).
4. Em `Library Manager`, clique em `+` e adicione `PubSubClient` e `DHT sensor library for ESPx`. Isso gera
   o mesmo `libraries.txt` de [no/libraries.txt](no/libraries.txt).
5. Em `config.h`, ponha o prefixo da equipe em `MQTT_PREFIXO` e o token da casa 1 em `CASA_TOKEN`.
6. Clique no botão verde de iniciar. No monitor serial aparecem `[wifi] conectando`, `[mqtt] conectado` e um
   `[mqtt] {...}` para cada evento.

Para mexer no circuito durante a simulação:

- PIR: clique no sensor e em `Simulate motion`.
- Botão: clique no botão verde e segure um instante, ou aperte a tecla `b` com o foco no diagrama. O
  firmware ignora toque com menos de 30 ms, que é ruído de contato. Um clique de automação, sem tempo de
  pressão, não conta. Dois toques rápidos são "hoje vou sair".
- Tensão: gire o potenciômetro, ou clique nele e use `Home` e `End`. Ele começa em 127 V. Abaixo de
  100 V, o serial mostra `[energia] tensão cruzou 100 V`.
- Temperatura: clique no DHT22 e mude a temperatura.
- Wi-Fi: a chave começa na posição "Wi-Fi ligado". Mude de posição: o serial mostra
  `[wifi] chave desligada`, e cada evento
  seguinte mostra `sem rede: guardado (N no buffer)`. Volte a chave: o serial mostra
  `[buffer] N evento(s) reenviados com a hora original`.

### Publicar o projeto no wokwi.com (equipe)

O link do projeto é entregável de Physical Computing. Publicar exige uma conta da equipe:

1. Entre em <https://wokwi.com> com a conta da equipe.
2. Monte o projeto pelos passos de "Abrir e rodar no wokwi.com", com o prefixo da equipe e o token da casa 1.
3. Clique em `Save`. Dê o nome `Corredor Aceso - nó do corredor`.
4. Clique em `Share` e copie o link `https://wokwi.com/projects/<id>`.
5. Antes de entregar o link, confira se o projeto público pode levar o token. Quem abre o projeto vê o
   `config.h`. Se não puder, troque `CASA_TOKEN` de volta para a casa de teste antes de salvar a versão
   pública, e use uma cópia privada para gravar o vídeo.
6. Cole o link aqui, no lugar da linha abaixo, e no arquivo de entrega da disciplina.

Link do projeto: ainda não publicado.

### O que já rodou no Wokwi

Em 17/11/2026, este firmware e este `diagram.json` rodaram no simulador do wokwi.com, sem conta, com um
prefixo de teste e a casa de teste. Os arquivos colados no Wokwi tinham o mesmo SHA-256 dos arquivos do
repositório. Um assinante no `broker.hivemq.com` capturou o que o nó publicou, e o `conferir_fio.py`
conferiu a captura. Tudo está em [docs/evidencias/iot/wokwi/](../docs/evidencias/iot/wokwi/):

- `serial.txt`: o monitor serial da rodada.
- `captura-broker-publico.txt` e `conferencia.txt`: 20 eventos e 2 comandos no fio, todos no contrato.
- `luz-acesa-pelo-pir.png`, `circuito-conectado.jpg` e `wifi-cortado-luz-acesa.jpg`: telas da rodada.

O que a rodada mostrou:

1. Conexão ao `Wokwi-GUEST`, ao NTP e ao broker público. `TELEMETRIA` inicial com 29,5 °C e 127,1 V.
2. O PIR acendeu a luz de caminho e mandou `MOVIMENTO`. O botão mandou `BOTAO_BEM`.
3. `DIA_CRITICO` e `PEDIR_TOQUE`, publicados no broker público, chegaram ao nó. O LED âmbar acendeu.
4. Com a chave do Wi-Fi desligada, um `MOVIMENTO` e um `BOTAO_BEM` ficaram no buffer, e a luz acendeu
   mesmo assim. Na volta, os dois saíram com o `ts` de quando aconteceram (35 e 40 s antes da chegada) e
   `bufferizado: true`. O heartbeat não entrou no buffer.
5. O potenciômetro em 0 V e depois no máximo mandou uma `TELEMETRIA` em cada cruzamento de 100 V.
6. Os pacotes ao vivo chegaram ao assinante de 1 a 8 s depois do `ts`, dentro da tolerância de 2 min do
   Motor.

A rodada achou três defeitos, todos consertados antes da versão final: a chave do Wi-Fi começava na
posição desligada, o potenciômetro começava no fim da escala (o atributo `value` vai de 0 a 1023) e a
primeira `TELEMETRIA` de cada boot saía marcada como bufferizada, porque nascia antes de a rede subir.

Não rodou: a ponte do Mosquitto da VM até o Motor com este nó, e o Wokwi CLI, que pede um token de conta.

## O simulador das 39 casas

O simulador faz as casas 2 a 40 da semente. A casa 1 é o nó do Wokwi. Ele publica no Mosquitto local, com
QoS 1, o mesmo contrato do nó: mesmos tipos, mesma cadência, mesmo buffer. Ele assina
`corredor/v1/+/comando` e responde ao `PEDIR_TOQUE` como a pessoa responderia.

### Rodar

Na VM, o serviço `corredor-simulador.service` roda o cenário `demo` e é ligado à mão:

```bash
sudo systemctl start corredor-simulador
```

O serviço chama `simulador.py --host 127.0.0.1 --porta 1883` dentro de `iot/simulador/`, com o Python de
`/opt/corredor/venv`. O `02-app.sh` instala o [requirements.txt](simulador/requirements.txt) nesse venv.

No notebook, com o Mosquitto do `compose.dev.yml`:

```bash
pip install -r iot/simulador/requirements.txt
```

```bash
python3 iot/simulador/simulador.py --host 127.0.0.1 --porta 1883 --cenario manha --tokens-ficticios
```

O simulador lê os tokens de `api/semente/tokens.local.json`, que o `Corredor.Api --semear` escreve. O
arquivo tem `n`, `casaToken` e `papelNaDemo` de cada casa e fica fora do Git. Sem ele, passe `--tokens`
com outro caminho, ou `--tokens-ficticios` para sortear 39 tokens que o Motor vai descartar.

### Cenários

| `--cenario` | O que acontece | Casa padrão (`--casa`) |
|---|---|---|
| `demo` (padrão) | Cada casa faz o papel que a semente dá a ela: B manda `BOTAO_SAIR`, C, D e E ficam em silêncio, F fica a 34,5 °C, G só aperta o botão depois do `PEDIR_TOQUE`, a casa do aparelho mudo para de mandar pacotes. As outras andam normalmente. | todas |
| `manha` | Manhã normal: as 39 casas com movimento, heartbeat a cada 20 s e telemetria ao ligar e a cada 5 min. | nenhuma |
| `silencio` | A casa continua mandando heartbeat e telemetria, mas ninguém passa no corredor e ninguém aperta o botão. | 5 |
| `mudo` | O aparelho para de mandar qualquer pacote. | 10 |
| `queda-de-energia` | A tensão cai a 0 V por `--janela` segundos e volta a 127 V. Uma `TELEMETRIA` sai em cada cruzamento de 100 V. A casa 2 é eletrodependente na semente. | 2 |
| `reenvio` | O Wi-Fi cai por `--janela` segundos. Os eventos ficam no buffer e voltam duas vezes com o `seq` e o `ts` originais e `bufferizado: true`, como o cenário `broker` do teste de falha. | 13 |

Outras opções: `--inicio` (segundos até o cenário começar, padrão 30), `--janela` (padrão 60), `--duracao`
(segundos até sair; 0 roda para sempre), `--semente` (repete o sorteio), `--detalhado` (mostra os
heartbeats no log).

### Testes e conferência no fio

```bash
cd iot/simulador && python3 -m unittest -v
```

```bash
npm run check:iot
```

Os testes não precisam de broker nem de dependência. Eles andam o relógio do modelo e conferem o contrato,
a cadência, cada cenário e o conferidor do fio, inclusive com capturas erradas de propósito.

[simulador/provar_no_fio.sh](simulador/provar_no_fio.sh) roda os seis cenários contra um Mosquitto em
contêiner, captura tudo com `mosquitto_sub` e confere cada captura com
[simulador/conferir_fio.py](simulador/conferir_fio.py). O conferidor recusa tópico, campo, tipo ou `ts` fora
do contrato e a mesma chave com corpos diferentes. Com o log do simulador, ele confere também o heartbeat a
cada 20 s, o silêncio, o aparelho mudo, a telemetria da queda e o reenvio com o `seq`, o `ts` e o tipo de
quando o evento foi guardado. O resultado da última rodada está em
[docs/evidencias/iot/](../docs/evidencias/iot/).

```bash
docker run -d --name corredor-iot-fio -p 127.0.0.1:18839:1883 -v "$PWD/infra/config/dev/mosquitto.conf:/mosquitto/config/mosquitto.conf:ro" eclipse-mosquitto:2
```

```bash
bash iot/simulador/provar_no_fio.sh corredor-iot-fio 18839 docs/evidencias/iot/fio
```

```bash
docker rm -f corredor-iot-fio
```

## Roteiro do vídeo de Physical Computing (até 4 minutos)

Antes de gravar: VM local de pé com a última versão, simulador parado, relógio de demo às 08h00
(`PUT /api/demo/relogio`), projeto do Wokwi com o prefixo da equipe e o token da casa 1, app aberto no
radar da microárea 3. Tela dividida: Wokwi à esquerda, app ou Swagger à direita, monitor serial visível.

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:25 | Foto ou desenho do corredor. Depois, o circuito no Wokwi. | "Dona Efigênia mora sozinha. Se ela cair, ninguém sabe. Este aparelho fica no corredor. Ela não precisa usar celular nem lembrar de nada." |
| 0:25 a 0:55 | Aponta cada peça do circuito. | "O sensor de presença acende a luz de caminho. O DHT22 mede o calor dentro da casa. O potenciômetro faz o papel do sensor de tensão da tomada, porque ela usa concentrador de oxigênio. O botão grande é o 'estou bem'. O LED âmbar e o buzzer são o recado da UBS." |
| 0:55 a 1:30 | Inicia a simulação. Serial: `[mqtt] conectado`, a `TELEMETRIA` inicial. Clica no PIR. A luz acende. Serial mostra o `MOVIMENTO`. | "Ela passa, a luz acende, e o aparelho manda este pacote. Só o token da casa, nunca o nome. A hora é a do fato, em UTC." |
| 1:30 a 1:55 | Liga o simulador na VM (`sudo systemctl start corredor-simulador`). Radar no app enche de casas. Abre a linha do tempo da casa 1 com o `MOVIMENTO`. | "As outras 39 casas da microárea vêm do simulador, com o mesmo contrato. O movimento da Dona Efigênia já está na linha do tempo dela." |
| 1:55 a 2:45 | Muda a chave do Wi-Fi. Serial: `Wi-Fi cortado`. Clica no PIR, aperta o botão. Serial: `sem rede: guardado (1 no buffer)`, `(2 no buffer)`. A luz acende mesmo assim. Volta a chave. Serial: `reenviados com a hora original` e os pacotes com `"bufferizado":true`. Linha do tempo mostra os dois eventos com a hora em que aconteceram, marcados como bufferizados. | "A internet da casa caiu. A luz continua. O aparelho guarda o que aconteceu. Quando a rede volta, ele manda tudo com a hora original, e o servidor grava cada evento uma vez só." |
| 2:45 a 3:15 | Declara o dia crítico (job de ML ou Swagger). O LED âmbar acende. Move o relógio para 14h30. Âmbar pisca, buzzer toca. Aperta o botão: buzzer para, serial mostra `BOTAO_BEM`. | "Num dia crítico de calor, a UBS acende o âmbar em todas as casas. Às 14h30, quem não deu sinal desde as 10h ouve o buzzer. Um toque e a casa volta ao verde." |
| 3:15 a 3:45 | Gira o potenciômetro abaixo de 100 V. Serial: `tensão cruzou 100 V`. Radar mostra a casa 1 vermelha com o motivo de energia. | "Se a luz da rua cai, o concentrador para. O aparelho avisa na hora, e a casa sobe para o topo do radar da ACS." |
| 3:45 a 4:00 | Radar com as casas silenciosas no topo. | "A ACS não liga para 40 casas. Ela vê só as que ficaram em silêncio." |
