# Evidências do nó e do simulador

Rodadas de 17 e 18/11/2026. Todos os tokens daqui são de teste: a casa de teste `fa11…` do
`corredor.conf.padrao` ou tokens sorteados na hora, que nenhum banco conhece. Nenhum arquivo tem nome,
endereço ou telefone.

## `final-2026-11-18/`: o estado atual, com o nó e o painel

Rodada de 18/11/2026, das 22h32 às 22h56, com o firmware e o painel desta entrega: sem sensor de teto, com
luz de emergência e o botão "Encerrar alertas". O nó rodou no Wokwi do VS Code (`iot/no/wokwi.toml`), porque
o gateway público do wokwi.com estava fora do ar na hora. O painel foi o `site/painel/index.html`, servido
localmente com o mesmo arquivo publicado no Pages. Os dois usaram `test.mosquitto.org`, o prefixo
`corredor-aceso-40028922` e a casa de teste. Não teve API nem VM.

O firmware foi recompilado às 22h51, no meio da rodada, para o tópico `estado` contar a luz de emergência
em `luz`. O serial mostra o novo boot. As cenas de energia em diante já são do firmware corrigido.

| Arquivo | O que é |
|---|---|
| `00-circuito-no-wokwi.png` | O circuito do projeto público no wokwi.com, rodando, com a tela em "TUDO BEM". |
| `painel-01-tudo-bem.png` | O painel conectado, com os sensores do aparelho. |
| `painel-02-passou-no-corredor.png` | O PIR acendeu a luz, e `MOVIMENTO` virou o último sinal de vida. |
| `painel-03-possivel-queda.png` | Pingente em 2 g: "Possível queda", com o buzzer e o âmbar ligados. |
| `painel-04-queda.png` | Sem toque no botão em 15 s: `QUEDA` na linha do tempo e a casa em vermelho. |
| `painel-05-sem-energia.png` | Tomada em 12 V: "Sem energia", com a luz de emergência acesa e o buzzer quieto. |
| `painel-06-dia-critico.png` | O botão "Declarar dia crítico" acendeu o âmbar no aparelho. |
| `painel-07-calor.png` | DHT22 em 41,2 °C depois do "Encerrar alertas": o dia crítico apagou, e o calor seguiu o sensor. |
| `painel-08-pedido-de-toque.png` | "Pedir um toque": o buzzer chama até o botão. |
| `painel-09-reenvio-do-buffer.png` | Wi-Fi cortado pela chave, uma passagem e um toque guardados e reenviados com a hora original. |
| `serial.txt` | Monitor serial da rodada, com a hora local da captura em cada linha. |
| `broker.txt` | O que um assinante de `corredor-aceso-40028922/#` recebeu, sem o prefixo e sem heartbeats. Do tópico `estado`, só as fotos em que `alerta`, `luz`, `ambar` ou `buzzer` mudaram. |

As pastas abaixo são de rodadas anteriores. Ficam como histórico: algumas têm o sensor de teto e o alarme de
energia com bipes, que saíram do circuito.

## `wokwi/`: o firmware rodando no simulador do wokwi.com

| Arquivo | O que é |
|---|---|
| `serial.txt` | Monitor serial da rodada: conexão, `MOVIMENTO`, `BOTAO_BEM`, os dois comandos, o Wi-Fi cortado com dois eventos no buffer, o reenvio com a hora original e a queda de tensão. |
| `captura-broker-publico.txt` | O que um assinante no `broker.hivemq.com` recebeu no prefixo de teste, sem o prefixo, no formato `hora-de-chegada tópico corpo`. As cinco primeiras linhas são de um boot anterior do mesmo nó: o `seq` volta a 1 depois do reinício, com outro `ts`. |
| `conferencia.txt` | Saída de `iot/simulador/conferir_fio.py` sobre a captura. |
| `luz-acesa-pelo-pir.png` | A luz de caminho acesa pelo PIR. |
| `circuito-conectado.jpg` | O circuito rodando, conectado. |
| `wifi-cortado-luz-acesa.jpg` | Wi-Fi cortado pela chave, luz acesa e o serial com `BOTAO_BEM sem rede: guardado (2 no buffer)`. |

O sketch e o `diagram.json` colados no Wokwi tinham o mesmo SHA-256 dos arquivos do repositório, com o
`config.h` embutido e o prefixo de teste no lugar de `gs26-TROQUE-ISTO`.

## `wokwi-2026-11-18/`: o nó no wokwi.com até o Motor no notebook

Rodada de 18/11/2026, com o firmware das reações locais. Os arquivos foram colados sem mudança, inclusive
`config.h` e `libraries.txt`, com uma exceção: `SILENCIO_DEMO_S 90` no `config.h`. O prefixo era
`corredor-aceso-40028922`, e a casa era a de teste. O caminho foi Wokwi → `broker.hivemq.com` → ponte
`infra/config/dev/mosquitto-ponte.conf` → Mosquitto do `compose.dev.yml` → Motor do `tools/demo.sh`.

| Arquivo | O que é |
|---|---|
| `serial.txt` | Monitor serial: conexão, `MOVIMENTO`, o `DIA_CRITICO` vindo do notebook, o alarme de energia silenciado pelo botão, o calor e o pedido de toque por silêncio. |
| `captura-broker-publico.txt` | O que um assinante no `broker.hivemq.com` recebeu no prefixo da equipe, sem os heartbeats. |
| `motor-evento-sensor.txt` | As linhas que o Motor gravou em `motor.evento_sensor` para a casa de teste, com `ocorrido_em` e `recebido_em`. |

## `painel-2026-11-18/`: o nó no wokwi.com com o painel web

Rodada de 18/11/2026, com pingente MPU6050, sensor de teto HC-SR04 e tela OLED. Os arquivos foram colados
sem mudança, com uma exceção: `SILENCIO_DEMO_S 90` no `config.h`. O painel (`site/painel/`) foi servido pelo
`tools/montar-site.sh`. Os dois usaram o broker público, sem API.

| Arquivo | O que é |
|---|---|
| `serial.txt` | Monitor serial de dois boots: queda pelo pingente encerrada e cancelada pelo botão, queda pelo sensor de teto guardada no buffer com o Wi-Fi do Wokwi caído, e o `DIA_CRITICO` enviado pelo painel. |
| `painel-linha-do-tempo.txt` | A linha do tempo que o painel mostrou na mesma rodada. A `QUEDA` das 18:11:28 é da versão com o defeito da contagem, corrigido antes do serial acima (ver `iot/README.md`). |

## `fio/`: o simulador contra um Mosquitto em contêiner

Gerado por `bash iot/simulador/provar_no_fio.sh <contêiner> <porta> docs/evidencias/iot/fio`, com
`eclipse-mosquitto:2` (2.1.2). Para cada cenário:

- `<cenario>.captura.txt`: tudo que passou em `corredor/v1/#`, capturado com `mosquitto_sub -F '%U %t %p'`.
- `<cenario>.log.txt`: a saída do simulador, com o mapa de casas e cada evento guardado no buffer.
- `<cenario>.conferencia.txt`: a saída de `conferir_fio.py --log`, que termina em `PASSOU` ou `FALHOU`.

`tokens.demo.json` tem os papéis da semente com tokens sorteados, no formato que o `--semear` escreve.
