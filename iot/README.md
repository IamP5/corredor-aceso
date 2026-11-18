# Nó do corredor e simulador das 39 casas

Esta pasta tem as duas fontes de pacote do Corredor Aceso:

- `no/`: o nó do corredor, um ESP32 simulado no Wokwi, que constitui a entrega de Physical Computing.
- `simulador/`: um programa em Python que faz as outras 39 casas da microárea na demo.

Os dois falam o mesmo contrato MQTT, da seção 6.2 de
[03-modelo-de-dominio.md](../docs/arquitetura/03-modelo-de-dominio.md). O pacote leva só o token da casa.
Nenhum nome, endereço ou telefone sai do nó.

Na entrega de Physical Computing, o nó conversa com o [painel web](../site/painel/index.html) pelo MQTT, sem a
API: o Wokwi publica no broker público, e o painel assina o mesmo broker por WebSocket seguro. Quem avalia
abre o link do Wokwi e o link do painel e vê a casa ao vivo, sem VM e sem notebook. Os eventos continuam os
mesmos que o Motor da API consome. Por isso o caminho até a API segue de pé (ver "Rede: do Wokwi até a VM"),
mas fica fora desta entrega.

## O que Physical Computing avalia e onde está

| Item | Onde |
|---|---|
| Circuito ESP32 com sensores | `no/diagram.json` e `no/no.ino`: sensor de presença, DHT22, potenciômetro no papel do sensor de tensão, pingente MPU6050, sensor de teto HC-SR04, tela OLED, botão, dois LEDs e buzzer. Ver "Peças e por que cada uma existe". |
| Integrado à solução | O nó publica os eventos do contrato MQTT do Corredor Aceso. O [painel web](../site/painel/index.html) é a visão da agente de saúde sobre uma casa: mostra a situação, os sensores e a linha do tempo, e manda `DIA_CRITICO` e `PEDIR_TOQUE`. Ver "O painel web". |
| Evidências | [docs/evidencias/iot/](../docs/evidencias/iot/README.md): monitor serial, captura do broker, linha do tempo do painel e prints das rodadas no wokwi.com. |
| Link do projeto no Wokwi | <https://wokwi.com/projects/481069519743904769>, com a casa de teste e `SILENCIO_DEMO_S 90`. Para montar outro, siga "Abrir e rodar no wokwi.com". |
| Link do painel | <https://iamp5.github.io/corredor-aceso/painel/>, publicado pelo GitHub Pages junto com o guia. |
| Vídeo de até 4 minutos | Roteiro em [roteiro-iot.md](../docs/entrega/roteiro-iot.md). O link consta na seção "Vídeos" do [README da raiz](../README.md#vídeos). |
| Nomes dos integrantes | `integrantes.txt`, na raiz do repositório. |

## O nó do corredor

A idosa mora sozinha. Um aparelho pequeno fica no corredor entre o quarto e o banheiro. Quando ela passa,
a luz de caminho acende e o aparelho manda um `MOVIMENTO`. Esse movimento vale como um "estou bem" que
não depende de a pessoa lembrar de nada. Num dia crítico de calor, a ACS vê no radar só as casas que ficaram em silêncio.

### Peças e por que cada uma existe

| Peça no Wokwi | Pino do ESP32 | Para a pessoa | Para o sistema |
|---|---|---|---|
| Sensor de presença PIR | GPIO 27 | Acende a luz quando ela passa, sem tocar em nada. | `MOVIMENTO`, o sinal de vida da casa `Sozinho`. Um por passagem, no máximo um a cada 30 s. |
| LED branco com resistor de 220 Ω | GPIO 26 | Luz de caminho por 30 s. Evita a queda no escuro, de madrugada. | Funciona sem rede. A luz nunca espera o Wi-Fi. |
| DHT22 | GPIO 15 | A 34 °C ou mais, o âmbar acende sozinho com três bipes e a tela mostra "CALOR". Apaga abaixo de 33,5 °C. | `tempC` na `TELEMETRIA`. Casa a 34 °C ou mais por muito tempo vira `CalorInterno` no radar. |
| Potenciômetro | GPIO 34 (ADC) | Faz o papel do sensor de tensão da tomada. Abaixo de 100 V, o aparelho dispara o alarme (âmbar piscando rápido e buzzer) até a tensão voltar ou alguém apertar o botão. | `tensaoV` na `TELEMETRIA`. Escala de 0 a 250 V. Abaixo de 100 V numa casa eletrodependente, o concentrador de oxigênio parou: `EnergiaEletrodependente`. |
| Botão grande verde | GPIO 25, pull-up interno | Cada toque é um "estou bem", confirmado por um bipe. Também silencia o buzzer. | `BOTAO_BEM`. O aparelho não registra se a pessoa saiu de casa. |
| LED laranja (âmbar) com resistor de 220 Ω | GPIO 33 | Aceso: dia crítico ou casa a 34 °C. Piscando: pedido de toque. Piscando rápido: falta de energia ou queda. | Comandos `DIA_CRITICO` e `PEDIR_TOQUE`, e as reações locais. |
| Buzzer | GPIO 32 | Às 14h30, se ela não deu sinal desde as 10h, o buzzer chama até ela apertar o botão. Também toca no alarme de energia. | Comando `PEDIR_TOQUE` ou a mesma regra decidida no aparelho. O toque no botão para o buzzer e manda `BOTAO_BEM`. |
| Chave deslizante | GPIO 14, pull-up interno | Não existe no aparelho real. | Chave da demo: desligada, o firmware desliga o Wi-Fi de verdade (`WiFi.mode(WIFI_OFF)`). Permite demonstrar o buffer. |
| Pingente MPU6050 | I2C: SDA 21, SCL 22 (0x68) | Ela usa no pescoço. Num tombo, o impacto passa de 2 g. | Começa a contagem de "possível queda". No aparelho real, o pingente seria sem fio. No Wokwi, ele vai ligado ao ESP32. |
| Sensor de teto HC-SR04 | TRIG 5, ECHO 18 | Fica no teto do corredor, apontado para o chão, e mede a altura do que está embaixo: vazio, alguém em pé ou alguém deitado (de 10 a 50 cm). | Alguém deitado no chão por 30 s também começa a contagem de "possível queda", mesmo sem o pingente. No aparelho real, o ECHO precisa de divisor de tensão, porque sai em 5 V. |
| Tela OLED SSD1306 | I2C: SDA 21, SCL 22 (0x3C) | Só o fato, em letras grandes, sem ordem para a pessoa: "TUDO BEM", "CALOR", "DIA QUENTE", "SEM LUZ", "UBS CHAMOU", "QUEDA?" e "QUEDA". A fonte da tela não tem acentos. | Mostra a conexão (`on` ou `--`), a temperatura e a tensão. |

### O que o firmware faz

- A luz e os sensores rodam no `loop()` antes da rede. Sem Wi-Fi, a luz continua acendendo.
- Todo evento (`MOVIMENTO`, `BOTAO_BEM`, `TELEMETRIA`, `QUEDA`) entra num buffer de 64 eventos em RAM.
  Com rede, sai na hora com `"bufferizado": false`. Sem rede, fica guardado. Na volta, sai na ordem, com o
  `seq` e o `ts` do momento do fato e `"bufferizado": true`. O Motor grava cada evento uma vez, pela chave
  `(casaToken, seq, ts)`.
- O `HEARTBEAT` sai a cada 20 s, só com rede, e nunca entra no buffer. Ele indica que o aparelho está ligado
  naquele instante, e por isso um heartbeat atrasado não teria sentido.
- A `TELEMETRIA` sai ao ligar, a cada 5 min e na hora em que a tensão cruza 100 V, para cima ou para baixo.
- O `ts` vem do NTP, em UTC. Um evento que acontece antes do NTP responder guarda o `millis()` e ganha o
  `ts` certo quando a hora chega. Nenhum pacote sai sem hora.
- O `seq` começa em 1 a cada religamento e sobe em todo pacote. Depois de um reinício, o `seq` repete com
  outro `ts`, e o Motor aceita.
- Comandos chegam em `<prefixo>/corredor/v1/{token}/comando`. `DIA_CRITICO` acende o âmbar por 12 h.
  `PEDIR_TOQUE` pisca o âmbar e toca o buzzer até o botão ou por 10 min.
- A queda tem três passos. Primeiro, um impacto no pingente ou alguém deitado sob o sensor de teto por 30 s.
  Depois, a tela mostra "QUEDA?" com a contagem e o buzzer bipa a cada segundo por 15 s. Um toque no botão cancela e manda
  `BOTAO_BEM`. Sem toque, sai um evento `QUEDA`, que entra no buffer como os outros, e o alarme toca até o
  botão.
- O tópico `<prefixo>/corredor/v1/{token}/estado` recebe uma foto dos sensores e das saídas a cada 2 s, retida
  no broker, para o painel web. A foto não entra no buffer: ela só vale no instante em que sai.
- O buzzer só toca bipes curtos, nunca um som contínuo. São três bipes por segundo na queda avisada, três a
  cada 1,6 s na falta de energia, um por segundo na possível queda e dois a cada 2 s no pedido de toque. O firmware
  liga e desliga o PWM pelo LEDC (`ledcWriteTone`), e o buzzer do `diagram.json` usa `"mode": "accurate"`.
  O modo padrão (`smooth`) e o `noTone()` emendavam os bipes num som contínuo.

`QUEDA` e o tópico `estado` existem só para o painel. O Motor da API não conhece o tipo `QUEDA` e recusa o
pacote pelo tradutor, sem quebrar. A ponte da VM só leva o tópico `evento`. Não houve mudança na API.

### O que o nó faz sem a VM

O aparelho protege a pessoa mesmo sem servidor, sem broker e sem Wi-Fi. As reações locais usam os
limites do Motor e não mudam o contrato MQTT: os pacotes continuam os mesmos.

| Situação | Reação no aparelho | Constante |
|---|---|---|
| Pessoa passa no corredor | Luz de caminho por 30 s. | `LUZ_ACESA` |
| Botão | Um bipe para "estou bem". Para o buzzer, o alarme de energia e a queda. | `DEBOUNCE` |
| DHT22 a 34 °C ou mais | Âmbar aceso e três bipes. Apaga abaixo de 33,5 °C. | `TEMPERATURA_DE_CALOR`, `FIM_DO_CALOR` |
| Tensão abaixo de 100 V | Âmbar piscando rápido e buzzer a cada segundo, até a tensão voltar ou o botão. | `TENSAO_MINIMA` |
| 14h30 sem `MOVIMENTO` nem botão desde as 10h | Âmbar piscando e buzzer até o botão ou por 10 min, uma vez por dia. | `HORA_DO_PEDIDO_MIN`, `INICIO_DA_JANELA_MIN` em `config.h` |
| Impacto de 2 g ou mais no pingente, ou alguém deitado sob o sensor de teto por 30 s | "QUEDA?" na tela e bipe a cada segundo por 15 s. O botão cancela. Sem toque, `QUEDA` e alarme até o botão. | `IMPACTO_G`, `ALTURA_DEITADO_CM`, `TEMPO_NO_CHAO_S`, `CONTAGEM_DA_QUEDA_S` em `config.h` |
| Qualquer situação acima | A tela OLED mostra o recado do momento em letras grandes. | |

O pedido de toque local usa o relógio do ESP32 (NTP, no fuso `FUSO_SEGUNDOS`), não o relógio de demo da
API. Ele só conclui que houve silêncio desde as 10h se o aparelho estava ligado desde as 10h. Se o servidor
também mandar `PEDIR_TOQUE`, o buzzer toca uma vez só. `PEDIDO_LOCAL_DE_TOQUE 0` desliga a regra
local, para uma casa que não é `Sozinho`.

Para mostrar o pedido de toque sem esperar as 14h30, `SILENCIO_DEMO_S` em `config.h` troca a janela do
relógio por N segundos sem sinal de vida. Com `90`, o buzzer chama 90 s depois do boot ou do último
movimento ou toque. O repositório fica com `0`, o comportamento real.

O alarme de energia pressupõe que o aparelho real tenha bateria própria, porque mede a tomada do
concentrador. No Wokwi, o ESP32 continua ligado com o potenciômetro em 0 V.

Limites conhecidos: o buffer vive na RAM e some se o ESP32 reiniciar sem rede. Com 64 eventos cheios, o
mais antigo é descartado e o serial avisa. O `mqtt.connect()` da PubSubClient bloqueia até 5 s quando o
broker não responde, e nesse tempo a luz não muda de estado. Por isso, depois de cada falha, a próxima
tentativa espera o dobro, até 60 s. Sem isso, com o gateway de Wi-Fi do Wokwi caído, o loop passava quase
todo o tempo preso em `connect()`, o botão não respondia e o ESP32 chegou a reiniciar com um `assert` do
lwIP.

### O painel web

[site/painel/index.html](../site/painel/index.html) é uma página estática, sem servidor. Ela usa a biblioteca
mqtt.js e assina `wss://broker.hivemq.com:8884/mqtt`, o mesmo broker do nó, pelo WebSocket seguro. O GitHub Pages
publica o painel em <https://iamp5.github.io/corredor-aceso/painel/>, e o `tools/montar-site.sh` copia a pasta
para o site.

| Parte da tela | De onde vem |
|---|---|
| Situação da casa: tudo bem, dia crítico, casa quente, falta de energia, pedido de toque, possível queda, queda avisada ou sem notícias do aparelho | Campo `alerta` do tópico `estado`. Sem foto há mais de 30 s, aparece "Sem notícias do aparelho". |
| Último sinal de vida | O `MOVIMENTO` ou `BOTAO_BEM` mais recente, ou o `ultimoSinalHaS` do estado. |
| Sensores | Temperatura, tensão, altura sob o sensor de teto, aceleração do pingente, e o estado da luz, do âmbar e do buzzer, todos do tópico `estado`. |
| Linha do tempo | O tópico `evento`, com o rótulo em português e a marca "guardado sem rede" nos eventos bufferizados. |
| Ações da UBS | "Declarar dia crítico" publica `DIA_CRITICO` e "Pedir um toque" publica `PEDIR_TOQUE` no tópico `comando`. |

O prefixo e o token vêm da URL (`?prefixo=...&token=...`) ou dos campos em "Casa e broker". O padrão é o
prefixo da equipe com a casa de teste. Como o broker é público, o painel deve ser usado só com a casa de teste.

Para testar no notebook antes do Pages:

```bash
bash tools/montar-site.sh /tmp/site && python3 -m http.server -d /tmp/site 8000
```

Depois, abra <http://localhost:8000/painel/>.

### Rede: do Wokwi até a VM

O Wokwi do navegador não alcança a rede local da VM. O nó publica no broker público `broker.hivemq.com:1883`
(QoS 0, o único da PubSubClient), em `<prefixo>/corredor/v1/{casaToken}/evento`. O Mosquitto da VM abre
uma ponte de saída e traz o tópico para `corredor/v1/{casaToken}/evento`. Os comandos fazem o caminho
inverso. A seção 1.11 de [05-infra-e-entrega.md](../docs/arquitetura/05-infra-e-entrega.md) mede essa ponte.

O prefixo e o broker vêm de [corredor.conf.padrao](../infra/config/corredor.conf.padrao)
(`MQTT_PUBLICO_HOST`, `MQTT_PUBLICO_PORTA`, `MQTT_PREFIXO_PUBLICO`). Em [no/config.h](no/config.h),
`MQTT_PREFIXO` precisa ter o mesmo valor da VM. O prefixo da equipe é `corredor-aceso-40028922`, igual
nos dois arquivos. Para trocar, mude os dois juntos e refaça o deploy, porque o `02-app.sh` gera a ponte
do Mosquitto a partir do `corredor.conf`.

### Rede sem a VM: ponte no notebook

O mesmo caminho roda no notebook, com a API do `tools/demo.sh`. O
[mosquitto-ponte.conf](../infra/config/dev/mosquitto-ponte.conf) roda no macOS, fora do Docker, e liga o
broker público ao Mosquitto do `compose.dev.yml`, onde a API escuta. Ele fica fora do Docker porque, dentro
do Colima, o Mosquitto não resolve `broker.hivemq.com`: a consulta de IPv6 estoura o tempo. O arquivo tem
o prefixo da equipe escrito nele, e uma troca de prefixo precisa passar por ele também.

```bash
brew install mosquitto
```

```bash
bash tools/demo.sh subir
```

```bash
mosquitto -c infra/config/dev/mosquitto-ponte.conf
```

Em 18/11/2026, um `MOVIMENTO` com o token da casa 1, publicado com `mosquitto_pub` no broker público com o
prefixo da equipe, passou pela ponte e chegou a `motor.evento_sensor` em 5 s, com o `ts` original. Um
`DIA_CRITICO` publicado no broker do compose saiu no broker público em
`corredor-aceso-40028922/corredor/v1/{token}/comando`. O nó do Wokwi rodou por esse caminho logo depois:
ver "O que rodou no Wokwi em 18/11".

### Token da casa

`CASA_TOKEN` em [no/config.h](no/config.h) vem com `fa11fa11fa11fa11fa11fa11fa11fa11`, a casa de teste
(`CASA_TESTE_TOKEN`) que o seed do Motor ativa. A entrega de Physical Computing usa sempre a casa de teste,
no Wokwi e no painel. Só uma demonstração com a API, fora desta entrega, usaria o token da casa 1 de
`api/semente/tokens.local.json`, que o `Corredor.Api --semear` escreve na VM. Nesse caso, faça a troca só
numa cópia do projeto no wokwi.com que não seja salva como pública. O token real nunca entra no Git: o
broker é público, e quem tem o token pode mandar um `MOVIMENTO` falso.

### Compilar fora do Wokwi

Testado com `arduino-cli` 1.6.0, núcleo `esp32:esp32` 3.3.13, PubSubClient 2.8.0, DHT sensor library
for ESPx 1.19.0, Adafruit SSD1306 2.5.17 e Adafruit GFX Library 1.13.0. O MPU6050 é lido direto pelo
`Wire`, sem biblioteca:

```bash
arduino-cli core install esp32:esp32 --additional-urls https://espressif.github.io/arduino-esp32/package_esp32_index.json
```

```bash
arduino-cli lib install PubSubClient "DHT sensor library for ESPx" "Adafruit SSD1306" "Adafruit GFX Library"
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
4. Em `Library Manager`, clique em `+` e adicione `PubSubClient`, `DHT sensor library for ESPx`,
   `Adafruit SSD1306` e `Adafruit GFX Library`. Isso gera o mesmo `libraries.txt` de
   [no/libraries.txt](no/libraries.txt). Se o índice de bibliotecas não carregar, crie um arquivo
   `libraries.txt` pelo menu da seta e cole o conteúdo do repositório.
5. Em `config.h`, deixe o prefixo `corredor-aceso-40028922` e a casa de teste. Para a demonstração, mude
   `SILENCIO_DEMO_S` para `90`.
6. Clique no botão verde de iniciar. No monitor serial aparecem `[pecas] tela ok, pingente ok`,
   `[wifi] conectando`, `[mqtt] conectado` e um `[mqtt] {...}` para cada evento. Se o Wi-Fi do Wokwi
   mostrar "IoT Gateway ... closed the connection", reinicie a simulação.
7. Abra o [painel](https://iamp5.github.io/corredor-aceso/painel/). Ele já assina o prefixo e a casa de teste.

Para mexer no circuito durante a simulação:

- PIR: clique no sensor e em `Simulate motion`.
- Botão: clique no botão verde e segure um instante, ou aperte a tecla `b` com o foco no diagrama. O
  firmware ignora toque com menos de 30 ms, que é ruído de contato. Um clique de automação, sem tempo de
  pressão, não conta.
- Tensão: gire o potenciômetro, ou clique nele e use `Home` e `End`. Ele começa em 127 V. Abaixo de
  100 V, o serial mostra `[energia] tensão cruzou 100 V`, o âmbar pisca rápido e o buzzer toca até a
  tensão voltar ou o botão.
- Temperatura: clique no DHT22 e mude a temperatura. A 34 °C ou mais, o serial mostra `[calor]` e o
  âmbar acende.
- Silêncio: com `SILENCIO_DEMO_S 90` em `config.h`, não toque em nada por 90 s. O serial mostra
  `[silêncio] demo: 90 s sem sinal de vida` e o buzzer chama até o botão.
- Queda pelo pingente: clique no MPU6050 e leve a aceleração Z de 1 g para 2 g. A tela mostra "QUEDA?" e a
  contagem de 15 s. Volte Z para 1 g e aperte o botão para cancelar, ou espere para ver `QUEDA`.
- Queda pelo sensor de teto: clique no HC-SR04 e ponha a distância em 220 cm (algo de 30 cm no chão). Depois
  de 30 s, começa a mesma pergunta. Volte para 400 cm, que é o corredor vazio.
- Wi-Fi: a chave começa na posição "Wi-Fi ligado". Ao mudar a chave de posição, o serial mostra
  `[wifi] chave desligada`, e cada evento
  seguinte mostra `sem rede: guardado (N no buffer)`. Ao voltar a chave, o serial mostra
  `[buffer] N evento(s) reenviados com a hora original`.

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

A rodada revelou três defeitos, corrigidos na versão atual: a chave do Wi-Fi começava na
posição desligada, o potenciômetro começava no fim da escala (o atributo `value` vai de 0 a 1023) e a
primeira `TELEMETRIA` de cada boot saía marcada como bufferizada, porque nascia antes de a rede subir.

Não foram executados a ponte do Mosquitto da VM até o Motor com este nó e o Wokwi CLI, que exige um token de conta.

As reações locais de calor, energia e silêncio entraram em 18/11, depois dessa rodada.

### O que rodou no Wokwi em 18/11, até o Motor no notebook

Em 18/11/2026, o firmware atual rodou no wokwi.com com o prefixo `corredor-aceso-40028922`, a casa de teste e
`SILENCIO_DEMO_S 90`. A ponte do notebook levou os pacotes ao Motor do `tools/demo.sh`. Tudo está em
[docs/evidencias/iot/wokwi-2026-11-18/](../docs/evidencias/iot/wokwi-2026-11-18/):

1. `TELEMETRIA`, `MOVIMENTO` e dois `BOTAO_BEM` do nó chegaram a `motor.evento_sensor`, de 1 a 6 s depois do `ts`.
2. Um `DIA_CRITICO` publicado no Mosquitto do notebook chegou ao nó e acendeu o âmbar.
3. Potenciômetro em 0 V: alarme local, com buzzer e âmbar piscando. O botão silenciou o alarme e mandou `BOTAO_BEM`.
4. DHT22 em 35 °C: `[calor]` no serial e âmbar aceso.
5. 90 s sem sinal de vida: `[silêncio]` no serial e buzzer chamando até o botão.

Na primeira partida, o gateway de Wi-Fi do Wokwi (`wss://netwi.wokwi.com/`) fechou a conexão. O nó guardou a
`TELEMETRIA` no buffer e seguiu funcionando. Ao reiniciar a simulação, o Wi-Fi conectou.

### O que rodou no Wokwi em 18/11, com o painel

Na mesma tarde, rodaram no wokwi.com o firmware com pingente, sensor de teto e tela, e o painel servido pelo
`tools/montar-site.sh`, os dois no broker público. Os arquivos colados eram os do repositório, com
`SILENCIO_DEMO_S 90`. As provas estão em
[docs/evidencias/iot/painel-2026-11-18/](../docs/evidencias/iot/painel-2026-11-18/):

1. Ao ligar, o serial mostrou `[pecas] tela ok, pingente ok`. O painel recebeu o estado (29,5 °C, 127 V,
   corredor vazio, 1,00 g) e mostrou "Tudo bem".
2. Pingente em 2 g: a tela mostrou "CAIU?". Depois de 15 s sem toque, saiu `QUEDA`, e o painel ficou vermelho
   com "Queda avisada". O botão encerrou o alarme.
3. Pingente em 2 g e botão dentro do prazo: `[queda] cancelada: ela disse que está bem`.
4. Sensor de teto em 220 cm (30 cm de altura): depois de 30 s, `[queda] alguém deitado no chão do corredor`.
   O Wi-Fi do Wokwi tinha caído, e a `QUEDA` entrou no buffer (`sem rede: guardado`).
5. Botão "Declarar dia crítico" do painel: o nó recebeu `DIA_CRITICO`, e o painel mostrou "Dia crítico de
   calor" com o âmbar aceso.

A primeira rodada achou um defeito, corrigido antes das rodadas descritas acima: a contagem de 15 s terminava
na hora, porque a subtração `agora - perguntaEm` dava a volta quando `perguntaEm` era um pouco mais novo que
`agora`. A primeira `QUEDA` da linha do tempo do painel, às 18:11:28, é dessa versão. Também por causa da
rodada, o limite de impacto baixou de 2,5 g para 2 g, porque o controle do MPU6050 no Wokwi vai só até 2 g por
eixo.

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
