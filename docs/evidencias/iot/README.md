# Evidências do nó e do simulador

Rodadas de 17/11/2026. Todos os tokens daqui são de teste: a casa de teste `fa11…` do
`corredor.conf.padrao` ou tokens sorteados na hora, que nenhum banco conhece. Nenhum arquivo tem nome,
endereço ou telefone.

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

## `fio/`: o simulador contra um Mosquitto em contêiner

Gerado por `bash iot/simulador/provar_no_fio.sh <contêiner> <porta> docs/evidencias/iot/fio`, com
`eclipse-mosquitto:2` (2.1.2). Para cada cenário:

- `<cenario>.captura.txt`: tudo que passou em `corredor/v1/#`, capturado com `mosquitto_sub -F '%U %t %p'`.
- `<cenario>.log.txt`: a saída do simulador, com o mapa de casas e cada evento guardado no buffer.
- `<cenario>.conferencia.txt`: a saída de `conferir_fio.py --log`, que termina em `PASSOU` ou `FALHOU`.

`tokens.demo.json` tem os papéis da semente com tokens sorteados, no formato que o `--semear` escreve.
