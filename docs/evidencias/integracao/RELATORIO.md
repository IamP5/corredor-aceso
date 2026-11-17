# Relatório da integração

As sete partes do Corredor Aceso foram feitas em separado: os módulos Motor, Cadastro e Guarda da API, o app Expo, o nó ESP32 com o simulador de casas, o job de ML e os documentos de QA. Este relatório mostra as partes rodando juntas no notebook, em 17/11/2026: o que foi rodado, com qual comando, o que se viu e onde está a evidência. Tudo pode ser repetido com os comandos daqui.

Os dados de pessoas são simulados. Os tokens das casas aparecem nos logs só pelo número da casa na semente.

## Resumo

| Cena | Situação | Evidência |
|---|---|---|
| Subida: papéis, migrações, API e carga duas vezes | verificada | `demo-completa.log`, `carga-impressao-do-banco.txt` |
| 1. Dia crítico pelo job de ML, casas esperando, movimento tirando cards | verificada | `demo-completa.log`, cenas 1 e "movimento" |
| 2. Aparelho mudo fica cinza, visita vale só na janela atual, nunca verde | verificada | `demo-completa.log`, cena 2 |
| 3. Mensagem bloqueada, tradução do OpenRouter, painel muda | verificada | `demo-completa.log`, cena 3, e `07-mensagem-bloqueada.png` |
| Queda de energia na casa eletrodependente | verificada | `demo-completa.log` |
| Desfechos: "não abriu", "estava fora", "estava bem" e resposta do vizinho | verificada | `demo-completa.log`, `05-visita.png`, `06-casa-depois-da-visita.png` |
| Reenvio do buffer gravado uma vez | verificada | `demo-completa.log` |
| App web contra a API real, todas as telas | verificada | `01` a `11-*.png`, `telas.log` |
| Fila do app com a API parada, entregue uma vez | verificada | `08-fila-api-fora.png`, `09-fila-enviada.png`, `telas.log` |
| Invariante do falso verde no sistema integrado | verificada: 3 sementes, 4578 casas verdes conferidas, 0 violação | `invariante.log` |
| Nó do Wokwi falando com esta API | não rodada | o nó só roda no wokwi.com, contra o broker público |
| Provisionamento da VM e `infra/teste-de-falha.sh` | não rodados | a integração rodou no notebook, sem VM |
| App no celular contra a API real | não rodado | só o alvo web |

## Como repetir

Pré-requisitos: Docker, SDK do .NET 10, Node 22.13 ou mais novo, Python 3.11 ou mais novo e o Google Chrome (para os prints). O `.env` da raiz com `OPENROUTER_API_KEY` liga a tradução do LLM. Sem ele, a Guarda usa a frase fixa e a cena 3 acusa isso.

```bash
export DOCKER_API_VERSION=1.43   # só em máquina com Docker antigo, como a desta rodada
bash tools/demo.sh               # sobe do zero e roda todas as cenas (cerca de 10 minutos)
```

Para os prints do app, com a API ainda de pé:

```bash
bash tools/demo.sh app           # num terminal: o app web em http://localhost:8081, ligado à API
bash tools/demo.sh telas         # noutro: dirige o app e salva os prints nesta pasta
```

E a invariante, que precisa de banco novo e do simulador parado:

```bash
bash tools/demo.sh subir && bash tools/demo.sh invariante 250 20261119
```

`bash tools/demo.sh parar` para a API, o simulador e o app. `parar --tudo` para também os contêineres.

O que o `tools/demo.sh` faz, na ordem:

1. `docker compose -f compose.dev.yml up -d`, o mesmo do `npm run dev:deps`.
2. Recria o banco `corredor_demo`, separado do `corredor` do `npm run dev:api`. Para cada módulo, roda `infra/config/bd-modulo.sql` como superusuário, que cria `corredor_<modulo>_migra`, dono do schema, e `corredor_<modulo>`, o papel da API.
3. Gera o SQL das migrações com `tools/migracoes.sh` e aplica cada `<modulo>.sql` duas vezes como `corredor_<modulo>_migra`, igual ao `tools/verificar-api.sh` do CI. Esta máquina não tem `psql`: o script usa o `psql` do contêiner.
4. Roda `Corredor.Api --semear api/semente/casas.json` duas vezes e compara os tokens e uma impressão de cada tabela.
5. Sobe a API em `Development` na porta 5080, conectada com os papéis `corredor_<modulo>`. No macOS, a porta 5000 é do receptor do AirPlay, que responde 403 no lugar da API.
6. Roda `tools/demo/cenas.py`, que chama a API por HTTP, liga o simulador real de `iot/simulador/` contra o Mosquitto e roda `ml/prever_hoje.py`.

O relógio de demo só anda para frente, então cada `subir` recria o banco. Depois de um salto do relógio, toda casa parece muda até o próximo heartbeat, porque `UltimoPacoteEm` é a hora do servidor. As cenas esperam 25 segundos depois de cada salto. Quem grava o vídeo precisa da mesma espera.

## 1. Subida e carga da demo

Comando: `bash tools/demo.sh subir`.

Visto em `demo-completa.log`:

- Os seis papéis foram criados e os três scripts de migração foram aplicados duas vezes, sem erro, cada um pelo dono do seu schema.
- Primeira carga: 40 idosos cadastrados, 26 vizinhos indicados, 40 tokens. Segunda carga: 0 cadastrados, 40 que já existiam, 0 vizinhos.
- Os 40 tokens das duas cargas são iguais, e as 15 tabelas dos três schemas têm o mesmo número de linhas e o mesmo md5 do conteúdo antes e depois da segunda carga. A impressão está em `carga-impressao-do-banco.txt`.
- A API respondeu 200 em `/healthz/ready`, com os três `DbContext` e o MQTT.

## 2. Cena 1: o dia crítico abre o turno e o movimento tira os cards

Comando: `bash tools/demo.sh cenas dia-critico desfechos movimento`.

Passos e o que se viu:

1. Relógio às 05h00 (`PUT /api/demo/relogio`). Simulador no cenário `demo`, com os papéis da semente.
2. Antes do dia crítico: turno fechado, 38 casas `Aguardando`, 0 verdes. Sem turno não existe verde.
3. `python3 ml/prever_hoje.py --dia-das-variaveis 2025-12-29`: regra da noite quente disparada (mínimas de 22,6 e 24,3 °C), `origem` `REGRA_BASE`, `PUT /api/dias-criticos/2026-11-17` com 200, sem token de ACS. O turno abriu às 05h00.
4. O Motor publicou `DIA_CRITICO` no tópico de comando, e o simulador registrou o comando recebido.
5. As quatro casas acamadas (2, 11, 21 e 31) entram vermelhas na abertura, como diz a seção 5.2.
6. Em 90 segundos, com o movimento chegando, 24 casas ficaram verdes.
7. Relógio às 10h00: C, D e E (casas 5, 6 e 7), em silêncio desde cedo, ficaram vermelhas por `SemMovimentoNaJanela`. A casa B (4) avisou a saída e ficou `Aguardando` com `saidaAvisada: true`.
8. Na cena "movimento", o simulador passa ao cenário `manha`, em que toda casa volta a ter gente passando. Cada casa que estava vermelha recebeu um `MOVIMENTO` e saiu dos pendentes para `Verde`. O log lista casa por casa, com o número de movimentos depois das 10h na linha do tempo.

## 3. Desfechos da visita e resposta do vizinho

Na mesma rodada, às 10h00 (cena `desfechos`):

- Casa 7 (E), `NaoAbriu`: 201, e a casa continua `Vermelho`, pendente, sem atendimento.
- Casa 7, depois `EstavaFora`: 201, e a casa fica `Vermelho` com `atendida: true`. Ausência confirmada atende o alerta, mas a casa nunca vira verde. A linha do tempo mostra só a confirmação `Acs` `AusenciaConfirmada`.
- Casa 6 (D), `EstavaBem`: a casa fica `Verde`.
- Casa 5 (C): o `VigiaDoRadar` pediu contato, o Cadastro registrou o aviso ao vizinho, e `POST /api/avisos/{id}/resposta` com `EstaBem` deixou a casa `Verde`. Uma resposta diferente depois deu 409 `AVISO_JA_RESPONDIDO`.

## 4. Cena 2: o aparelho mudo fica cinza

Comando: `bash tools/demo.sh cenas mudo` (depois das cenas acima).

1. Casa 15, `Sozinho` e não eletrodependente, `Verde`, com sinal de vida na janela.
2. Simulador no cenário `mudo --casa 15 --inicio 20`: 20 segundos de pacotes e depois nenhum.
3. Passado o `SilencioMaximo` de demo, de 1 minuto: `Cinza`, `SemHeartbeat`, pendente. Durante mais 30 segundos, seis leituras do radar, todas `Cinza`.
4. Visita `EstavaBem`: a casa fica `Cinza` com `atendida: true`. Continua `Cinza` sem pacote novo, mesmo com a pessoa vista, porque a regra 2 vem antes do sinal de vida.
5. Relógio às 15h00: na janela seguinte a visita não vale mais. A casa volta aos pendentes, com `atendida: false`.
6. O simulador volta ao cenário `manha` e a casa manda um pacote novo: ela sai do `Cinza`.

## 5. Queda de energia e reenvio do buffer

Comando: `bash tools/demo.sh cenas energia reenvio`.

Queda de energia na casa 2 (acamada, com concentrador), cenário `queda-de-energia --casa 2`:

- Durante a queda, a casa ficou `Vermelho` por `EnergiaEletrodependente`.
- A linha do tempo mostra as tensões 127, 0 e 127 V. Com a luz de volta, a casa continuou vermelha pela trava da condição (b) da regra 1.
- O vizinho da casa 2 recebeu um aviso por `EnergiaEletrodependente`.
- Só a visita `EstavaBem` da ACS tirou a casa do vermelho.

Reenvio na casa 13, cenário `reenvio --casa 13`: o Wi-Fi do nó caiu por 40 segundos. O nó guardou 5 eventos e publicou 10 pacotes com `bufferizado: true`, cada evento duas vezes, com o `seq` e o `ts` originais. A linha do tempo tem cada um dos 5 eventos uma vez só, com `bufferizado: true`.

## 6. Invariante do falso verde no sistema integrado

Comando: `bash tools/demo.sh subir && bash tools/demo.sh invariante 250 20261119`.

O script `tools/demo/invariante.py` não usa o simulador. Ele publica no Mosquitto, chama a API por HTTP e lê o radar depois de cada passo. Em cada passo ele sorteia heartbeats, movimentos, botões, telemetria com calor e com tensão zero nas casas eletrodependentes, eventos do buffer com hora antiga, aparelhos que emudecem e voltam, visitas com os quatro desfechos, respostas de vizinho e saltos do relógio de 30 segundos a 90 minutos. O dia crítico é declarado num passo sorteado, e a ACS pode encerrar o turno à tarde.

Para cada casa `Verde`, o script confere com um modelo próprio, feito só com o que ele mandou: turno aberto, nenhum alerta (pacote no último minuto, tensão boa ou visita da ACS depois da tensão baixa, sem calor de 34 °C há 2 minutos) e sinal de vida aceito pelo perfil desde o início da janela atual. Casos a menos de 3 segundos de um limite contam como indeterminados.

Resultado, em `invariante.log`, três rodadas com sementes diferentes:

| Semente | Passos | Pacotes | Visitas | Saltos do relógio | Leituras do radar | Turno | Casas verdes conferidas | Violações |
|---|---|---|---|---|---|---|---|---|
| 20261119 | 164 | 3737 | 135 | 31 | 195 | até as 18h | 1723 | 0 |
| 7 | 96 | 2705 | 72 | 21 | 117 | encerrado pela ACS | 765 | 0 |
| 42 | 209 | 5148 | 157 | 38 | 247 | até as 18h | 2090 | 0 |

Nenhuma casa ficou verde sem prova, e nenhum caso caiu na folga de 3 segundos. Cada rodada vai das 04h30 até depois das 19h do relógio de demo e termina quando o relógio passa das 19h, por isso o número de passos varia.

A primeira rodada acusou 8 casas verdes com calor. O defeito era do modelo do script: duas leituras de temperatura no mesmo segundo, uma de 34,5 °C e outra de 31 °C, e o script ordenava pelo valor. O Motor tomou como última a de 31 °C, que tem o `seq` maior e chegou depois, e não viu calor. Com o script desempatando pelo `seq`, a mesma semente deu 0 violação.

## 7. App da ACS contra a API real

Comandos: `bash tools/demo.sh app` e `bash tools/demo.sh telas`, depois das cenas `dia-critico` e `desfechos`.

O `tools/demo/telas.mjs` abre o Chrome sem janela numa tela de 390 por 844 pontos, entra com o código de acesso de desenvolvimento e navega só por toques. Cada tela leu dados reais da API:

| Print | Tela | O que mostra |
|---|---|---|
| `01-entrada.png` | Entrada | Código de acesso escondido e microárea 3 |
| `02-dia-critico.png` | Dia crítico | Turno aberto e o resumo do radar real |
| `03-radar.png` | Radar | 9 cards pendentes, com nomes da lista de nomes e motivos do Motor |
| `04-casa-e-linha-do-tempo.png` | Casa | Nome, endereço, vizinho, motivo e a linha do tempo de `GET /api/casas/{token}/linha-do-tempo` |
| `05-visita.png` | Visita | O desfecho "Não abriu, chamei o SAMU" e a observação |
| `06-casa-depois-da-visita.png` | Casa | A visita enviada, e a casa ainda urgente |
| `07-mensagem-bloqueada.png` | Mensagem | "ele mora sozinho e toma diurético" bloqueada, com a explicação e a versão segura da API |
| `08-fila-api-fora.png` | Casa, API parada | "Estava fora" registrada sem API: "Ainda não enviado" e "1 visita espera para ser enviada" |
| `09-fila-enviada.png` | Casa, API de volta | A mesma visita "Enviado", e a linha do tempo com a ausência confirmada |
| `10-encerrar.png` | Encerrar | A pergunta antes de encerrar |
| `11-turno-encerrado.png` | Turno encerrado | O resumo de `POST /api/dias-criticos/{data}/encerramento` |

Fila sem rede, em `telas.log`: a API foi parada com `bash tools/demo.sh parar-api`, a visita foi registrada no app, a API subiu de novo e o relógio de demo voltou ao ponto em que estava. A visita chegou ao banco em menos de 10 segundos, com 1 linha em `cadastro.visita`. Vinte segundos depois ainda era 1 linha.

## 8. Defeitos achados nas junções e corrigidos

Cada correção tem um teste que falha se o defeito voltar.

| Defeito | Onde estava | Correção | Teste |
|---|---|---|---|
| A carga escrevia `tokens.local.json` em `api/src/Corredor.Cadastro/Semente/`, e o simulador lia em `api/semente/`, o caminho do modelo de dados. O simulador não achava os tokens. | Cadastro | A semente foi para `api/semente/`. | `test_le_os_tokens_onde_a_carga_os_escreve`, em `iot/simulador/test_simulador.py` |
| O preflight do navegador recebia 401: o app web não falava com a API. | Host | CORS só em `Development`, com `Authorization` e `Content-Type`. | `Em_development_o_app_web_passa_pelo_preflight_com_authorization` e `Fora_de_development_nao_ha_cors` |
| O app mandava o código digitado em minúsculas como token, e a API recusava com 401. O código também aparecia na tela do dia. | App | A entrada pede o código de acesso, guarda como digitado, esconde o campo e não mostra o código. | `app/src/__testes__/cliente.test.ts` |
| A casa do teste de falha (`fa11...`) aparecia no radar da ACS como um card cinza sem nome. | Cadastro | A carga ativa essa casa na microárea 999. | `Carga_ativa_as_casas_e_a_casa_do_teste_de_falha_no_Motor_e_grava_os_tokens_sem_nome` |
| A tradução do OpenRouter chegava cortada ao painel: "impediu que dados sobre a sua saúde e a sua". O roteador `openrouter/auto` escolhia um modelo que pensa antes, e o raciocínio gastava os 200 tokens. | Guarda | Resposta com `finish_reason` `length` vira a frase fixa, e o pedido leva 1000 tokens. | `Resposta_cortada_no_limite_de_tokens_vira_a_frase_fixa` e `Pede_tokens_para_o_raciocinio_e_as_duas_frases` |
| Uma casa nunca aceitava visita: `POST /api/visitas` voltava 422 `CAMPO_INVALIDO`. O token dela, `75abf1b1e11543844512a28f04151ae5`, tem onze dígitos seguidos entre duas letras que passam no dígito do CPF, e o interceptador mascarava o `casaToken`. Achado na cena 2 de uma das rodadas, que usou essa casa. Como o token é sorteado na carga, qualquer banco pode ter uma casa assim. | Guarda | Os padrões de CPF, CNS e RG exigem que o número não esteja colado em letra ou dígito. | `Token_da_casa_com_onze_digitos_seguidos_nao_vira_cpf` e `Nenhum_token_de_casa_sorteado_vira_documento`, que sorteia 20 mil tokens e falha sem a correção |
| O `corredor-ml.service` rodaria `prever_hoje.py` sem clima de hoje, e o job sairia com 2 todo dia. | Infra | A unit passa `CORREDOR_DIA_DAS_VARIAVEIS=2025-12-29`. | `tools/conferir-infra.sh` |
| Os testes do Jest do app não rodavam no CI. | CI | `npm run test:app` no job do app. | o próprio CI |

Conferidos na junção e sem defeito:

- O Cadastro registra `IReceptorDeContato` com `AddScoped`, e o pedido de contato do Motor chega ao Cadastro: os avisos ao vizinho nasceram nas cenas. O teste `No_host_o_pedido_de_contato_do_Motor_chega_ao_Cadastro` guarda isso.
- As rotas de demo da Guarda (`/api/guarda/demo/*`) e o `PUT /api/demo/relogio` só existem em `Development`. Em `Production`, com token válido, respondem 404. Guardado pelo teste `Fora_de_development_as_rotas_de_demo_nao_existem`. Na VM local, que roda em `Development`, elas existem de propósito, para as evidências.
- O job de ML chama `PUT /api/dias-criticos/{data}` sem token, e a rota aceita.
- O contrato MQTT do simulador e o do Motor batem: os seis campos, `ts` em UTC com `Z` e os comandos `DIA_CRITICO` e `PEDIR_TOQUE`.

## 9. O que ainda não funciona ou não foi visto

- O resumo do encerramento conta todas as casas ativas do Motor, de todas as microáreas. Com a casa do teste de falha ativa, o app mostra "24 de 41 casas" em vez de 40. Corrigir pede uma decisão de domínio: o turno é do dia, e o resumo não tem microárea.
- No app, com o relógio de demo deslocado, a hora da visita aparece na hora do celular ("Estava fora às 02h04") e o resto da tela na hora do Motor ("10h02"). Em produção o deslocamento é zero e as horas batem.
- `openrouter/auto` escolhe o modelo a cada chamada. Numa rodada de teste, um modelo respondeu cortado mesmo com o raciocínio desligado. A Guarda agora troca resposta cortada pela frase fixa, mas a tradução do LLM pode não aparecer em toda rodada. Fixar um modelo no `.env` deixa a demo previsível.
- O nó do Wokwi não falou com esta API: ele publica no broker público, e a ponte do Mosquitto só existe na VM.
- A VM, o provisionamento e o `infra/teste-de-falha.sh` não rodaram nesta integração.
- O app não rodou no celular contra a API real, só no alvo web.

## 10. Arquivos desta pasta

| Arquivo | O que é |
|---|---|
| `demo-completa.log` | Saída de `bash tools/demo.sh`: subida, carga e todas as cenas |
| `carga-impressao-do-banco.txt` | Linhas e md5 de cada tabela depois da segunda carga, iguais aos da primeira |
| `invariante.log` | Saída da invariante do falso verde |
| `telas.log` | Saída de `bash tools/demo.sh telas` |
| `01-entrada.png` a `11-turno-encerrado.png` | Prints do app web contra a API real |
