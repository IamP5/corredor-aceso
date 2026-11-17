# Casos de teste do Corredor Aceso

Arquivo gerado por `docs/qa/ferramentas/gerar.py` a partir de `docs/qa/fonte/casos_*.py`. Edite a fonte, não este arquivo.

São 222 casos funcionais de caixa preta: 91 positivos e 131 negativos. Cada caso nomeia a Feature que verifica, o objetivo, a preparação, as entradas, as saídas esperadas e os passos numerados. A linha **Caso do modelo** aponta o número do caso na seção 7 de `docs/arquitetura/03-modelo-de-dominio.md` quando o caso parte de lá.

Convenções que valem para todos os casos:

- Todo dado de pessoa é simulado. Os nomes, endereços e telefones vêm da carga da demo (`api/semente/casas.json`), com DDD 00.
- "Dia crítico de 18/11" quer dizer `PUT /api/dias-criticos/2026-11-18` às 05h00, com o relógio de demo movido por `PUT /api/demo/relogio`.
- As rotas ficam sob `/api` e exigem a ACS `acs-demo-01` autenticada na microárea 3, menos `GET /api/painel` e as rotas que o caso diz.
- Um pacote MQTT é publicado em `corredor/v1/{casaToken}/evento` no Mosquitto local, com o formato da seção 6.2 do modelo de domínio.
- Os horários são de São Paulo (-03:00), salvo quando o caso diz UTC. Com o relógio de demo movido, todo horário citado é o que o Motor enxerga, já no relógio de demo. Quem publica e quem registra manda a hora real: o servidor soma o deslocamento ao `ts` e à `ocorridaEm` (seção 5.9 do modelo de domínio). Para um evento "às 07h00", o simulador (`iot/simulador`) publica com o `ts` real que vira 07h00; para uma visita "às 10h30", registre-a quando o relógio de demo marca 10h30.
- Os parâmetros são os valores de demo da seção 5.9 do modelo de domínio (silêncio máximo de 1 minuto, calor por 2 minutos), salvo quando o caso diz "produção". Os casos escritos em horas de produção valem igual com o relógio de demo.

## F01 Cadastro do idoso pela ACS

### TC-F01-01

**Cadastro válido responde 201 e a casa nasce sem token**

**Feature:** F01

**Tipo:** Positivo

**Objetivo:** Provar que o cadastro com nome, endereço e perfil grava o idoso inteiro e que a casa ainda não tem token.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador

**Entradas:**

- `POST /api/idosos` com `microareaId` 3, `nome` `Amélia Cedro`, `endereco` `Rua do Sabiá, 99 - Vila Exemplo`, `perfil` `Sozinho` não eletrodependente, `condicoes` e `medicamentos`

**Saídas esperadas:**

- 201 com `id` do idoso
- `nome` e `casa_endereco` gravados inteiros
- `casa_token` nulo
- A pessoa fora de `GET /api/lista-de-nomes?microarea=3`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `POST /api/idosos` com `microareaId` 3, `nome` `Amélia Cedro`, `endereco` `Rua do Sabiá, 99 - Vila Exemplo`, `perfil` `Sozinho` não eletrodependente, `condicoes` e `medicamentos` | API responde 201 com um `id` em formato UUID |
| 2 | Consultar a linha do idoso no schema `cadastro` pelo `id` | `nome` e `casa_endereco` iguais aos enviados, sem máscara; `casa_token` nulo |
| 3 | Chamar `GET /api/lista-de-nomes?microarea=3` | Nenhum item traz `Amélia Cedro`, porque a casa não tem token |

**Cobre:** `POST /idosos`, INV-Idoso-9, INV-Idoso-10

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/CadastrarIdosoTestes.cs::CorpoValido_Responde201_CasaSemToken` (proposto)

### TC-F01-02

**Cadastro sem nome ou só com espaços responde 422 NOME_OBRIGATORIO**

**Feature:** F01

**Tipo:** Negativo

**Objetivo:** Provar que o nome é obrigatório e que espaços em branco não contam como nome.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Contagem de idosos da microárea 3 anotada (40)

**Entradas:**

- Corpo válido sem o campo `nome`
- Corpo válido com `nome` igual a três espaços

**Saídas esperadas:**

- 422 com `codigo` `NOME_OBRIGATORIO` nas duas chamadas
- A mensagem não repete o valor enviado
- A contagem de idosos continua 40

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `POST /api/idosos` sem `nome` | API responde 422 `NOME_OBRIGATORIO` |
| 2 | Enviar `POST /api/idosos` com `nome` de três espaços | API responde 422 `NOME_OBRIGATORIO` |
| 3 | Contar os idosos da microárea 3 no banco | Continuam 40; nenhum idoso foi gravado |

**Cobre:** `POST /idosos`, INV-Idoso-9

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 9: nome vazio` (existe)

### TC-F01-03

**Cadastro sem endereço responde 422 ENDERECO_OBRIGATORIO**

**Feature:** F01

**Tipo:** Negativo

**Objetivo:** Provar que o endereço da casa é obrigatório no cadastro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Contagem de idosos da microárea 3 anotada (40)

**Entradas:**

- Corpo válido sem o campo `endereco`

**Saídas esperadas:**

- 422 com `codigo` `ENDERECO_OBRIGATORIO`
- A contagem de idosos continua 40

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `POST /api/idosos` sem `endereco` | API responde 422 `ENDERECO_OBRIGATORIO` |
| 2 | Contar os idosos da microárea 3 no banco | Continuam 40; nenhum idoso foi gravado |

**Cobre:** `POST /idosos`, INV-Idoso-9

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 9: endereco ausente` (existe)

### TC-F01-04

**O mesmo corpo de cadastro enviado duas vezes cria dois idosos**

**Feature:** F01

**Tipo:** Positivo

**Objetivo:** Provar que `CadastrarIdoso` não tem chave de idempotência: cada chamada é um fato novo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador

**Entradas:**

- `POST /api/idosos` com `microareaId` 3, `nome` `Amélia Cedro`, `endereco` `Rua do Sabiá, 99 - Vila Exemplo`, `perfil` `Sozinho` não eletrodependente, `condicoes` e `medicamentos`, enviado duas vezes

**Saídas esperadas:**

- Dois 201 com `id` diferentes
- Dois idosos gravados, os dois sem token

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o corpo pela primeira vez | API responde 201 com um `id` |
| 2 | Enviar o mesmo corpo de novo | API responde 201 com outro `id` |
| 3 | Contar os idosos da microárea 3 no banco | São 42, e os dois novos têm `casa_token` nulo |

**Cobre:** `POST /idosos`, IDEM-Cadastro-CadastrarIdoso

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/CadastrarIdosoTestes.cs::CorpoRepetido_CriaDoisIdosos` (proposto)

### TC-F01-05

**Rodar a carga da demo duas vezes não duplica idoso nem troca token**

**Feature:** F01

**Tipo:** Negativo

**Objetivo:** Provar que só a carga informa o `id` e que repetir a carga pula o idoso que já existe.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Acesso de leitura ao Postgres da demo, como observador
- `api/semente/tokens.local.json` da primeira carga guardado

**Entradas:**

- Segunda execução de `Corredor.Api --semear api/semente/casas.json`

**Saídas esperadas:**

- 40 idosos, com os mesmos `id`
- Os 40 tokens iguais aos da primeira carga
- 40 casas ativas no Motor

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `--semear` de novo | O comando termina sem erro |
| 2 | Contar os idosos da microárea 3 e comparar os `id` com `casas.json` | Continuam 40, com os mesmos `id` |
| 3 | Comparar o novo `tokens.local.json` com o guardado | Os 40 tokens são iguais, porque o consentimento vigente devolve o mesmo token |
| 4 | Contar as casas ativas em `motor.casa_monitorada` | Continuam 40 |

**Cobre:** IDEM-Cadastro-CadastrarIdoso

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/CargaDaDemoTestes.cs::SemearDuasVezes_MesmoBanco` (proposto)

### TC-F01-06

**Cadastro não grava CPF nem data de nascimento**

**Feature:** F01

**Tipo:** Negativo

**Objetivo:** Provar que o cadastro não tem CPF, RG, CNS nem data de nascimento e que um CPF digitado não fica em claro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador

**Entradas:**

- Corpo válido com os campos extras `cpf` `123.456.789-00` e `dataDeNascimento` `1950-01-01`
- Corpo válido com `condicoes` `CPF 123.456.789-00, usa bengala`

**Saídas esperadas:**

- Nenhuma coluna do idoso guarda `cpf` ou data de nascimento
- `condicoes` gravada com o CPF mascarado como `***.456.***-00`; nome e endereço inteiros

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o corpo com `cpf` e `dataDeNascimento` | A API não grava os dois campos, que não existem no contrato. O modelo não diz se o campo extra é ignorado ou recusado; anotar o status observado |
| 2 | Se houve 201, ler a linha do idoso e as colunas da tabela `cadastro.idoso` | Não existe coluna de CPF, RG, CNS ou data de nascimento, e o valor `123.456.789-00` não aparece em nenhuma coluna |
| 3 | Enviar o corpo com o CPF dentro de `condicoes` | API responde 201 |
| 4 | Ler `condicoes` do idoso no banco | O texto traz `***.456.***-00`, nunca o CPF em claro; `nome` e `casa_endereco` inteiros |

**Cobre:** `POST /idosos`, INV-Idoso-10

**Caso do modelo:** CT-38

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::caso 38: CPF em claro em condicoes nao entra no banco` (existe)

## F02 Consentimento na UBS e ativação da casa

### TC-F02-01

**Consentimento gera token de 32 hex e a casa entra Cinza no radar**

**Feature:** F02

**Tipo:** Positivo

**Objetivo:** Provar que o token nasce no consentimento, que P1 ativa a casa no Motor e que a casa sem pacote entra cinza.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento
- Nenhum nó publica com o token novo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `POST /api/idosos/{id}/consentimento` às 08h00 com `versaoDoTermo` `2026-11`, `avisarVizinho` verdadeiro e `avisarFamilia` falso
- Relógio em 09h00

**Saídas esperadas:**

- 200 com `casaToken` de 32 caracteres hexadecimais minúsculos
- Consentimento gravado com `colhido_por` `acs-demo-01`, a data das 08h00 e a versão `2026-11`
- Casa nova em `pendentes`, `Cinza`, `SemHeartbeat`, `desde` 08h00, `ultimoPacoteEm` nulo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 08h00 e enviar o consentimento | API responde 200 com `casaToken` que casa com `^[0-9a-f]{32}$` |
| 2 | Ler o consentimento no banco | Uma linha vigente com `colhido_por` `acs-demo-01`, data 08h00, versão `2026-11`, sem data de revogação |
| 3 | Ler `motor.casa_monitorada` pelo token | Casa ativa na microárea 3, perfil `Sozinho`, `ativada_em` 08h00 |
| 4 | Mover o relógio para 09h00 e chamar `GET /api/radar?microarea=3` | O card do token novo está em `pendentes`, `Cinza`, desde 08h00, com `ultimoPacoteEm` nulo |

**Cobre:** `POST /idosos/{id}/consentimento`, INV-Idoso-1, INV-Idoso-3, INV-Idoso-4, NC-6, P1

**Caso do modelo:** CT-15

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::Consentimento_GeraToken_AtivaCasaNoMotor` (proposto)

### TC-F02-02

**Idoso sem consentimento não tem token nem aparece na lista de nomes e no radar**

**Feature:** F02

**Tipo:** Negativo

**Objetivo:** Provar que sem consentimento vigente não existe token, nem casa no Motor, nem nome no celular.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento

**Entradas:**

- `GET /api/lista-de-nomes?microarea=3`
- `GET /api/radar?microarea=3`

**Saídas esperadas:**

- `casa_token` nulo e nenhuma linha de consentimento
- A pessoa fora da lista de nomes
- Nenhuma casa nova no Motor e no radar

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Ler o idoso novo e seus consentimentos no banco | `casa_token` nulo; nenhum consentimento |
| 2 | Chamar `GET /api/lista-de-nomes?microarea=3` | 40 itens, todos da carga; `Amélia Cedro` não aparece |
| 3 | Contar as casas de `motor.casa_monitorada` e os cards do radar | Continuam 40 casas; nenhum card novo |

**Cobre:** INV-Idoso-1, NC-6, P1

**Caso do modelo:** CT-32

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::Caso32_SemConsentimento_SemToken_ForaDaLista` (proposto)

### TC-F02-03

**Repetir o consentimento vigente devolve o mesmo token e repete a ativação**

**Feature:** F02

**Tipo:** Positivo

**Objetivo:** Provar que `RegistrarConsentimento` é idempotente por `IdosoId` e que `AtivarCasa` converge pelo token.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 13 (`Sozinho`) com consentimento vigente desde a carga
- Token da casa 13 lido em `tokens.local.json`

**Entradas:**

- `POST /api/idosos/{id}/consentimento` da casa 13, igual ao da carga

**Saídas esperadas:**

- 200 com o mesmo `casaToken` da carga
- Um consentimento vigente
- Uma casa no Motor, com o mesmo `ativada_em`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o consentimento da casa 13 | API responde 200 com o token igual ao de `tokens.local.json` |
| 2 | Contar os consentimentos vigentes da casa 13 | Um só |
| 3 | Ler `motor.casa_monitorada` pelo token | Uma linha, ativa, com `ativada_em` igual ao de antes |

**Cobre:** `POST /idosos/{id}/consentimento`, INV-Idoso-2, INV-Idoso-5, IDEM-Cadastro-RegistrarConsentimento, IDEM-Motor-AtivarCasa

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::ConsentimentoVigente_DevolveMesmoToken` (proposto)

### TC-F02-04

**Motor fora do ar: 503 MOTOR_INDISPONIVEL e a repetição ativa a casa sem token novo**

**Feature:** F02

**Tipo:** Negativo

**Objetivo:** Provar que o Cadastro grava antes de chamar o Motor e que repetir o comando conserta sem criar segundo consentimento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento
- Motor indisponível só para a chamada de `AtivarCasaAsync`. O modelo não diz como provocar a falha; o teste automatizado usa um `IMotorDeCasas` que lança erro

**Entradas:**

- `POST /api/idosos/{id}/consentimento` com o Motor fora
- A mesma chamada com o Motor de volta

**Saídas esperadas:**

- 503 `MOTOR_INDISPONIVEL` na primeira chamada
- Consentimento e token gravados no Cadastro, casa ausente no Motor
- 200 na repetição com o mesmo token; um consentimento vigente; uma casa ativa no Motor

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o consentimento com o Motor fora | API responde 503 `MOTOR_INDISPONIVEL`, sem dado pessoal na mensagem |
| 2 | Ler o idoso e `motor.casa_monitorada` | O idoso tem um consentimento vigente e um token; o Motor não tem a casa |
| 3 | Religar o Motor e repetir a chamada | API responde 200 com o token já gravado |
| 4 | Contar consentimentos vigentes e casas com o token | Um consentimento vigente e uma casa ativa |

**Cobre:** `POST /idosos/{id}/consentimento`, INV-Idoso-2, INV-Idoso-5, IDEM-Cadastro-RegistrarConsentimento, IDEM-Motor-AtivarCasa

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ConsentimentoTestes.cs::MotorIndisponivel_503_RepetirAtivaComMesmoToken` (proposto)

### TC-F02-05

**Reconciliação ao iniciar ativa no Motor a casa que ficou de fora, com o mesmo token**

**Feature:** F02

**Tipo:** Positivo

**Objetivo:** Provar que P22 reenvia `AtivarCasaAsync` para todo idoso com token e conserta a casa ausente no Motor.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Idoso no estado do segundo passo do TC-F02-04: token gravado, casa ausente no Motor

**Entradas:**

- Reinício da API

**Saídas esperadas:**

- Casa ativa no Motor com o token já gravado e a microárea 3
- Nenhum token novo no Cadastro
- As 40 casas da carga com o mesmo token e a mesma microárea

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Anotar o token do idoso e os 40 tokens de `tokens.local.json` | Tokens anotados |
| 2 | Reiniciar a API e esperar o host subir | A API responde no Swagger |
| 3 | Ler `motor.casa_monitorada` pelo token do idoso | Casa ativa, microárea 3, com o token anotado |
| 4 | Comparar os tokens e as microáreas do Cadastro e do Motor com os anotados | Nenhum token mudou; nenhuma microárea mudou; 41 casas ativas |

**Cobre:** P22, IDEM-Motor-AtivarCasa, INV-CasaMonitorada-1

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReconciliacaoComMotorTestes.cs::AoIniciar_AtivaCasaAusente_SemTokenNovo` (proposto)

### TC-F02-06

**Reconciliação não ativa idoso sem token nem reabre casa revogada**

**Feature:** F02

**Tipo:** Negativo

**Objetivo:** Provar que P22 só reenvia a ativação de idoso com token e que a casa desativada continua desativada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento
- Casa 16 com consentimento revogado por `DELETE /api/idosos/{id}/consentimento`, token antigo anotado

**Entradas:**

- Reinício da API

**Saídas esperadas:**

- Nenhuma casa nova no Motor para o idoso sem token
- A casa do token antigo da casa 16 continua com `desativada_em`
- Nenhum token ou microárea mudou

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Reiniciar a API | A API responde no Swagger |
| 2 | Contar as casas ativas no Motor | 39: as 40 da carga menos a casa 16; nenhuma casa para o idoso novo |
| 3 | Ler a linha do token antigo da casa 16 em `motor.casa_monitorada` | `desativada_em` preenchido, sem eventos nem confirmações |
| 4 | Chamar `GET /api/radar?microarea=3` | Nenhum card com o token antigo da casa 16 |

**Cobre:** P22, IDEM-Motor-AtivarCasa, INV-CasaMonitorada-1

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReconciliacaoComMotorTestes.cs::AoIniciar_IgnoraIdosoSemToken` (proposto)

### TC-F02-07

**Token não deriva de nome e a ACS e a data do consentimento não vêm do corpo**

**Feature:** F02

**Tipo:** Negativo

**Objetivo:** Provar que dois idosos iguais recebem tokens diferentes e que o corpo não falsifica quem colheu nem a data.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Dois idosos novos com o mesmo `nome` `Amélia Cedro` e o mesmo endereço, sem consentimento

**Entradas:**

- Consentimento do primeiro idoso às 09h00
- Consentimento do segundo idoso às 09h00, com os campos extras `colhidoPor` `acs-falsa` e `data` `2026-12-25`

**Saídas esperadas:**

- Dois tokens diferentes, sem relação com nome ou endereço
- O segundo consentimento gravado com `colhido_por` `acs-demo-01` e a data das 09h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o consentimento do primeiro idoso | API responde 200 com um token |
| 2 | Enviar o consentimento do segundo idoso com `colhidoPor` e `data` no corpo | API responde 200 com outro token |
| 3 | Comparar os dois tokens | São diferentes; nenhum contém trecho de nome, endereço ou documento |
| 4 | Ler o consentimento do segundo idoso no banco | `colhido_por` `acs-demo-01`, que vem da autenticação; data 09h00, nunca no futuro |

**Cobre:** INV-Idoso-3, INV-Idoso-4

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::o mesmo token em dois idosos` (existe)

## F03 Revogação do consentimento

### TC-F03-01

**Revogar apaga token, vizinho, avisos e eventos e mantém as visitas**

**Feature:** F03

**Tipo:** Positivo

**Objetivo:** Provar o efeito completo da revogação no Cadastro e, por P4, no Motor.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 6 (`Sozinho`, vizinho `Conceição Jabuticabeira`) com eventos do dia
- Às 10h00 a casa 6 ficou vermelha e o Cadastro registrou um aviso ao vizinho (`GET /api/avisos?microarea=3`)
- Visita `EstavaBem` da casa 6 registrada às 10h12

**Entradas:**

- `DELETE /api/idosos/{id}/consentimento` da casa 6 às 10h30

**Saídas esperadas:**

- 204
- Token da casa apagado e vizinho removido
- Avisos daquele token apagados
- Casa desativada no Motor, sem eventos nem confirmações
- A visita das 10h12 continua gravada

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h30 e enviar a revogação | API responde 204 |
| 2 | Chamar `GET /api/lista-de-nomes?microarea=3` | 39 itens; nenhum com o token da casa 6 nem com `Iracema Sibipiruna` |
| 3 | Chamar `GET /api/radar?microarea=3` e `GET /api/avisos?microarea=3` | Nenhum card e nenhum aviso com o token da casa 6 |
| 4 | Ler o idoso, o consentimento e o vizinho no banco | `casa_token` nulo; consentimento com data de revogação; nenhum vizinho |
| 5 | Ler `motor.casa_monitorada`, `motor.evento_sensor` e `motor.confirmacao` pelo token antigo | Casa com `desativada_em` 10h30; zero eventos; zero confirmações |
| 6 | Ler as visitas do idoso da casa 6 | A visita das 10h12 continua lá |

**Cobre:** `DELETE /idosos/{id}/consentimento`, INV-Idoso-8, P4, INV-CasaMonitorada-3, INV-Visita-6, IDEM-Cadastro-RevogarConsentimento, IDEM-Motor-DesativarCasa

**Caso do modelo:** CT-33

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RevogacaoTestes.cs::Caso33_Revogar_ApagaTokenAvisos_DesativaCasa_MantemVisitas` (proposto)

### TC-F03-02

**Casa desativada recusa pacote, visita e linha do tempo do token antigo**

**Feature:** F03

**Tipo:** Negativo

**Objetivo:** Provar que a desativação é definitiva: nada novo é gravado para o token antigo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 6 revogada como no TC-F03-01, token antigo anotado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento` de `MOVIMENTO` com o token antigo
- `POST /api/visitas` `EstavaBem` com o token antigo
- `GET /api/casas/{token antigo}/linha-do-tempo`

**Saídas esperadas:**

- Nenhum evento gravado e `ultimo_pacote_em` sem mudança
- Visita recusada com 422 `TOKEN_INATIVO` e nenhuma confirmação
- Linha do tempo 404 `CASA_DESCONHECIDA`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar um `MOVIMENTO` com `ts` de agora para o token antigo | O Motor descarta; nenhuma linha em `motor.evento_sensor`; `ultimo_pacote_em` igual |
| 2 | Enviar a visita `EstavaBem` com o token antigo | API responde 422 `TOKEN_INATIVO`; nenhuma linha em `motor.confirmacao` |
| 3 | Pedir a linha do tempo do token antigo | API responde 404 `CASA_DESCONHECIDA` |

**Cobre:** INV-CasaMonitorada-3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::CasaDesativada_RecusaPacoteEConfirmacao` (proposto)

### TC-F03-03

**Revogar de novo ou revogar idoso sem consentimento responde 204 e nada muda**

**Feature:** F03

**Tipo:** Negativo

**Objetivo:** Provar que `RevogarConsentimento` e `DesativarCasa` são idempotentes e que P4 só age com consentimento vigente.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 6 revogada como no TC-F03-01
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento

**Entradas:**

- Segundo `DELETE /api/idosos/{id}/consentimento` da casa 6
- `DELETE` do idoso novo, que nunca consentiu
- `DELETE` com um `id` que não existe

**Saídas esperadas:**

- 204 nas duas primeiras chamadas
- Data de revogação e `desativada_em` iguais às de antes
- Nenhuma casa criada ou desativada para o idoso novo
- 404 `IDOSO_DESCONHECIDO` para o `id` inexistente (seção 7.2 do modelo de dados)

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Anotar a data de revogação da casa 6 e o `desativada_em` do token antigo | Valores anotados |
| 2 | Revogar a casa 6 de novo | API responde 204; os dois valores continuam iguais; a visita continua gravada |
| 3 | Revogar o idoso novo | API responde 204; ele continua sem consentimento, sem token e sem casa no Motor |
| 4 | Revogar um `id` aleatório | API responde 404 `IDOSO_DESCONHECIDO` |

**Cobre:** `DELETE /idosos/{id}/consentimento`, P4, IDEM-Cadastro-RevogarConsentimento, IDEM-Motor-DesativarCasa, INV-Visita-6

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RevogacaoTestes.cs::RevogarSemVigente_NaoFazNada` (proposto)

### TC-F03-04

**Consentimento novo depois da revogação não traz de volta o token antigo nem o vizinho**

**Feature:** F03

**Tipo:** Negativo

**Objetivo:** Provar que um consentimento novo gera token novo e que o vizinho removido não volta sozinho.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 6 revogada como no TC-F03-01, token antigo anotado

**Entradas:**

- `POST /api/idosos/{id}/consentimento` da casa 6

**Saídas esperadas:**

- 200 com token diferente do antigo
- Casa nova ativa no Motor; a linha do token antigo continua desativada
- Na lista de nomes, a casa 6 com o token novo e `vizinho` nulo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o consentimento da casa 6 | API responde 200 com um token diferente do anotado |
| 2 | Ler as duas linhas em `motor.casa_monitorada` | Token novo ativo; token antigo com `desativada_em` |
| 3 | Chamar `GET /api/lista-de-nomes?microarea=3` | A casa 6 aparece com o token novo e `"vizinho": null`; o token antigo não aparece |
| 4 | Contar os consentimentos da casa 6 | Dois: o revogado, guardado com a data, e o novo vigente |

**Cobre:** INV-Idoso-8, INV-CasaMonitorada-3

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 2: revogado fica guardado e um novo vigente entra` (existe)

### TC-F03-05

**Revogação e expurgo de 24 horas não apagam a visita**

**Feature:** F03

**Tipo:** Negativo

**Objetivo:** Provar que as visitas ficam como proteção da ACS, mesmo velhas e mesmo depois da revogação.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visita `EstavaBem` da casa 14 registrada em 18/11 às 10h00

**Entradas:**

- Relógio em 19/11 às 11h00 e `Expurgo` rodado
- `DELETE /api/idosos/{id}/consentimento` da casa 14

**Saídas esperadas:**

- Confirmação da visita expurgada do Motor
- A visita continua em `cadastro.visita` depois do expurgo e da revogação

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 19/11 às 11h00 e esperar o ciclo do `Expurgo` | A confirmação das 10h00 de 18/11 some de `motor.confirmacao` |
| 2 | Ler as visitas da casa 14 | A visita de 18/11 continua lá, igual |
| 3 | Revogar a casa 14 e ler as visitas de novo | API responde 204; a visita continua lá |

**Cobre:** INV-Visita-6

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::Visita inv. 6: apagar o idoso nao leva a visita junto` (existe)

## F04 Vizinho de confiança e autorizações

### TC-F04-01

**Indicar vizinho com AvisarVizinho responde 204 e ele aparece na lista de nomes**

**Feature:** F04

**Tipo:** Positivo

**Objetivo:** Provar que o vizinho é indicado quando o idoso autorizou avisar o vizinho e o vizinho aceitou.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Casa 9 (`AvisarVizinho` verdadeiro, ainda sem vizinho)

**Entradas:**

- `PUT /api/idosos/{id}/vizinho` com `nome` `Rosa Cedro`, `telefone` `(00) 90000-0099` e `aceitouEm` 16/11 às 14h00

**Saídas esperadas:**

- 204
- Item da casa 9 na lista de nomes com o vizinho e o telefone

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a lista de nomes e achar a casa 9 | `"vizinho": null` |
| 2 | Enviar o `PUT` do vizinho | API responde 204 |
| 3 | Chamar `GET /api/lista-de-nomes?microarea=3` | A casa 9 traz `vizinho` com `Rosa Cedro` e `(00) 90000-0099` |

**Cobre:** `PUT /idosos/{id}/vizinho`, INV-Idoso-6, IDEM-Cadastro-IndicarVizinho

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VizinhoTestes.cs::ComAvisarVizinho_Indica_204` (proposto)

### TC-F04-02

**Indicar vizinho sem AvisarVizinho responde 422 AUTORIZACAO_AUSENTE**

**Feature:** F04

**Tipo:** Negativo

**Objetivo:** Provar que o vizinho não é gravado sem a autorização de avisar o vizinho.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 8 (`AvisarVizinho` falso, sem vizinho)

**Entradas:**

- `PUT /api/idosos/{id}/vizinho` com nome, telefone `(00) 90000-0098` e `aceitouEm`

**Saídas esperadas:**

- 422 `AUTORIZACAO_AUSENTE`
- Nenhum vizinho gravado
- Na lista de nomes, a casa 8 com `vizinho` nulo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `PUT` do vizinho da casa 8 | API responde 422 `AUTORIZACAO_AUSENTE`; a mensagem não repete nome nem telefone |
| 2 | Ler `cadastro.vizinho_de_confianca` do idoso da casa 8 | Nenhuma linha |
| 3 | Chamar a lista de nomes | A casa 8 continua com `"vizinho": null` |

**Cobre:** `PUT /idosos/{id}/vizinho`, INV-Idoso-6

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VizinhoTestes.cs::SemAvisarVizinho_422AutorizacaoAusente` (proposto)

### TC-F04-03

**Indicar vizinho sem a data em que ele aceitou não grava o vizinho**

**Feature:** F04

**Tipo:** Negativo

**Objetivo:** Provar que o vizinho só é indicado com a data de aceite.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 19 (`AvisarVizinho` verdadeiro, sem vizinho)

**Entradas:**

- `PUT /api/idosos/{id}/vizinho` com nome e telefone, sem `aceitouEm`

**Saídas esperadas:**

- A API recusa com 422; a tabela 6.3 não fixa o código
- Nenhum vizinho gravado

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `PUT` sem `aceitouEm` | API responde 422; anotar o `codigo` observado |
| 2 | Ler `cadastro.vizinho_de_confianca` do idoso da casa 19 | Nenhuma linha |

**Cobre:** INV-Idoso-6

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::Idoso inv. 6: vizinho sem a data em que aceitou` (existe)

### TC-F04-04

**Indicar outro vizinho substitui o antigo**

**Feature:** F04

**Tipo:** Positivo

**Objetivo:** Provar que `IndicarVizinho` usa o `IdosoId` como chave e que o último valor vale.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 13 com o vizinho `Severina Jatobá`, `(00) 90000-0013`

**Entradas:**

- `PUT /api/idosos/{id}/vizinho` com `Rosa Cedro`, `(00) 90000-0099` e `aceitouEm`

**Saídas esperadas:**

- 204
- Um vizinho da casa 13, o novo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `PUT` com o vizinho novo | API responde 204 |
| 2 | Chamar a lista de nomes e achar a casa 13 | `vizinho` é `Rosa Cedro`, `(00) 90000-0099`; `Severina Jatobá` não aparece |
| 3 | Contar os vizinhos do idoso no banco | Uma linha |

**Cobre:** `PUT /idosos/{id}/vizinho`, IDEM-Cadastro-IndicarVizinho

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VizinhoTestes.cs::IndicarDeNovo_SubstituiVizinho` (proposto)

### TC-F04-05

**O mesmo vizinho indicado duas vezes não cria vizinho repetido**

**Feature:** F04

**Tipo:** Negativo

**Objetivo:** Provar que a repetição de `IndicarVizinho` é inofensiva: o idoso tem no máximo um vizinho.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 29 (`AvisarVizinho` verdadeiro, sem vizinho)

**Entradas:**

- O mesmo `PUT /api/idosos/{id}/vizinho` enviado duas vezes

**Saídas esperadas:**

- 204 nas duas chamadas
- Uma linha de vizinho
- Um item da casa 29 na lista de nomes

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `PUT` duas vezes | API responde 204 nas duas |
| 2 | Contar os vizinhos do idoso da casa 29 | Uma linha |
| 3 | Chamar a lista de nomes | Um só item com o token da casa 29, com o vizinho |

**Cobre:** IDEM-Cadastro-IndicarVizinho

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::Idoso: no maximo um vizinho` (existe)

### TC-F04-06

**Aviso do alerta vai ao vizinho autorizado da casa**

**Feature:** F04

**Tipo:** Positivo

**Objetivo:** Provar que `DestinatariosDoAviso()` traz o vizinho quando o idoso autorizou e indicou um vizinho.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 5 (`AvisarVizinho` e `AvisarFamilia` verdadeiros, vizinho `Palmira Pitangueira`), papel C: silêncio às 10h

**Entradas:**

- Relógio em 10h01
- `GET /api/avisos?microarea=3`

**Saídas esperadas:**

- Um aviso para o token da casa 5, motivo `SemMovimentoNaJanela`, `desde` 10h00
- Uma linha de log do envio simulado, sem telefone

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h01 e esperar um ciclo do `VigiaDoRadar` | A casa 5 fica em `pendentes`, `Vermelho` |
| 2 | Chamar `GET /api/avisos?microarea=3` | Um aviso com o token da casa 5, desde 10h00 |
| 3 | Ler o log da API | Uma linha do envio simulado ao vizinho, sem o telefone `(00) 90000-0005` e sem nome |

**Cobre:** INV-Idoso-7

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/IdosoTestes.cs::DestinatariosDoAviso_ComVizinho_TrazVizinho` (proposto)

### TC-F04-07

**Sem AvisarFamilia a família nunca entra nos destinatários do aviso**

**Feature:** F04

**Tipo:** Negativo

**Objetivo:** Provar que a família não recebe aviso sem a autorização de avisar a família.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 6 (`AvisarVizinho` verdadeiro, `AvisarFamilia` falso), papel D: silêncio às 10h

**Entradas:**

- Relógio em 10h01
- `DestinatariosDoAviso()` do idoso da casa 6

**Saídas esperadas:**

- Os destinatários trazem só o vizinho
- Nenhum envio além do aviso ao vizinho

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h01 e esperar um ciclo do `VigiaDoRadar` | Um aviso ao vizinho da casa 6 |
| 2 | Ler o log do envio simulado | Um só envio, para o vizinho; nenhum para a família |
| 3 | Rodar o teste de unidade de `DestinatariosDoAviso()` com `AvisarFamilia` falso | A família não aparece na lista |

**Cobre:** INV-Idoso-7

**Caso do modelo:** CT-34

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/IdosoTestes.cs::Caso34_SemAvisarFamilia_FamiliaNaoAparece` (proposto)

## F05 Perfil da casa

### TC-F05-01

**Trocar o perfil no Cadastro troca o perfil no radar**

**Feature:** F05

**Tipo:** Positivo

**Objetivo:** Provar que `PUT /perfil` com token ativo chama `DefinirPerfilAsync` (P21).

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 13 (`Sozinho`, não eletrodependente)

**Entradas:**

- `PUT /api/idosos/{id}/perfil` da casa 13 com `contato` `Acamado` e `eletrodependente` verdadeiro

**Saídas esperadas:**

- 204
- Card da casa 13 com `perfil` `{contato: Acamado, eletrodependente: true}`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar o radar e achar o token da casa 13 | Perfil `Sozinho`, não eletrodependente |
| 2 | Enviar o `PUT` do perfil | API responde 204 |
| 3 | Chamar `GET /api/radar?microarea=3` | O card da casa 13 traz `Acamado` e eletrodependente |
| 4 | Ler `motor.casa_monitorada` pelo token | Perfil novo gravado; token e microárea iguais |

**Cobre:** `PUT /idosos/{id}/perfil`, P21, IDEM-Motor-DefinirPerfil

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::TokenAtivo_TrocaPerfilNoMotor` (proposto)

### TC-F05-02

**Perfil repetido fica igual e o último valor vale**

**Feature:** F05

**Tipo:** Positivo

**Objetivo:** Provar que `AlterarPerfil` e `DefinirPerfil` convergem pelo último valor.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 14 (`Sozinho`)

**Entradas:**

- `PUT /perfil` com `Coabitado` duas vezes
- Depois `PUT /perfil` com `Sozinho`

**Saídas esperadas:**

- 204 nas três chamadas
- Depois das duas primeiras, perfil `Coabitado`
- No fim, perfil `Sozinho` no Cadastro e no radar

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `Coabitado` duas vezes | API responde 204 nas duas; o radar mostra `Coabitado`; uma casa com o token |
| 2 | Enviar `Sozinho` | API responde 204 |
| 3 | Chamar o radar e ler o idoso no banco | Os dois mostram `Sozinho` |

**Cobre:** `PUT /idosos/{id}/perfil`, IDEM-Motor-DefinirPerfil, IDEM-Cadastro-IndicarVizinho

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/MotorDeCasasTestes.cs::DefinirPerfil_UltimoValorVale` (proposto)

### TC-F05-03

**Trocar o perfil de idoso sem token grava no Cadastro e não chama o Motor**

**Feature:** F05

**Tipo:** Negativo

**Objetivo:** Provar que P21 só age com token ativo e que o perfil gravado vai ao Motor no consentimento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento

**Entradas:**

- `PUT /api/idosos/{id}/perfil` com `Acamado`
- Depois, `POST /api/idosos/{id}/consentimento`

**Saídas esperadas:**

- 204 e perfil `Acamado` no Cadastro
- Nenhuma casa no Motor antes do consentimento
- Depois do consentimento, a casa entra no Motor como `Acamado`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `PUT` do perfil | API responde 204 |
| 2 | Ler o idoso e contar as casas do Motor | Perfil `Acamado` no idoso; o Motor continua com 40 casas |
| 3 | Registrar o consentimento e chamar o radar | O card do token novo traz `Acamado`, enviado por P1 |

**Cobre:** `PUT /idosos/{id}/perfil`, P21

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::SemToken_GravaPerfil_NaoChamaMotor` (proposto)

### TC-F05-04

**Perfil de idoso revogado não reativa a casa no Motor**

**Feature:** F05

**Tipo:** Negativo

**Objetivo:** Provar que a troca de perfil sem token ativo não chega à casa desativada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 16 revogada, token antigo anotado

**Entradas:**

- `PUT /api/idosos/{id}/perfil` da casa 16 com `Acamado`

**Saídas esperadas:**

- 204 e perfil gravado no Cadastro
- A linha do token antigo continua desativada e com o perfil antigo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `PUT` do perfil | API responde 204 |
| 2 | Ler a linha do token antigo em `motor.casa_monitorada` | `desativada_em` preenchido; perfil `Sozinho`, como antes |
| 3 | Chamar o radar | Nenhum card com o token antigo |

**Cobre:** P21, IDEM-Motor-DefinirPerfil

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::Revogado_NaoChamaMotor` (proposto)

### TC-F05-05

**Perfil com contato desconhecido ou idoso inexistente é recusado**

**Feature:** F05

**Tipo:** Negativo

**Objetivo:** Provar que a rota não grava perfil inválido nem perfil de idoso que não existe.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Casa 13 (`Sozinho`)

**Entradas:**

- `PUT /perfil` da casa 13 com `contato` `Visitante`
- `PUT /perfil` com um `id` aleatório

**Saídas esperadas:**

- 422 `CAMPO_INVALIDO` para o enum desconhecido (seção 7.2 do modelo de dados)
- 404 `IDOSO_DESCONHECIDO` para o `id` inexistente
- Perfil da casa 13 sem mudança

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o perfil `Visitante` para a casa 13 | API responde 422 `CAMPO_INVALIDO` |
| 2 | Enviar um perfil válido com `id` aleatório | API responde 404 `IDOSO_DESCONHECIDO` |
| 3 | Chamar o radar | A casa 13 continua `Sozinho` |

**Cobre:** `PUT /idosos/{id}/perfil`

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/PerfilTestes.cs::PerfilInvalido_422_NaoGrava` (proposto)

## F06 Lista de nomes para o celular da ACS

### TC-F06-01

**Lista de nomes traz token, nome, endereço e vizinho das casas ativas**

**Feature:** F06

**Tipo:** Positivo

**Objetivo:** Provar que a lista sai pela saída segura com o canal `AppAutenticado`, que aceita nome, endereço e telefone.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3

**Entradas:**

- `GET /api/lista-de-nomes?microarea=3`

**Saídas esperadas:**

- 200 com 40 itens
- Casa 1: `Efigênia Jatobá`, `Rua do Sabiá, 12 - Vila Exemplo`, vizinho `Odete Embaúba`, `(00) 90000-0001`
- Casa 9: `"vizinho": null`
- Nenhum campo mascarado

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a lista de nomes autenticado | API responde 200 com 40 itens, um por casa com token |
| 2 | Achar o item da casa 1 pelo token de `tokens.local.json` | Nome, endereço, nome e telefone do vizinho inteiros, sem `[NOME]` nem `[TELEFONE]` |
| 3 | Achar o item da casa 9 | `"vizinho": null`, porque ela autorizou e ainda não indicou vizinho |
| 4 | Abrir o app, entrar e sincronizar | O radar mostra os cards com o nome de cada casa, juntado no celular |

**Cobre:** `GET /lista-de-nomes?microarea={id}`, P3, CANAL-AppAutenticado

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::Autenticado_TrazTokenNomeEnderecoVizinho` (proposto)

### TC-F06-02

**Lista de nomes não traz condições, medicamentos nem observação**

**Feature:** F06

**Tipo:** Negativo

**Objetivo:** Provar que só nome, endereço e telefone saem pelo canal do app.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Visita da casa 6 com `observacao` `vizinha disse que foi à feira`

**Entradas:**

- `GET /api/lista-de-nomes?microarea=3`

**Saídas esperadas:**

- Itens só com `casaToken`, `nome`, `endereco` e `vizinho`
- Nenhum texto de `condicoes`, `medicamentos` ou observação de visita

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a lista de nomes | Cada item tem só as chaves `casaToken`, `nome`, `endereco` e `vizinho` |
| 2 | Procurar na resposta `usa concentrador de oxigênio`, `anti-hipertensivo` e `foi à feira` | Nenhum aparece |

**Cobre:** P3, CANAL-AppAutenticado

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::NaoTrazCondicoesNemMedicamentos` (proposto)

### TC-F06-03

**Lista de nomes sem autenticação é recusada e nenhum nome sai**

**Feature:** F06

**Tipo:** Negativo

**Objetivo:** Provar que o canal `AppAutenticado` exige a ACS autenticada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado

**Entradas:**

- `GET /api/lista-de-nomes?microarea=3` sem credencial

**Saídas esperadas:**

- Resposta de recusa, sem nenhum item. O modelo de domínio não fixa o código; o app trata 401 (seção 6.3 do modelo de dados)
- Nenhum nome, endereço ou telefone no corpo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a lista de nomes por curl, sem cabeçalho de autenticação | A API recusa; anotar o status |
| 2 | Procurar no corpo da resposta `Vila Exemplo` e `(00) 9` | Nada aparece |

**Cobre:** `GET /lista-de-nomes?microarea={id}`, CANAL-AppAutenticado

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::SemAutenticacao_Recusa` (proposto)

### TC-F06-04

**Lista de nomes não traz idoso sem token nem de outra microárea**

**Feature:** F06

**Tipo:** Negativo

**Objetivo:** Provar que a lista só traz casas com token ativo da microárea da ACS.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento
- Casa 16 revogada

**Entradas:**

- `GET /api/lista-de-nomes?microarea=3`
- `GET /api/lista-de-nomes?microarea=4`

**Saídas esperadas:**

- 39 itens na microárea 3, sem o idoso novo e sem a casa 16
- 403 `MICROAREA_DE_OUTRA_ACS` na microárea 4 (seção 7.2 do modelo de dados)

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a lista da microárea 3 | 39 itens; nem `Amélia Cedro` nem `Ivo Copaíba` aparecem |
| 2 | Chamar a lista da microárea 4 | API responde 403 `MICROAREA_DE_OUTRA_ACS`, sem item |

**Cobre:** `GET /lista-de-nomes?microarea={id}`, P3

**Caso do modelo:** CT-32

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ListaDeNomesTestes.cs::SoCasasComTokenAtivo` (proposto)

## F07 Recebimento dos pacotes do nó

### TC-F07-01

**Movimento e telemetria válidos viram eventos e o último pacote recebe a hora do servidor**

**Feature:** F07

**Tipo:** Positivo

**Objetivo:** Provar que pacote de casa ativa é gravado com o tipo certo e que `UltimoPacoteEm` é a hora de chegada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 (`Sozinho`) ativa
- Relógio em 09h00

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `MOVIMENTO` `seq` 100 com `ts` 08h59min00s
- `TELEMETRIA` `seq` 101 com `ts` 08h59min30s, `tempC` 31,5 e `tensaoV` 126,8

**Saídas esperadas:**

- Dois eventos na linha do tempo, `MOVIMENTO` e `TELEMETRIA`
- `tempC` 31,5 e `tensaoV` 126,8 gravados
- `ultimoPacoteEm` igual à hora de chegada do último pacote, não ao `ts`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o `MOVIMENTO` e anotar a hora de envio | Pacote aceito |
| 2 | Publicar a `TELEMETRIA` | Pacote aceito |
| 3 | Chamar `GET /api/casas/{token}/linha-do-tempo` | Eventos `MOVIMENTO` 08h59min00s e `TELEMETRIA` 08h59min30s, com `recebidoEm` perto das 09h00; a telemetria traz 31,5 °C e 126,8 V |
| 4 | Comparar `ultimoPacoteEm` com o `recebidoEm` da telemetria | Iguais, e depois do `ts` 08h59min30s |

**Cobre:** INV-EventoSensor-2, INV-EventoSensor-4, INV-EventoSensor-5, INV-CasaMonitorada-2

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::PacoteValido_GravaEvento_UltimoPacoteHoraDoServidor` (proposto)

### TC-F07-02

**O mesmo pacote recebido duas vezes vira um evento só**

**Feature:** F07

**Tipo:** Negativo

**Objetivo:** Provar que a chave `(casaToken, seq, ts)` impede a duplicação e que a repetição conta como sucesso.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 ativa
- Relógio em 09h00

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `MOVIMENTO` `seq` 1842 com `ts` 08h58min00s, publicado duas vezes

**Saídas esperadas:**

- Um evento com a chave `(token, 1842, 08h58)`
- Nenhum erro no log do consumidor

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o pacote | Pacote aceito |
| 2 | Publicar o mesmo pacote de novo | Pacote aceito sem erro |
| 3 | Contar os eventos da casa 13 com `seq` 1842 em `motor.evento_sensor` e na linha do tempo | Um só |

**Cobre:** INV-EventoSensor-1, P6, NC-1, IDEM-Motor-RegistrarPacote

**Caso do modelo:** CT-26

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::caso 26: o mesmo pacote com ON CONFLICT DO NOTHING conta como sucesso` (existe)

### TC-F07-03

**Seq que volta a 1 depois do reinício do nó é aceito com ts novo**

**Feature:** F07

**Tipo:** Positivo

**Objetivo:** Provar que a chave inclui o `ts`, então o `seq` reiniciado não descarta eventos novos.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 ativa
- Evento `MOVIMENTO` `seq` 1 com `ts` 06h00 já gravado
- Relógio em 09h00

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `MOVIMENTO` `seq` 1 com `ts` 08h59min00s, depois do reinício do nó

**Saídas esperadas:**

- Dois eventos com `seq` 1, às 06h00 e às 08h59

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o `MOVIMENTO` `seq` 1 com o `ts` novo | Pacote aceito |
| 2 | Chamar a linha do tempo da casa 13 | Dois eventos `MOVIMENTO` com `seq` 1, em ordem de `ocorridoEm` |

**Cobre:** INV-EventoSensor-1, P6, NC-1, IDEM-Motor-RegistrarPacote

**Caso do modelo:** CT-27

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::caso 27: seq 1 de novo com outro ts` (existe)

### TC-F07-04

**Pacote com ts 3 minutos à frente ou com mais de 24 horas é recusado inteiro**

**Feature:** F07

**Tipo:** Negativo

**Objetivo:** Provar que o Motor recusa `ts` fora da tolerância e que o pacote recusado não avança `UltimoPacoteEm`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 ativa, com o nó simulado parado
- Relógio em 09h00
- `ultimo_pacote_em` da casa 13 anotado

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `MOVIMENTO` com `ts` 09h03, 3 minutos à frente da chegada
- `MOVIMENTO` com `ts` de 17/11 às 08h00, 25 horas antes

**Saídas esperadas:**

- Nenhum evento gravado
- `ultimo_pacote_em` igual ao anotado

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o `MOVIMENTO` de `ts` 09h03 | O Motor recusa o pacote |
| 2 | Publicar o `MOVIMENTO` de `ts` 17/11 08h00 | O Motor recusa o pacote |
| 3 | Ler os eventos e o `ultimo_pacote_em` da casa 13 | Nenhum evento novo; `ultimo_pacote_em` igual ao anotado |

**Cobre:** INV-EventoSensor-3, NC-1, INV-CasaMonitorada-2

**Caso do modelo:** CT-28

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::Caso28_TsAdiantado_Recusado_UltimoPacoteNaoMuda` (proposto)

### TC-F07-05

**Pacote dentro da tolerância e evento bufferizado de 23 horas são aceitos**

**Feature:** F07

**Tipo:** Positivo

**Objetivo:** Provar os limites de `ToleranciaDoRelogio` de 2 minutos e de 24 horas e que o buffer não recua o último pacote.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 ativa
- Relógio em 18/11 às 09h00

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `MOVIMENTO` com `ts` 09h01min30s, 90 s à frente
- `MOVIMENTO` com `ts` de 17/11 às 10h00 e `bufferizado: true`

**Saídas esperadas:**

- Os dois eventos gravados
- O evento de 17/11 com a marca de bufferizado
- `ultimoPacoteEm` na hora de chegada do último pacote, nunca 17/11

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o `MOVIMENTO` de `ts` 09h01min30s | Pacote aceito |
| 2 | Publicar o `MOVIMENTO` bufferizado de 17/11 às 10h00 | Pacote aceito |
| 3 | Chamar a linha do tempo | Os dois eventos aparecem; o de 17/11 traz `bufferizado` verdadeiro; `ultimoPacoteEm` é a chegada, perto das 09h00 de 18/11 |

**Cobre:** INV-EventoSensor-3, INV-CasaMonitorada-2

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RegistrarPacoteTestes.cs::TsDentroDaTolerancia_Aceito` (proposto)

### TC-F07-06

**Pacote de token desconhecido ou revogado é descartado sem logar o conteúdo**

**Feature:** F07

**Tipo:** Negativo

**Objetivo:** Provar que P2 descarta o pacote, não grava evento e não escreve o payload no log.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Casa 16 revogada, token antigo anotado
- Acesso ao log da API

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `MOVIMENTO` `seq` 777 com `tempC` 31,5 para o token `00000000000000000000000000000abc`, que nunca existiu
- O mesmo pacote para o token antigo da casa 16

**Saídas esperadas:**

- Nenhum evento para os dois tokens
- Nenhuma casa criada
- O log não traz `seq` 777, `31.5` nem o JSON do pacote

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o pacote para o token que nunca existiu | O Motor descarta |
| 2 | Publicar o pacote para o token antigo da casa 16 | O Motor descarta |
| 3 | Contar eventos e casas dos dois tokens | Zero eventos; nenhuma casa nova; a casa 16 continua desativada |
| 4 | Procurar no log `"seq": 777` e `31.5` | Nada aparece |

**Cobre:** P2, INV-EventoSensor-4

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::EventoSensor inv. 4: evento de token desconhecido` (existe)

### TC-F07-07

**Depois de um pacote descartado, o próximo pacote válido é gravado**

**Feature:** F07

**Tipo:** Positivo

**Objetivo:** Provar que o descarte de P2 não derruba o consumidor MQTT.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 ativa
- Relógio em 09h00

**Entradas:**

- `MOVIMENTO` para o token `00000000000000000000000000000abc`
- Logo depois, `MOVIMENTO` `seq` 200 com `ts` 08h59min50s para a casa 13

**Saídas esperadas:**

- O primeiro pacote descartado
- O evento `seq` 200 na linha do tempo da casa 13

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o pacote do token desconhecido | O Motor descarta, sem erro que pare o consumidor |
| 2 | Publicar o `MOVIMENTO` da casa 13 | Pacote aceito |
| 3 | Chamar a linha do tempo da casa 13 | O evento `seq` 200 aparece |

**Cobre:** P2, INV-EventoSensor-4

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/ConsumidorMqttTestes.cs::TokenDesconhecido_Descarta_SegueConsumindo` (proposto)

### TC-F07-08

**Heartbeat só avança o último pacote e nunca vira evento**

**Feature:** F07

**Tipo:** Negativo

**Objetivo:** Provar que o `HEARTBEAT` não gera linha em `evento_sensor`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 ativa, com o nó simulado parado
- Relógio em 09h00

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: três `HEARTBEAT` com `seq` 300 a 302, um a cada 20 s

**Saídas esperadas:**

- `ultimo_pacote_em` avança a cada heartbeat
- Nenhum evento novo na casa 13

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Contar os eventos da casa 13 | Contagem anotada |
| 2 | Publicar os três heartbeats, lendo `ultimoPacoteEm` depois de cada um | `ultimoPacoteEm` cresce a cada leitura |
| 3 | Contar os eventos da casa 13 e procurar `Heartbeat` na linha do tempo | A contagem não mudou; nenhum evento de heartbeat |

**Cobre:** INV-EventoSensor-2, INV-CasaMonitorada-2

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes.sql::EventoSensor inv. 2: heartbeat nunca vira linha` (existe)

### TC-F07-09

**Telemetria com 85 °C ou 300 V grava o evento com a leitura ausente**

**Feature:** F07

**Tipo:** Negativo

**Objetivo:** Provar que o valor fora da faixa física não entra como leitura, mas o evento é gravado.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 14 ativa
- Relógio em 09h00

**Entradas:**

- Publicação MQTT em `corredor/v1/{casaToken}/evento`: `TELEMETRIA` `seq` 400 com `tempC` 85 e `tensaoV` 127
- `TELEMETRIA` `seq` 401 com `tempC` 30 e `tensaoV` 300

**Saídas esperadas:**

- Dois eventos `TELEMETRIA`
- O primeiro com `tempC` nulo e `tensaoV` 127
- O segundo com `tempC` 30 e `tensaoV` nulo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as duas telemetrias | Os dois pacotes aceitos |
| 2 | Chamar a linha do tempo da casa 14 | `seq` 400 com `tempC` nulo e `tensaoV` 127; `seq` 401 com `tempC` 30 e `tensaoV` nulo |

**Cobre:** INV-EventoSensor-5

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/EventoSensorTestes.cs::LeituraForaDaFaixa_GravadaComoAusente` (proposto)

## F08 Sinal de vida conforme o perfil de contato

### TC-F08-01

**Casa Sozinho com movimento na janela fica Verde**

**Feature:** F08

**Tipo:** Positivo

**Objetivo:** Provar que, na casa `Sozinho`, um movimento do nó na janela atual é sinal de vida e deixa a casa verde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 33 (`Sozinho`, não eletrodependente) escolhida
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 07h00 para a casa 33
- Relógio de demo em 09h00

**Saídas esperadas:**

- Casa 33 fora de `pendentes`, `atendidas` e `aguardando`
- `verdes` conta a casa 33

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 07h00 e publicar `MOVIMENTO` com `ts` 07h00 para a casa 33 | Pacote aceito; a linha do tempo mostra o movimento das 07h00 |
| 2 | Mover o relógio para 09h00 | Relógio responde 200 com `agora` 09h00 |
| 3 | Chamar `GET /api/radar?microarea=3` | A casa 33 não aparece em nenhuma lista e `verdes` inclui a casa |

**Cobre:** SV-Sozinho, NC-5

**Caso do modelo:** CT-2

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso02_SozinhoComMovimento_09h_Verde` (proposto)

### TC-F08-02

**Casa Sozinho com um toque no botão fica Verde**

**Feature:** F08

**Tipo:** Positivo

**Objetivo:** Provar que o `BOTAO_BEM` conta como sinal de vida na casa `Sozinho`, sem nenhum movimento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 9 (`Sozinho`) sem movimento desde a abertura
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `BOTAO_BEM` com `ts` 14h40 para a casa 9
- Relógio em 14h41

**Saídas esperadas:**

- Casa 9 conta em `verdes` às 14h41

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 14h40 e publicar `BOTAO_BEM` com `ts` 14h40 | Pacote aceito |
| 2 | Mover o relógio para 14h41 e chamar o radar | A casa 9 não está em `pendentes` nem em `aguardando`; `verdes` a inclui |

**Cobre:** SV-Sozinho

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Sozinho_BotaoBem_ContaComoSinalDeVida` (proposto)

### TC-F08-03

**Casa Coabitado com movimento e botão continua Aguardando**

**Feature:** F08

**Tipo:** Negativo

**Objetivo:** Provar que na casa com outra pessoa nenhum evento do sensor vale como sinal de vida da idosa.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 12 (`Coabitado`, não eletrodependente)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 07h00
- `BOTAO_BEM` com `ts` 08h00
- Relógio em 09h00 e em 10h00

**Saídas esperadas:**

- Casa 12 em `aguardando` às 09h00 e às 10h00
- A casa não conta em `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 07h00 e `BOTAO_BEM` com `ts` 08h00 para a casa 12 | Os dois pacotes aceitos e na linha do tempo |
| 2 | Mover o relógio para 09h00 e chamar o radar | Casa 12 em `aguardando`; `verdes` não a conta |
| 3 | Mover o relógio para 10h00 e chamar o radar | Casa 12 continua em `aguardando`, sem alerta |

**Cobre:** SV-Coabitado, NC-5

**Caso do modelo:** CT-24

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso24_CoabitadoComMovimentoEBotao_Aguardando` (proposto)

### TC-F08-04

**Casa Coabitado fica Verde com visita EstavaBem**

**Feature:** F08

**Tipo:** Positivo

**Objetivo:** Provar que a confirmação humana com pessoa vista é o único sinal de vida da casa `Coabitado`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 12 (`Coabitado`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- App ou Swagger autenticado como `acs-demo-01`

**Entradas:**

- `POST /api/visitas` com `desfecho` `EstavaBem` e `ocorridaEm` 10h30 para o token da casa 12
- Relógio em 10h31

**Saídas esperadas:**

- Visita 201
- Casa 12 conta em `verdes` às 10h31

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h30 e registrar a visita `EstavaBem` com `ocorridaEm` 10h30 | API responde 201 |
| 2 | Mover o relógio para 10h31 e chamar o radar | A casa 12 não está em `aguardando` e `verdes` a inclui |

**Cobre:** SV-Coabitado, NC-5

**Caso do modelo:** CT-25

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso25_CoabitadoComVisita_Verde` (proposto)

### TC-F08-05

**Movimento na casa Acamado não conta e a casa entra vermelha**

**Feature:** F08

**Tipo:** Negativo

**Objetivo:** Provar que a casa `Acamado` ignora o movimento do corredor e fica vermelha no início da janela da manhã.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 11 (`Acamado`, não eletrodependente)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 05h00 para a casa 11
- Relógio em 05h01

**Saídas esperadas:**

- Casa 11 em `pendentes`, `situacao` `Vermelho`, `motivo` `SemMovimentoNaJanela`, `desde` 05h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 05h00 para a casa 11 | Pacote aceito |
| 2 | Mover o relógio para 05h01 e chamar o radar | Casa 11 em `pendentes`, `Vermelho` desde 05h00, `atendida` falso |

**Cobre:** SV-Acamado, NC-5

**Caso do modelo:** CT-16

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso16_Acamado_MovimentoNaoConta` (proposto)

### TC-F08-06

**Casa Acamado fica Verde com visita e volta vermelha na janela seguinte**

**Feature:** F08

**Tipo:** Positivo

**Objetivo:** Provar que a visita com pessoa vista é sinal de vida do acamado só até o fim da janela em que ocorreu.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 11 (`Acamado`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` com `ocorridaEm` 09h00
- Relógio em 09h01 e em 10h00

**Saídas esperadas:**

- `Verde` às 09h01
- `Vermelho` desde 10h00 às 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h00 e registrar a visita `EstavaBem` da casa 11 | API responde 201 |
| 2 | Mover o relógio para 09h01 e chamar o radar | Casa 11 conta em `verdes` |
| 3 | Mover o relógio para 10h00 e chamar o radar | Casa 11 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, desde 10h00 |

**Cobre:** SV-Acamado

**Caso do modelo:** CT-16

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso16_Acamado_VisitaValeSoNaJanela` (proposto)

### TC-F08-07

**Casa Sozinho só com heartbeat e telemetria não fica Verde**

**Feature:** F08

**Tipo:** Negativo

**Objetivo:** Provar que pacotes de aparelho ligado, sem movimento nem botão, não são sinal de vida.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Telemetria de 31 °C e 127 V a cada 5 minutos
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Só `HEARTBEAT` e `TELEMETRIA` desde 05h00
- Relógio em 09h00

**Saídas esperadas:**

- Casa 13 em `aguardando`
- Casa 13 não conta em `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Deixar o nó simulado mandar só heartbeat e telemetria das 05h00 às 09h00 | Linha do tempo mostra só telemetrias; nenhum heartbeat vira evento |
| 2 | Mover o relógio para 09h00 e chamar o radar | Casa 13 em `aguardando`, nunca em `verdes` |

**Cobre:** SV-Sozinho, NC-5

**Caso do modelo:** CT-1

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso01_SozinhoSemSinal_09h_Aguardando` (proposto)

## F09 Aparelho mudo fica cinza e nunca verde

### TC-F09-01

**Aparelho mudo deixa a casa Cinza e nunca Verde**

**Feature:** F09

**Tipo:** Negativo

**Objetivo:** Provar que o silêncio do nó vira cinza mesmo com movimento anterior na janela.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 10 (`Sozinho`, não eletrodependente)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 09h00
- Último pacote da casa às 09h10, depois o nó para
- Relógio em 10h00

**Saídas esperadas:**

- Casa 10 em `pendentes`, `Cinza`, `SemHeartbeat`, `desde` 09h10, `atendida` falso
- Casa 10 fora de `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 09h00 e heartbeats até 09h10 | Pacotes aceitos; `ultimoPacoteEm` 09h10 |
| 2 | Parar o nó simulado da casa 10 | Nenhum pacote novo chega |
| 3 | Mover o relógio para 10h00 e chamar o radar | Casa 10 `Cinza`, motivo `SemHeartbeat`, desde 09h10, pendente; não conta em `verdes` |

**Cobre:** R2, P14, NC-2, TV-SIT-2

**Caso do modelo:** CT-10

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso10_AparelhoMudo_Cinza` (proposto)

### TC-F09-02

**Casa ativada que nunca mandou pacote é Cinza**

**Feature:** F09

**Tipo:** Negativo

**Objetivo:** Provar que a casa sem nenhum pacote desde a ativação aparece cinza, desde a ativação.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Um idoso novo cadastrado e com consentimento às 08h00; nenhum nó gravado com o token
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 09h00

**Saídas esperadas:**

- Casa nova em `pendentes`, `Cinza`, `SemHeartbeat`, `desde` igual à ativação, `ultimoPacoteEm` nulo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Cadastrar o idoso por `POST /api/idosos` e registrar o consentimento às 08h00 | 201 e 200 com `casaToken` |
| 2 | Mover o relógio para 09h00 e chamar o radar | A casa nova aparece `Cinza` desde 08h00, com `ultimoPacoteEm` nulo |

**Cobre:** R2, TV-SIT-2

**Caso do modelo:** CT-15

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso15_NuncaMandouPacote_Cinza` (proposto)

### TC-F09-03

**Aparelho ligado dentro do silêncio máximo não fica Cinza**

**Feature:** F09

**Tipo:** Positivo

**Objetivo:** Provar que a casa com pacote recente não cai na regra do heartbeat e segue para as outras regras.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 14 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Parâmetros de demo: `SilencioMaximo` de 1 minuto
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 07h00
- Nó parado 50 s antes da consulta, às 08h59min10s
- Relógio em 09h00

**Saídas esperadas:**

- Casa 14 conta em `verdes`, sem alerta `SemHeartbeat`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 07h00 e manter os heartbeats até 08h59min10s | Pacotes aceitos |
| 2 | Mover o relógio para 09h00 e chamar o radar | Casa 14 não está em `pendentes`; `verdes` a inclui |
| 3 | Esperar mais 20 s sem pacote e chamar o radar | Casa 14 aparece `Cinza`, desde 08h59min10s |

**Cobre:** R2, P14, NC-2

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::SemHeartbeat_DentroDoSilencioMaximo_NaoDisparaRegra` (proposto)

### TC-F09-04

**Nó volta e reenvia o buffer: o OcorridoEm conta**

**Feature:** F09

**Tipo:** Positivo

**Objetivo:** Provar que, quando o nó volta, a casa sai do cinza e o evento bufferizado conta pela hora em que aconteceu.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 10 no estado do TC-F09-01 (cinza desde 09h10)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Às 11h00, reenvio de `MOVIMENTO` com `ts` 10h20 e `bufferizado: true`, mais um `HEARTBEAT`
- Variante: o mesmo reenvio com o movimento de `ts` 09h50

**Saídas esperadas:**

- Com o movimento das 10h20: casa 10 `Verde` às 11h00
- Com o movimento das 09h50: casa 10 `Aguardando` às 11h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 11h00 e publicar o `MOVIMENTO` bufferizado de `ts` 10h20 e um `HEARTBEAT` | Pacotes aceitos; linha do tempo marca o evento como bufferizado |
| 2 | Chamar o radar | Casa 10 conta em `verdes`, porque o movimento das 10h20 é da janela da tarde |
| 3 | Reiniciar a demo (seção 8.4 do modelo de dados), repetir o cenário com o movimento de `ts` 09h50 e chamar o radar às 11h00 | Casa 10 em `aguardando`, sem alerta |

**Cobre:** R2, P14, TV-SIT-2

**Caso do modelo:** CT-13

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso13_NoVoltaComBuffer_OcorridoEmConta` (proposto)

### TC-F09-05

**Cinza atendida volta a pendente na janela seguinte**

**Feature:** F09

**Tipo:** Negativo

**Objetivo:** Provar que a visita atende o cinza só na janela atual e que uma visita de antes do silêncio não atende.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 10 cinza desde 09h10 (TC-F09-01)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` com `ocorridaEm` 10h30
- Relógio em 10h31 e em 15h00
- Variante: visita `EstavaBem` às 09h05, antes de o aparelho calar

**Saídas esperadas:**

- `Cinza` atendida às 10h31
- `Cinza` pendente às 15h00
- Na variante, `Cinza` pendente às 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar a visita `EstavaBem` às 10h30 e chamar o radar às 10h31 | Casa 10 em `atendidas`, `Cinza`, `atendida` verdadeiro; fora de `verdes` |
| 2 | Mover o relógio para 15h00 e chamar o radar | Casa 10 de volta em `pendentes`, `Cinza` |
| 3 | Reiniciar a demo e repetir com a visita às 09h05, consultando às 10h00 | Casa 10 em `pendentes`, porque a visita veio antes do `desde` 09h10 |

**Cobre:** R2, TV-SIT-2

**Caso do modelo:** CT-11, CT-12

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso11e12_CinzaAtendidaSoNaJanela` (proposto)

### TC-F09-06

**Aparelho mudo que volta sem movimento cai na regra de silêncio**

**Feature:** F09

**Tipo:** Negativo

**Objetivo:** Provar que a ordem das regras não deixa a casa muda passar para verde quando o nó volta sem sinal de vida.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 15 (`Sozinho`) sem nenhum sinal de vida desde a abertura
- Nó da casa 15 parado das 09h00 às 11h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Às 11h00, o nó volta só com `HEARTBEAT`
- Relógio em 11h00

**Saídas esperadas:**

- Casa 15 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, `desde` 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar o radar às 10h30, com o nó parado | Casa 15 `Cinza` pendente |
| 2 | Mover o relógio para 11h00, ligar o nó e publicar um `HEARTBEAT` | Pacote aceito |
| 3 | Chamar o radar | Casa 15 `Vermelho`, `SemMovimentoNaJanela`, desde 10h00; nunca `Verde` |

**Cobre:** P14, NC-2, TV-SIT-2

**Caso do modelo:** CT-14

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso14_MudoVoltaSemMovimento_VermelhoDesde10h` (proposto)

## F10 Silêncio na janela e saída avisada

### TC-F10-01

**Casa Sozinho sem sinal de vida fica vermelha às 10h**

**Feature:** F10

**Tipo:** Negativo

**Objetivo:** Provar que a janela da manhã vence às 10h e põe a casa silenciosa em vermelho pendente.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 5 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Nenhum movimento nem botão desde 05h00
- Relógio em 09h59 e em 10h00

**Saídas esperadas:**

- `Aguardando` às 09h59
- `Vermelho`, `SemMovimentoNaJanela`, `desde` 10h00, pendente, às 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h59 e chamar o radar | Casa 5 em `aguardando` |
| 2 | Mover o relógio para 10h00 e chamar o radar | Casa 5 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, desde 10h00 |

**Cobre:** R4, TV-SIT-4

**Caso do modelo:** CT-3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso03_SozinhoSemSinal_10h_Vermelho` (proposto)

### TC-F10-02

**Movimento antes da abertura do turno não vale**

**Feature:** F10

**Tipo:** Negativo

**Objetivo:** Provar que o movimento da madrugada não conta para a janela da manhã.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 16 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 03h12
- Relógio em 10h00

**Saídas esperadas:**

- Casa 16 `Vermelho`, `SemMovimentoNaJanela`, desde 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 03h12 | Pacote aceito |
| 2 | Mover o relógio para 10h00 e chamar o radar | Casa 16 em `pendentes`, `Vermelho`, desde 10h00 |

**Cobre:** R4

**Caso do modelo:** CT-5

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso05_MovimentoAntesDaAbertura_NaoVale` (proposto)

### TC-F10-03

**Movimento depois do prazo tira o vermelho da manhã**

**Feature:** F10

**Tipo:** Positivo

**Objetivo:** Provar que a regra aceita sinal de vida desde o início da janela vencida, mesmo depois do prazo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 5 vermelha desde 10h00 (TC-F10-01)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 10h30
- Relógio em 10h31

**Saídas esperadas:**

- Casa 5 conta em `verdes` às 10h31

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h30 e publicar `MOVIMENTO` com `ts` 10h30 | Pacote aceito |
| 2 | Mover o relógio para 10h31 e chamar o radar | Casa 5 fora de `pendentes`; `verdes` a inclui |

**Cobre:** R4, TV-SIT-4

**Caso do modelo:** CT-4

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso04_MovimentoDepoisDoPrazo_Verde` (proposto)

### TC-F10-04

**Sinal de vida só de manhã deixa a casa vermelha às 15h**

**Feature:** F10

**Tipo:** Negativo

**Objetivo:** Provar que a janela da tarde pede sinal de vida novo e vence às 15h.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 17 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 07h00 e nada depois
- Relógio em 15h30

**Saídas esperadas:**

- Casa 17 `Vermelho`, `SemMovimentoNaJanela`, desde 15h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 07h00 | Pacote aceito |
| 2 | Mover o relógio para 15h30 e chamar o radar | Casa 17 em `pendentes`, `Vermelho`, desde 15h00 |

**Cobre:** R4, TV-SIT-4

**Caso do modelo:** CT-7

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso07_SoManha_Vermelho15h` (proposto)

### TC-F10-05

**Saída avisada suspende a regra de silêncio até o fim do turno**

**Feature:** F10

**Tipo:** Positivo

**Objetivo:** Provar que o duplo toque "hoje vou sair" impede o vermelho por silêncio no turno inteiro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 4 (`Sozinho`, papel B da demo)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `BOTAO_SAIR` com `ts` 08h40
- Relógio em 09h00, 10h00 e 16h00

**Saídas esperadas:**

- `Verde` às 09h00
- `Aguardando` às 10h00 e às 16h00, com `saidaAvisada` verdadeiro
- Nunca `Vermelho` no turno

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `BOTAO_SAIR` com `ts` 08h40 | Pacote aceito |
| 2 | Chamar o radar às 09h00 | Casa 4 conta em `verdes` |
| 3 | Chamar o radar às 10h00 | Casa 4 em `aguardando` com `saidaAvisada` verdadeiro, fora de `pendentes` |
| 4 | Chamar o radar às 16h00 | Casa 4 continua em `aguardando`, sem alerta |

**Cobre:** R4, P9

**Caso do modelo:** CT-17

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso17_SaidaAvisada_SuspendeSilencio` (proposto)

### TC-F10-06

**Saída avisada não suspende o alerta de aparelho mudo**

**Feature:** F10

**Tipo:** Negativo

**Objetivo:** Provar que a saída avisada só suspende a regra de silêncio e não esconde um nó que parou.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 18 (`Sozinho`)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `BOTAO_SAIR` com `ts` 08h40
- Nó parado a partir de 09h00
- Relógio em 09h30

**Saídas esperadas:**

- Casa 18 `Cinza`, `SemHeartbeat`, pendente às 09h30

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `BOTAO_SAIR` com `ts` 08h40 e heartbeats até 09h00 | Pacotes aceitos |
| 2 | Parar o nó e mover o relógio para 09h30 | Nenhum pacote novo |
| 3 | Chamar o radar | Casa 18 em `pendentes`, `Cinza`, `SemHeartbeat` |

**Cobre:** P9

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::SaidaAvisada_NaoSuspendeHeartbeat` (proposto)

### TC-F10-07

**Sinal de vida antes do prazo deixa a casa Aguardando depois das 10h**

**Feature:** F10

**Tipo:** Positivo

**Objetivo:** Provar que às 10h01 a casa que deu sinal às 09h50 vai para Aguardando, não para Vermelho.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 19 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 09h50
- Relógio em 10h01

**Saídas esperadas:**

- Casa 19 em `aguardando`, sem alerta

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 09h50 | Pacote aceito |
| 2 | Mover o relógio para 10h01 e chamar o radar | Casa 19 em `aguardando`; a contagem de verdes recomeçou |

**Cobre:** R4

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Exemplo58_SinalAntesDoPrazo_Aguardando` (proposto)

### TC-F10-08

**Casa Acamado vence a janela da tarde no início**

**Feature:** F10

**Tipo:** Negativo

**Objetivo:** Provar que, para o acamado, a janela vence no começo, então a visita da manhã não cobre a tarde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 11 (`Acamado`) com visita `EstavaBem` às 09h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 10h00

**Saídas esperadas:**

- Casa 11 `Vermelho`, `SemMovimentoNaJanela`, desde 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar a visita `EstavaBem` às 09h00 | API responde 201 |
| 2 | Mover o relógio para 10h00 e chamar o radar | Casa 11 em `pendentes`, `Vermelho`, desde 10h00 |

**Cobre:** R4, TV-SIT-4

**Caso do modelo:** CT-16

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso16_Acamado_JanelaVenceNoInicio` (proposto)

## F11 Calor dentro de casa

### TC-F11-01

**Calor contínuo deixa a casa vermelha mesmo com movimento**

**Feature:** F11

**Tipo:** Negativo

**Objetivo:** Provar que 2 horas a 34 °C ou mais põem a casa em vermelho por calor, e que a visita atende só na janela atual.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 8 (`Sozinho`, papel F da demo)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 35 °C a cada 5 minutos das 11h00 às 15h10
- `MOVIMENTO` com `ts` 13h00
- Visita `EstavaBem` às 13h30
- Relógio em 13h05, 13h31 e 15h00

**Saídas esperadas:**

- 13h05: `Vermelho`, `CalorInterno`, desde 13h00, pendente
- 13h31: atendida
- 15h00: pendente de novo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias de 35 °C a partir das 11h00 e o movimento das 13h00 | Pacotes aceitos |
| 2 | Chamar o radar às 13h05 | Casa 8 em `pendentes`, `Vermelho`, `CalorInterno`, desde 13h00 |
| 3 | Registrar a visita `EstavaBem` às 13h30 e chamar o radar às 13h31 | Casa 8 em `atendidas`, ainda `Vermelho` |
| 4 | Chamar o radar às 15h00 | Casa 8 de volta em `pendentes` |

**Cobre:** R3, TV-SIT-3

**Caso do modelo:** CT-18

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso18_CalorInterno` (proposto)

### TC-F11-02

**Temperatura logo abaixo do limite não gera alerta de calor**

**Feature:** F11

**Tipo:** Positivo

**Objetivo:** Provar o limite de 34 °C: 33,9 °C por 3 horas com movimento deixa a casa verde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 20 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 33,9 °C das 11h00 às 14h00
- `MOVIMENTO` com `ts` 13h50
- Relógio em 14h00

**Saídas esperadas:**

- Casa 20 conta em `verdes`, sem `CalorInterno`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias de 33,9 °C e o movimento das 13h50 | Pacotes aceitos |
| 2 | Chamar o radar às 14h00 | Casa 20 fora de `pendentes`; `verdes` a inclui |

**Cobre:** R3, TV-SIT-3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Calor_AbaixoDoLimite_SemAlerta` (proposto)

### TC-F11-03

**Uma leitura abaixo do limite recomeça a contagem do calor**

**Feature:** F11

**Tipo:** Positivo

**Objetivo:** Provar que a sequência quente precisa ser sem interrupção.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 34 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- 35 °C das 11h00 às 12h00
- 33 °C às 12h05
- 35 °C das 12h10 em diante
- `MOVIMENTO` com `ts` 13h00
- Relógio em 13h30 e em 14h10

**Saídas esperadas:**

- 13h30: sem alerta de calor
- 14h10: `Vermelho`, `CalorInterno`, desde 14h10

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias e o movimento | Pacotes aceitos |
| 2 | Chamar o radar às 13h30 | Casa 34 sem alerta de calor |
| 3 | Chamar o radar às 14h10 | Casa 34 em `pendentes`, `CalorInterno`, desde 14h10 |

**Cobre:** R3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Calor_InterrupcaoRecomecaSequencia` (proposto)

### TC-F11-04

**Calor fora do turno não gera alerta**

**Feature:** F11

**Tipo:** Negativo

**Objetivo:** Provar que a regra de calor exige turno aberto.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 37 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 36 °C das 11h00 às 14h00
- Relógio em 14h00

**Saídas esperadas:**

- Casa 37 em `aguardando`, sem alerta

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias de 36 °C | Pacotes aceitos |
| 2 | Chamar o radar às 14h00 | `turnoAberto` falso; casa 37 em `aguardando`, sem `CalorInterno` |

**Cobre:** R3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Calor_SemTurno_SemAlerta` (proposto)

## F12 Energia da casa eletrodependente, com trava

### TC-F12-01

**Queda de energia do eletrodependente com trava e vizinho que não resolve**

**Feature:** F12

**Tipo:** Negativo

**Objetivo:** Provar a regra de energia de ponta a ponta: queda, vizinho, visita, janela e trava depois que a luz volta.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 (`Sozinho`, eletrodependente), que tem vizinho indicado na carga
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 0 V das 14h20 às 15h05 e com 127 V às 15h10
- Vizinho responde `EstaBem` às 14h30
- Visitas `EstavaBem` às 14h40 e às 15h30
- Relógio em 14h21, 14h31, 14h41, 15h00, 15h11 e 15h31

**Saídas esperadas:**

- 14h21 e 14h31: `Vermelho`, `EnergiaEletrodependente`, pendente
- 14h41: atendida
- 15h00: pendente
- 15h11: pendente, pela trava
- 15h31: `Aguardando`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar a telemetria de 0 V às 14h20 e chamar o radar às 14h21 | Casa em `pendentes`, `Vermelho`, `EnergiaEletrodependente`, no topo |
| 2 | Responder `EstaBem` no aviso do vizinho às 14h30 e chamar o radar às 14h31 | Casa continua em `pendentes` |
| 3 | Registrar visita `EstavaBem` às 14h40 e chamar o radar às 14h41 | Casa em `atendidas`, ainda `Vermelho` |
| 4 | Chamar o radar às 15h00 | Casa de volta em `pendentes`, porque a janela mudou |
| 5 | Publicar 0 V às 15h05 e 127 V às 15h10; chamar o radar às 15h11 | Casa em `pendentes`, pela leitura baixa sem visita depois |
| 6 | Registrar visita `EstavaBem` às 15h30 e chamar o radar às 15h31 | Casa em `aguardando`, sem alerta |

**Cobre:** R1, NC-3, TV-SIT-1

**Caso do modelo:** CT-19

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso19_EnergiaComTrava` (proposto)

### TC-F12-02

**Eletrodependente com 61 segundos sem pacote fica vermelho**

**Feature:** F12

**Tipo:** Negativo

**Objetivo:** Provar o limite de 60 s de silêncio da casa eletrodependente.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 (eletrodependente)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Último pacote às 14h00min00s
- Relógio em 14h01min01s

**Saídas esperadas:**

- Casa 1 `Vermelho`, `EnergiaEletrodependente`, desde 14h00min00s, pendente

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar um `HEARTBEAT` às 14h00min00s e parar o nó | Pacote aceito |
| 2 | Mover o relógio para 14h01min01s e chamar o radar | Casa 1 em `pendentes`, `Vermelho`, `EnergiaEletrodependente` |

**Cobre:** R1, TV-SIT-1

**Caso do modelo:** CT-20

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso20_Eletrodependente61s_Vermelho` (proposto)

### TC-F12-03

**Eletrodependente com 59 segundos sem pacote e tensão normal fica Aguardando**

**Feature:** F12

**Tipo:** Positivo

**Objetivo:** Provar que a casa eletrodependente com energia normal e pacote recente não gera alerta.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 (eletrodependente)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 127 V às 14h00min00s
- Relógio em 14h00min59s

**Saídas esperadas:**

- Casa 1 em `aguardando`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar a telemetria de 127 V às 14h00min00s e parar o nó | Pacote aceito |
| 2 | Mover o relógio para 14h00min59s e chamar o radar | Casa 1 em `aguardando`, sem alerta |

**Cobre:** R1, NC-3, TV-SIT-1

**Caso do modelo:** CT-20

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso20_Eletrodependente59s_Aguardando` (proposto)

### TC-F12-04

**Queda de tensão em casa que não é eletrodependente não gera alerta de energia**

**Feature:** F12

**Tipo:** Positivo

**Objetivo:** Provar que a regra de energia só vale para a casa eletrodependente.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 23 (`Sozinho`, não eletrodependente)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 0 V às 14h20
- Relógio em 14h21

**Saídas esperadas:**

- Casa 23 em `aguardando`, sem `EnergiaEletrodependente`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar a telemetria de 0 V | Pacote aceito; a linha do tempo mostra 0 V |
| 2 | Chamar o radar às 14h21 | Casa 23 em `aguardando` |

**Cobre:** R1

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Energia_CasaComum_SemAlerta` (proposto)

### TC-F12-05

**Visita da ACS depois da última leitura baixa solta a trava**

**Feature:** F12

**Tipo:** Positivo

**Objetivo:** Provar que a confirmação da ACS posterior à leitura baixa tira a casa do vermelho depois que a luz volta.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 com 0 V às 02h40 e 127 V às 04h10 (fluxo 4 do event storming)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` às 07h30
- Relógio em 07h00 e em 07h31

**Saídas esperadas:**

- 07h00: `Vermelho`, pendente
- 07h31: `Aguardando`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias de 0 V e de 127 V e chamar o radar às 07h00 | Casa 1 em `pendentes`, `EnergiaEletrodependente` |
| 2 | Registrar a visita `EstavaBem` às 07h30 e chamar o radar às 07h31 | Casa 1 em `aguardando`, sem alerta |

**Cobre:** R1, NC-3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Energia_VisitaDepoisDaLeituraBaixa_SoltaTrava` (proposto)

### TC-F12-06

**Eletrodependente que nunca mandou pacote fica Cinza, não vermelho**

**Feature:** F12

**Tipo:** Negativo

**Objetivo:** Provar a condição "já mandou um pacote" da regra de energia: sem nenhum pacote, vale a regra do heartbeat.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Idoso novo eletrodependente com consentimento às 08h00; nenhum nó com o token
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 09h00

**Saídas esperadas:**

- Casa nova `Cinza`, `SemHeartbeat`, desde 08h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Cadastrar o idoso com `eletrodependente` verdadeiro e registrar o consentimento | 201 e 200 |
| 2 | Chamar o radar às 09h00 | Casa `Cinza`, `SemHeartbeat`, não `EnergiaEletrodependente` |

**Cobre:** R1

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Energia_NuncaMandouPacote_Cinza` (proposto)

### TC-F12-07

**Energia vence calor: a primeira regra define o motivo**

**Feature:** F12

**Tipo:** Negativo

**Objetivo:** Provar que a regra 1 vem antes das outras e define o motivo quando duas regras pegam a casa.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 1 (eletrodependente)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `TELEMETRIA` com 36 °C e 0 V a cada 5 minutos das 11h00 às 13h30
- Relógio em 13h30

**Saídas esperadas:**

- Casa 1 `Vermelho` com `motivo` `EnergiaEletrodependente`, desde 11h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias | Pacotes aceitos |
| 2 | Chamar o radar às 13h30 | Casa 1 em `pendentes`, motivo `EnergiaEletrodependente`, não `CalorInterno` |

**Cobre:** R1, TV-SIT-1

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::OrdemDasRegras_EnergiaAntesDeCalor` (proposto)

## F13 Verde só com prova, Aguardando no resto

### TC-F13-01

**Casa sem sinal de vida no prazo fica Aguardando, nunca Verde**

**Feature:** F13

**Tipo:** Negativo

**Objetivo:** Provar que a ausência de alerta não basta para ficar verde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 24 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Nenhum movimento desde 05h00
- Relógio em 09h00

**Saídas esperadas:**

- Casa 24 em `aguardando`; `verdes` não a conta

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar o radar às 09h00 | Casa 24 em `aguardando`, fora de `verdes` |

**Cobre:** TV-SIT-6, NC-4

**Caso do modelo:** CT-1

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso01_SozinhoSemSinal_09h_Aguardando` (proposto)

### TC-F13-02

**Visita EstavaBem depois do vermelho deixa a casa Verde**

**Feature:** F13

**Tipo:** Positivo

**Objetivo:** Provar que a confirmação com pessoa vista é sinal de vida e leva a casa a verde na janela atual.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 6 (`Sozinho`, papel D da demo) vermelha desde 10h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` às 10h12
- Relógio em 10h15

**Saídas esperadas:**

- Casa 6 conta em `verdes` às 10h15

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar a visita `EstavaBem` com `ocorridaEm` 10h12 | API responde 201 |
| 2 | Chamar o radar às 10h15 | Casa 6 fora de `pendentes` e de `atendidas`; `verdes` a inclui |

**Cobre:** TV-SIT-5, NC-4

**Caso do modelo:** CT-8

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso08_VisitaEstavaBem_Verde` (proposto)

### TC-F13-03

**Sinal de vida da manhã não deixa a casa verde à tarde**

**Feature:** F13

**Tipo:** Negativo

**Objetivo:** Provar que o verde exige sinal de vida na janela atual.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 25 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 07h00
- Relógio em 12h30

**Saídas esperadas:**

- Casa 25 em `aguardando`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 07h00 | Pacote aceito |
| 2 | Chamar o radar às 12h30 | Casa 25 em `aguardando`, sem alerta e fora de `verdes` |

**Cobre:** TV-SIT-6, NC-4

**Caso do modelo:** CT-6

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso06_SoManha_1230_Aguardando` (proposto)

### TC-F13-04

**Uma visita não cobre o turno inteiro**

**Feature:** F13

**Tipo:** Negativo

**Objetivo:** Provar que a visita das 09h vale só até as 10h e que a casa volta a vermelho às 15h.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 26 (`Sozinho`) sem nenhum evento do sensor
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` às 09h00
- Relógio em 09h30, 10h00 e 15h00

**Saídas esperadas:**

- 09h30: `Verde`
- 10h00: `Aguardando`
- 15h00: `Vermelho`, desde 15h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar a visita às 09h00 e chamar o radar às 09h30 | Casa 26 conta em `verdes` |
| 2 | Chamar o radar às 10h00 | Casa 26 em `aguardando` |
| 3 | Chamar o radar às 15h00 | Casa 26 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, desde 15h00 |

**Cobre:** TV-SIT-5, TV-SIT-6, NC-4

**Caso do modelo:** CT-9

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso09_VisitaNaoCobreOTurno` (proposto)

### TC-F13-05

**Dia comum não tem casa verde**

**Feature:** F13

**Tipo:** Negativo

**Objetivo:** Provar que fora do turno nenhuma casa é verde, mesmo com movimento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 27 (`Sozinho`)
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `MOVIMENTO` com `ts` 09h00
- Relógio em 09h05

**Saídas esperadas:**

- `turnoAberto` falso
- Casa 27 em `aguardando`
- `verdes` igual a 0

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar `MOVIMENTO` com `ts` 09h00 | Pacote aceito |
| 2 | Chamar o radar às 09h05 | `turnoAberto` falso; casa 27 em `aguardando`; `verdes` 0 |

**Cobre:** TV-SIT-7, NC-4

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::DiaComum_SemVerde` (proposto)

### TC-F13-06

**Depois do encerramento não existe verde nem vermelho por silêncio**

**Feature:** F13

**Tipo:** Negativo

**Objetivo:** Provar que o turno encerrado só mantém as regras de energia e de heartbeat.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Turno encerrado às 17h30 por `POST /api/dias-criticos/2026-11-18/encerramento`
- Casa 28 (`Sozinho`) com último movimento às 15h20
- Casa 29 (`Sozinho`) sem sinal de vida desde 10h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 18h30

**Saídas esperadas:**

- Casas 28 e 29 em `aguardando`
- `verdes` igual a 0
- `turnoAberto` falso

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Encerrar o turno às 17h30 | API responde 200 com o resumo |
| 2 | Chamar o radar às 18h30 | Casas 28 e 29 em `aguardando`; nenhuma `SemMovimentoNaJanela`; `verdes` 0 |

**Cobre:** TV-SIT-7, P13

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::TurnoEncerrado_SoEnergiaEHeartbeat` (proposto)

### TC-F13-07

**Depois do encerramento o aparelho mudo ainda aparece Cinza**

**Feature:** F13

**Tipo:** Positivo

**Objetivo:** Provar que a regra de heartbeat continua valendo depois do encerramento do turno.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Turno encerrado às 17h30
- Casa 10 com o nó parado desde 17h40
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 18h00

**Saídas esperadas:**

- Casa 10 `Cinza`, `SemHeartbeat`, pendente

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Parar o nó da casa 10 às 17h40 | Nenhum pacote novo |
| 2 | Chamar o radar às 18h00 | Casa 10 em `pendentes`, `Cinza` |

**Cobre:** P13, TV-SIT-2

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::TurnoEncerrado_HeartbeatContinua` (proposto)

### TC-F13-08

**O turno fecha sozinho às 18h com o resumo**

**Feature:** F13

**Tipo:** Positivo

**Objetivo:** Provar que o vigia do radar encerra o turno às 18h quando ninguém encerrou, e grava o resumo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Ninguém encerrou o turno
- Simulador das 39 casas ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 17h59 e em 18h00 mais um ciclo do vigia (5 s na demo)

**Saídas esperadas:**

- Às 17h59, `turnoAberto` verdadeiro
- Depois das 18h00, `turnoAberto` falso e `POST .../encerramento` devolve o resumo gravado às 18h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar o radar às 17h59 | `turnoAberto` verdadeiro |
| 2 | Mover o relógio para 18h00 e esperar 5 s | O vigia encerra o turno |
| 3 | Chamar `POST /api/dias-criticos/2026-11-18/encerramento` | 200 com `encerradoEm` 18h00 e os cinco números do resumo |

**Cobre:** P13

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::EncerraTurnoAs18h` (proposto)

## F14 Atendimento por confirmação humana

### TC-F14-01

**Visita da ACS atende o alerta de energia na janela**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar a linha da tabela 5.7: energia, ACS, pessoa vista, atendida.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 vermelha por 0 V desde 14h20
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` às 14h40
- Relógio em 14h41

**Saídas esperadas:**

- Casa 1 em `atendidas`, `Vermelho`, `atendida` verdadeiro

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar a visita às 14h40 | 201 |
| 2 | Chamar o radar às 14h41 | Casa 1 em `atendidas`, ainda `Vermelho`, fora de `verdes` |

**Cobre:** TV-ATD-1

**Caso do modelo:** CT-19

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_EnergiaAcsPessoaVista` (proposto)

### TC-F14-02

**Vizinho não resolve alerta de energia**

**Feature:** F14

**Tipo:** Negativo

**Objetivo:** Provar que a resposta EstaBem do vizinho não atende o alerta de energia.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 vermelha por 0 V desde 14h20, com aviso ao vizinho aberto
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `POST /api/avisos/{id}/resposta` com `EstaBem` às 14h30
- Relógio em 14h31

**Saídas esperadas:**

- Resposta 200
- A confirmação `Vizinho` aparece na linha do tempo
- Casa 1 continua em `pendentes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Responder o aviso com `EstaBem` às 14h30 | 200 com `respondidoEm` 14h30 |
| 2 | Abrir a linha do tempo da casa 1 | Confirmação `Vizinho`, `PessoaVista`, 14h30 |
| 3 | Chamar o radar às 14h31 | Casa 1 em `pendentes`, `atendida` falso |

**Cobre:** TV-ATD-2

**Caso do modelo:** CT-19

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_EnergiaVizinho_NaoAtende` (proposto)

### TC-F14-03

**Estava fora não atende alerta de energia nem de aparelho mudo**

**Feature:** F14

**Tipo:** Negativo

**Objetivo:** Provar que a ausência confirmada só atende o alerta de silêncio.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 1 vermelha por 0 V desde 14h20
- Casa 10 cinza desde 09h10 (TC-F09-01)
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaFora` na casa 1 às 14h40
- Visita `EstavaFora` na casa 10 às 10h30

**Saídas esperadas:**

- Casa 1 continua em `pendentes`
- Casa 10 continua em `pendentes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar `EstavaFora` na casa 1 às 14h40 e chamar o radar às 14h41 | Casa 1 em `pendentes` |
| 2 | Registrar `EstavaFora` na casa 10 às 10h30 e chamar o radar às 10h31 | Casa 10 em `pendentes`, `Cinza` |

**Cobre:** TV-ATD-3, TV-ATD-5

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_AusenciaSoAtendeSilencio` (proposto)

### TC-F14-04

**ACS ou vizinho atendem o cinza com pessoa vista**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar que as duas origens atendem o alerta de aparelho mudo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 10 cinza desde 09h10 (TC-F09-01), com vizinho indicado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Vizinho responde `EstaBem` às 10h30
- Variante: visita `EstavaBem` da ACS às 10h30

**Saídas esperadas:**

- Casa 10 em `atendidas`, `Cinza`, às 10h31 nos dois cenários

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Responder o aviso com `EstaBem` às 10h30 e chamar o radar às 10h31 | Casa 10 em `atendidas` |
| 2 | Reiniciar a demo, repetir com a visita da ACS e chamar o radar às 10h31 | Casa 10 em `atendidas` |

**Cobre:** TV-ATD-4

**Caso do modelo:** CT-11, CT-23

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso23_CinzaAtendidaPeloVizinho` (proposto)

### TC-F14-05

**Vizinho atende o alerta de calor**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar a linha calor, vizinho, pessoa vista, atendida.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`, com vizinho indicado) com o cenário de calor do TC-F11-01: 35 °C desde 11h00 e movimento às 13h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Vizinho responde `EstaBem` às 13h20 no aviso aberto pelo alerta de calor
- Relógio em 13h21

**Saídas esperadas:**

- Casa 13 em `atendidas`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar o radar às 13h05 e `GET /api/avisos?microarea=3` | Casa 13 `Vermelho`, `CalorInterno`, pendente; aviso aberto para a casa 13 |
| 2 | Responder o aviso com `EstaBem` às 13h20 | 200 |
| 3 | Chamar o radar às 13h21 | Casa 13 em `atendidas`, `Vermelho`, `CalorInterno` |

**Cobre:** TV-ATD-6

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_CalorVizinho` (proposto)

### TC-F14-06

**Estava fora não atende alerta de calor**

**Feature:** F14

**Tipo:** Negativo

**Objetivo:** Provar que a ausência confirmada não atende o calor.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 8 vermelha por calor desde 13h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaFora` às 13h20
- Relógio em 13h21

**Saídas esperadas:**

- Casa 8 em `pendentes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar `EstavaFora` às 13h20 | 201 |
| 2 | Chamar o radar às 13h21 | Casa 8 em `pendentes` |

**Cobre:** TV-ATD-7

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_CalorAusencia_NaoAtende` (proposto)

### TC-F14-07

**Estava fora atende o silêncio só na janela atual**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar a linha silêncio, ACS, ausência confirmada, atendida, e o limite da janela.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 30 (`Sozinho`) vermelha por silêncio desde 10h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaFora` às 10h40
- Relógio em 10h41 e em 15h00

**Saídas esperadas:**

- 10h41: `Vermelho`, atendida
- 15h00: `Vermelho`, pendente, desde 15h00
- Nunca `Verde`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar `EstavaFora` às 10h40 e chamar o radar às 10h41 | Casa 30 em `atendidas`, `Vermelho` |
| 2 | Chamar o radar às 15h00 | Casa 30 em `pendentes`, desde 15h00; nunca em `verdes` |

**Cobre:** TV-ATD-8

**Caso do modelo:** CT-22

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Caso22_EstavaFora_AtendeSoNaJanela` (proposto)

### TC-F14-08

**Pessoa vista no silêncio é sinal de vida, não atendimento**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar que a confirmação com pessoa vista desfaz o alerta de silêncio em vez de só atendê-lo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 6 vermelha por silêncio desde 10h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Visita `EstavaBem` às 10h12
- Relógio em 10h15

**Saídas esperadas:**

- Casa 6 fora de `atendidas` e conta em `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar `EstavaBem` às 10h12 | 201 |
| 2 | Chamar o radar às 10h15 | Casa 6 não está em `atendidas`; está em `verdes` |

**Cobre:** TV-ATD-9

**Caso do modelo:** CT-8

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/AvaliadorDeCasasTestes.cs::Atendimento_SilencioPessoaVista_ViraSinalDeVida` (proposto)

### TC-F14-09

**Confirmação reenviada com o mesmo Id não duplica**

**Feature:** F14

**Tipo:** Negativo

**Objetivo:** Provar que o Motor ignora a confirmação repetida.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 30 ativa
- Acesso ao Swagger como `acs-demo-01`

**Entradas:**

- `POST /api/visitas` duas vezes com o mesmo `id` e `EstavaBem`

**Saídas esperadas:**

- Primeira resposta 201, segunda 200
- Uma confirmação na linha do tempo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a visita | 201 |
| 2 | Reenviar o mesmo corpo | 200 com o mesmo `id` e `registradaEm` |
| 3 | Abrir a linha do tempo da casa 30 | Uma confirmação `Acs`, `PessoaVista` |

**Cobre:** INV-CasaMonitorada-4, IDEM-Motor-Confirmar

**Caso do modelo:** CT-37

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_IdRepetido_Ignora` (proposto)

### TC-F14-10

**Duas confirmações diferentes ficam as duas**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar que a chave é o Id: visitas diferentes geram confirmações diferentes.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 30 ativa

**Entradas:**

- Duas visitas `EstavaBem` com `id` diferentes, às 10h12 e às 11h00

**Saídas esperadas:**

- Duas confirmações na linha do tempo, em ordem

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar as duas visitas | 201 nas duas |
| 2 | Abrir a linha do tempo | Duas confirmações, 10h12 e 11h00 |

**Cobre:** INV-CasaMonitorada-4, IDEM-Motor-Confirmar

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_IdsDiferentes_GuardaAmbas` (proposto)

### TC-F14-11

**Confirmação no futuro ou com mais de 24 horas é recusada**

**Feature:** F14

**Tipo:** Negativo

**Objetivo:** Provar a tolerância de 2 minutos e a retenção de 24 horas da confirmação.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 30 ativa
- Relógio de demo em 10h00 de 18/11

**Entradas:**

- Visita com `ocorridaEm` 10h05, 5 minutos à frente do servidor
- Visita com `ocorridaEm` 09h00 de 17/11, 25 horas antes

**Saídas esperadas:**

- A visita das 10h05 recebe 422 `VISITA_NO_FUTURO` e nenhuma confirmação chega ao Motor
- A visita de 25 horas é gravada e responde 201, sem confirmação no Motor

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `POST /api/visitas` com `ocorridaEm` 10h05 | 422 `VISITA_NO_FUTURO`; a linha do tempo não muda |
| 2 | Enviar `POST /api/visitas` com `ocorridaEm` 2026-11-17T09:00-03:00 | 201; a linha do tempo não ganha confirmação |

**Cobre:** INV-CasaMonitorada-5

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_ForaDaJanelaDeTempo_Recusa` (proposto)

### TC-F14-12

**Confirmação dentro da tolerância do relógio é aceita**

**Feature:** F14

**Tipo:** Positivo

**Objetivo:** Provar que uma confirmação até 2 minutos à frente do servidor é aceita.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 30 ativa
- Relógio de demo em 10h00

**Entradas:**

- Visita `EstavaBem` com `ocorridaEm` 10h01

**Saídas esperadas:**

- 201
- Confirmação das 10h01 na linha do tempo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a visita com `ocorridaEm` 10h01 | 201 |
| 2 | Abrir a linha do tempo | Confirmação `Acs` às 10h01 |

**Cobre:** INV-CasaMonitorada-5

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/CasaMonitoradaTestes.cs::Confirmar_DentroDaTolerancia_Aceita` (proposto)

## F15 Radar da microárea

### TC-F15-01

**Radar ordena vermelho por energia, vermelho por silêncio e cinza pelo alerta mais antigo**

**Feature:** F15

**Tipo:** Positivo

**Objetivo:** Provar a ordem de `pendentes` e o conteúdo de cada card.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 1 (`Sozinho`, eletrodependente) com `TELEMETRIA` de `tensaoV` 0 às 10h02
- Casas 5, 6 e 7 sem sinal de vida na manhã (papéis C, D e E)
- Casa 10 muda desde 09h10; nó simulado da casa 14 parado às 09h50

**Entradas:**

- `GET /api/radar?microarea=3` às 10h05

**Saídas esperadas:**

- Em `pendentes`, a casa 1 antes das casas 5, 6 e 7, que vêm antes das casas 10 e 14
- A casa 10, desde 09h10, antes da casa 14, desde 09h50
- Cada card com `casaToken`, `situacao`, `motivo`, `desde`, `atendida`, `perfil` e `ultimoPacoteEm`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h05 e chamar o radar | API responde 200 com `turnoAberto` verdadeiro e `agora` 10h05 |
| 2 | Achar os cards das casas 1, 5, 6, 7, 10 e 14 em `pendentes` | Casa 1 `Vermelho` `EnergiaEletrodependente` desde 10h02; casas 5, 6 e 7 `Vermelho` `SemMovimentoNaJanela` desde 10h00; casas 10 e 14 `Cinza` `SemHeartbeat` |
| 3 | Conferir a posição de cada card na lista | Nenhum `Cinza` antes de um `Vermelho`; a energia antes do silêncio; a casa 10 antes da casa 14 |
| 4 | Conferir as chaves de um card | Todas as sete chaves presentes; `perfil` com `contato` e `eletrodependente` |

**Cobre:** `GET /radar?microarea={id}`

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RadarTestes.cs::Pendentes_VermelhoAntesDeCinza_OrdemDaRegra_DesdeMaisAntigo` (proposto)

### TC-F15-02

**Casa atendida aparece em atendidas e não soma em verdes**

**Feature:** F15

**Tipo:** Positivo

**Objetivo:** Provar que a casa atendida sai de `pendentes`, mantém a situação e não conta como verde.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 7 vermelha por silêncio desde 10h00 (papel E)

**Entradas:**

- `POST /api/visitas` `EstavaFora` da casa 7 com `ocorridaEm` 10h40
- Radar às 10h41

**Saídas esperadas:**

- Casa 7 em `atendidas`, `Vermelho`, `atendida` verdadeiro
- `verdes` igual ao de antes da visita

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h39, chamar o radar e anotar `verdes` | Casa 7 em `pendentes` |
| 2 | Mover o relógio para 10h40 e registrar a visita `EstavaFora` | API responde 201 |
| 3 | Mover o relógio para 10h41 e chamar o radar | Casa 7 em `atendidas`, `Vermelho`, `atendida` verdadeiro; fora de `pendentes`; `verdes` igual ao anotado |

**Cobre:** `GET /radar?microarea={id}`

**Caso do modelo:** CT-22

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RadarTestes.cs::Atendida_ForaDePendentes_NaoContaVerde` (proposto)

### TC-F15-03

**Casa com saída avisada aparece em aguardando com a marca**

**Feature:** F15

**Tipo:** Positivo

**Objetivo:** Provar o formato da lista `aguardando`, só com token e saída avisada.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 4 (`Sozinho`) com `BOTAO_SAIR` às 07h30 (papel B)

**Entradas:**

- Radar às 09h00 e às 10h05

**Saídas esperadas:**

- Às 09h00, casa 4 conta em `verdes`
- Às 10h05, casa 4 em `aguardando` com `saidaAvisada` verdadeiro e nunca em `pendentes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h00 e chamar o radar | A casa 4 não está em nenhuma lista; `verdes` a conta |
| 2 | Mover o relógio para 10h05 e chamar o radar | Em `aguardando`, o item da casa 4 tem só `casaToken` e `saidaAvisada` verdadeiro |

**Cobre:** `GET /radar?microarea={id}`

**Caso do modelo:** CT-17

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RadarTestes.cs::Caso17_SaidaAvisada_AguardandoComMarca` (proposto)

### TC-F15-04

**Radar de outra microárea é recusado com 403**

**Feature:** F15

**Tipo:** Negativo

**Objetivo:** Provar que o host confere a microárea da rota contra a da ACS autenticada.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3

**Entradas:**

- `GET /api/radar?microarea=4`

**Saídas esperadas:**

- 403 `MICROAREA_DE_OUTRA_ACS` (seção 7.2 do modelo de dados)
- Nenhum card no corpo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar o radar da microárea 4 | API responde 403 `MICROAREA_DE_OUTRA_ACS` |
| 2 | Ler o corpo | Só `codigo` e `mensagem`; nenhum token nem lista |

**Cobre:** `GET /radar?microarea={id}`

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RadarTestes.cs::OutraMicroarea_403` (proposto)

### TC-F15-05

**Radar não traz nome, nem casa revogada, nem idoso sem consentimento**

**Feature:** F15

**Tipo:** Negativo

**Objetivo:** Provar que o radar só tem token e só casas ativas.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Idoso novo cadastrado por `POST /api/idosos` na microárea 3 (`Amélia Cedro`, `Rua do Sabiá, 99 - Vila Exemplo`, `Sozinho`), sem consentimento
- Casa 16 revogada às 09h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Radar às 09h30

**Saídas esperadas:**

- Nenhum card com o token antigo da casa 16 nem do idoso novo
- Nenhum nome, endereço ou telefone na resposta
- `verdes` mais os cards somam 39 casas

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h30 e chamar o radar | API responde 200 |
| 2 | Procurar o token antigo da casa 16 nas três listas | Não aparece |
| 3 | Procurar `Vila Exemplo`, `Ivo Copaíba` e `(00) 9` na resposta | Nada aparece |
| 4 | Somar `verdes` e os itens das três listas | 39 |

**Cobre:** `GET /radar?microarea={id}`

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RadarTestes.cs::SoCasasAtivas_SemNome` (proposto)

## F16 Linha do tempo da casa

### TC-F16-01

**Linha do tempo traz eventos e confirmações das 24 horas em ordem de ocorrência**

**Feature:** F16

**Tipo:** Positivo

**Objetivo:** Provar a ordem por `ocorridoEm`, a marca de bufferizado e a presença das confirmações.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Nó simulado da casa publicando `HEARTBEAT` a cada 20 s, para o aparelho estar ligado
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 (`Sozinho`)

**Entradas:**

- `MOVIMENTO` com `ts` 09h00
- Depois, `MOVIMENTO` bufferizado com `ts` 08h30
- Visita `EstavaBem` com `ocorridaEm` 10h12
- `GET /api/casas/{token}/linha-do-tempo` às 10h15

**Saídas esperadas:**

- Eventos 08h30 e 09h00 nessa ordem
- O de 08h30 com `bufferizado` verdadeiro
- Uma confirmação `Acs`, `PessoaVista`, `quando` 10h12

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h05 e publicar o `MOVIMENTO` das 09h00 e depois o bufferizado das 08h30 | Os dois pacotes aceitos |
| 2 | Mover o relógio para 10h12 e registrar a visita `EstavaBem` | API responde 201 |
| 3 | Mover o relógio para 10h15 e pedir a linha do tempo | 200; `eventos` com 08h30 antes de 09h00; o de 08h30 com `bufferizado` verdadeiro; `confirmacoes` com `Acs`, `PessoaVista`, 10h12 |

**Cobre:** `GET /casas/{token}/linha-do-tempo`

**Caso do modelo:** CT-13

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::OrdemDeOcorridoEm_ComBufferizado` (proposto)

### TC-F16-02

**Linha do tempo não traz evento com mais de 24 horas**

**Feature:** F16

**Tipo:** Negativo

**Objetivo:** Provar que a linha do tempo mostra só as últimas 24 horas.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- As horas de `ts` dos passos são horas de demo: o `TradutorDePacote` soma ao `ts` publicado o deslocamento do relógio (seção 5.9 do modelo de domínio)
- Casa 13 com `MOVIMENTO` de `ts` 18/11 às 06h12 e `MOVIMENTO` de 19/11 às 06h00

**Entradas:**

- Linha do tempo pedida em 19/11 às 07h00

**Saídas esperadas:**

- Só o evento de 19/11 às 06h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 19/11 às 07h00 e pedir a linha do tempo | API responde 200 |
| 2 | Ler `eventos` | O evento de 19/11 às 06h00 aparece; o de 18/11 às 06h12 não |

**Cobre:** `GET /casas/{token}/linha-do-tempo`

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::SoUltimas24Horas` (proposto)

### TC-F16-03

**Token desativado responde 404 CASA_DESCONHECIDA igual a token que nunca existiu**

**Feature:** F16

**Tipo:** Negativo

**Objetivo:** Provar que a resposta não revela se o token já existiu.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Casa 16 revogada, token antigo anotado

**Entradas:**

- Linha do tempo do token antigo da casa 16
- Linha do tempo do token `00000000000000000000000000000abc`

**Saídas esperadas:**

- 404 `CASA_DESCONHECIDA` nas duas chamadas, com o mesmo corpo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Pedir a linha do tempo do token antigo | API responde 404 `CASA_DESCONHECIDA` |
| 2 | Pedir a linha do tempo do token que nunca existiu | API responde 404 `CASA_DESCONHECIDA` |
| 3 | Comparar os dois corpos | Mesmo `codigo` e mesma `mensagem`; nenhum dos dois traz o token |

**Cobre:** `GET /casas/{token}/linha-do-tempo`

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::CasaDesativada_404_IgualDesconhecida` (proposto)

### TC-F16-04

**Linha do tempo não traz nome, endereço, telefone nem observação da visita**

**Feature:** F16

**Tipo:** Negativo

**Objetivo:** Provar que a casa monitorada não tem dado pessoal nem texto livre.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- Acesso de leitura ao Postgres da demo, como observador
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 6 com visita `EstavaFora` e `observacao` `vizinha disse que foi à feira`

**Entradas:**

- `GET /api/casas/{token}/linha-do-tempo` da casa 6

**Saídas esperadas:**

- Resposta só com `casaToken`, `ultimoPacoteEm`, `eventos` e `confirmacoes`
- Confirmação só com `origem`, `resultado` e `quando`
- Nenhuma coluna de nome ou texto em `motor.casa_monitorada`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Pedir a linha do tempo da casa 6 | API responde 200 |
| 2 | Procurar `Iracema`, `Vila Exemplo`, `(00) 9` e `feira` na resposta | Nada aparece |
| 3 | Ler as chaves de cada confirmação | Só `origem`, `resultado` e `quando` |
| 4 | Listar as colunas de `motor.casa_monitorada` | Nenhuma de nome, endereço, telefone ou texto livre |

**Cobre:** `GET /casas/{token}/linha-do-tempo`, INV-CasaMonitorada-6

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/LinhaDoTempoTestes.cs::SemDadoPessoalNemTextoLivre` (proposto)

### TC-F16-05

**Tela de detalhe junta no celular o nome à linha do tempo que só tem token**

**Feature:** F16

**Tipo:** Positivo

**Objetivo:** Provar que o nome vem da lista de nomes no SQLite do app e não do Motor.

**Preparação:**

- Carga da demo aplicada e simulador das 39 casas ligado, com os papéis da seção 8.1 do modelo de dados
- Swagger ou app autenticado como `acs-demo-01`, a ACS da microárea 3
- App instalado, ACS `acs-demo-01` com a lista de nomes sincronizada
- Casa 13 com eventos do dia

**Entradas:**

- Toque no card da casa 13 no radar do app

**Saídas esperadas:**

- A tela de detalhe mostra `Sebastião Pitangueira` e os eventos da casa
- A chamada de rede da tela é `GET /api/casas/{token}/linha-do-tempo`, sem nome na resposta

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Abrir o radar no app e tocar no card da casa 13 | A tela de detalhe abre com o nome `Sebastião Pitangueira` e o endereço |
| 2 | Conferir os eventos mostrados | Os mesmos eventos da resposta da linha do tempo, em ordem de ocorrência |
| 3 | Ler a resposta da linha do tempo pelo Swagger | Só o token identifica a casa; nenhum nome |

**Cobre:** `GET /casas/{token}/linha-do-tempo`, INV-CasaMonitorada-6

## F17 Visita e desfecho

### TC-F17-01

**EstavaBem e Encaminhada tiram do vermelho a casa Sozinho em silêncio**

**Feature:** F17

**Tipo:** Positivo

**Objetivo:** Provar que os dois desfechos com pessoa vista são gravados, viram confirmação `Acs` e `PessoaVista` e deixam a casa verde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 6 (D da cena das 10h) e casa 13, ambas `Sozinho`, sem sinal de vida desde a abertura
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita `EstavaBem` na casa 6 às 10h12, com `observacao` "conversou na porta e tomou água"
- Visita `Encaminhada` na casa 13 às 10h20
- Radar às 10h15 e às 10h21

**Saídas esperadas:**

- As duas visitas respondem 201 com `id` e `registradaEm`
- Linha do tempo de cada casa com uma confirmação `Acs`, `PessoaVista`, no `quando` da visita
- Casas 6 e 13 contam em `verdes`
- A observação fica gravada como foi enviada

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h00 e chamar `GET /api/radar?microarea=3` | Casas 6 e 13 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, desde 10h00 |
| 2 | Mover o relógio para 10h12 e enviar `POST /api/visitas` com `EstavaBem` e a observação para o token da casa 6 | 201 com o mesmo `id` enviado e `registradaEm` |
| 3 | Mover o relógio para 10h15 e chamar `GET /api/radar?microarea=3` | Casa 6 fora de `pendentes` e de `atendidas`; `verdes` a inclui |
| 4 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 6 | Uma confirmação `Acs`, `PessoaVista`, `quando` 10h12 |
| 5 | Ler `cadastro.visita` pelo `id` da visita | Desfecho `EstavaBem` e a observação igual ao texto enviado |
| 6 | Mover o relógio para 10h20 e enviar `POST /api/visitas` com `Encaminhada` para o token da casa 13 | 201 |
| 7 | Mover o relógio para 10h21 e chamar `GET /api/radar?microarea=3` e abrir a linha do tempo da casa 13 | Casa 13 conta em `verdes`; linha do tempo com `Acs`, `PessoaVista`, 10h20 |

**Cobre:** `POST /visitas`, DESF-EstavaBem, DESF-Encaminhada, P11, INV-Visita-4, INV-Visita-5

**Caso do modelo:** CT-8

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso08_EstavaBemEEncaminhada_ConfirmamPessoaVista` (proposto)

### TC-F17-02

**NaoAbriu não confirma a casa, que continua vermelha e pendente**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que o desfecho `NaoAbriu` é gravado, mas o Cadastro não chama o Motor e a casa não sai das pendentes.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 7 (E da cena das 10h, `Sozinho`) sem sinal de vida desde a abertura
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita `NaoAbriu` na casa 7 às 10h40
- Radar às 10h41

**Saídas esperadas:**

- Visita 201 e gravada em `cadastro.visita`
- Nenhuma confirmação nova na linha do tempo
- Casa 7 em `pendentes`, `Vermelho`, desde 10h00, `atendida` falso
- O app mostra "SAMU chamado"

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h40 e registrar `NaoAbriu` na casa 7 pelo app | Card da casa 7 mostra "SAMU chamado"; a API responde 201 |
| 2 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 7 | Nenhuma confirmação; só os eventos que já existiam |
| 3 | Mover o relógio para 10h41 e chamar `GET /api/radar?microarea=3` | Casa 7 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, desde 10h00, `atendida` falso; fora de `atendidas` e de `verdes` |
| 4 | Ler `cadastro.visita` pelo `id` | Uma linha com desfecho `NaoAbriu` |

**Cobre:** DESF-NaoAbriu, NC-7, P11

**Caso do modelo:** CT-21

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso21_NaoAbriu_NaoChamaOMotor` (proposto)

### TC-F17-03

**EstavaFora atende o alerta de silêncio só na janela atual**

**Feature:** F17

**Tipo:** Positivo

**Objetivo:** Provar que a ausência confirmada atende o vermelho por silêncio até o fim da janela e nunca deixa a casa verde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 14 (`Sozinho`) sem sinal de vida desde a abertura
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita `EstavaFora` às 10h40
- Radar às 10h41 e às 15h00

**Saídas esperadas:**

- 10h41: casa 14 em `atendidas`, `Vermelho`, `atendida` verdadeiro
- 15h00: casa 14 em `pendentes`, `Vermelho`, desde 15h00
- Casa 14 nunca conta em `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h40 e registrar `EstavaFora` na casa 14 | 201 |
| 2 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 14 | Uma confirmação `Acs`, `AusenciaConfirmada`, `quando` 10h40 |
| 3 | Mover o relógio para 10h41 e chamar `GET /api/radar?microarea=3` | Casa 14 em `atendidas`, `Vermelho`, `SemMovimentoNaJanela`, desde 10h00; `verdes` não a conta |
| 4 | Mover o relógio para 15h00 e chamar `GET /api/radar?microarea=3` | Casa 14 em `pendentes`, `Vermelho`, desde 15h00, `atendida` falso; `verdes` não a conta |

**Cobre:** DESF-EstavaFora, NC-7, P11

**Caso do modelo:** CT-22

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso22_EstavaFora_AtendeSoNaJanela` (proposto)

### TC-F17-04

**EstavaFora antes do prazo não vira sinal de vida nem atende o alerta seguinte**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que a ausência confirmada nunca é sinal de vida e não atende um alerta com `desde` posterior a ela.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`) sem sinal de vida desde a abertura
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita `EstavaFora` às 09h00
- Radar às 09h01 e às 10h00

**Saídas esperadas:**

- 09h01: casa 13 em `aguardando`, fora de `verdes`
- 10h00: casa 13 em `pendentes`, `Vermelho`, desde 10h00, `atendida` falso

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h00 e registrar `EstavaFora` na casa 13 | 201; linha do tempo com `Acs`, `AusenciaConfirmada`, 09h00 |
| 2 | Mover o relógio para 09h01 e chamar `GET /api/radar?microarea=3` | Casa 13 em `aguardando`; `verdes` não a conta |
| 3 | Mover o relógio para 10h00 e chamar `GET /api/radar?microarea=3` | Casa 13 em `pendentes`, `Vermelho`, desde 10h00, `atendida` falso, porque a ausência veio antes do `desde` |

**Cobre:** DESF-EstavaFora, NC-7

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::EstavaFora_NuncaESinalDeVida` (proposto)

### TC-F17-05

**Correção do desfecho é uma visita nova e a primeira fica como estava**

**Feature:** F17

**Tipo:** Positivo

**Objetivo:** Provar que a visita é imutável: depois de `NaoAbriu`, a ACS registra outra visita com outro `id` e a casa fica verde.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 7 (`Sozinho`) vermelha desde 10h00
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita A, `NaoAbriu`, às 10h40
- Visita B, `EstavaBem`, com outro `id`, às 10h50
- Radar às 10h51

**Saídas esperadas:**

- As duas visitas respondem 201
- `cadastro.visita` com duas linhas: A ainda `NaoAbriu`, B `EstavaBem`
- Uma confirmação `Acs`, `PessoaVista`, 10h50
- Casa 7 conta em `verdes` às 10h51

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h40 e registrar a visita A, `NaoAbriu` | 201; o app mostra "SAMU chamado" |
| 2 | Mover o relógio para 10h50 e registrar a visita B, `EstavaBem`, com `id` novo | 201 com o `id` de B |
| 3 | Ler `cadastro.visita` da casa 7 | Duas linhas; A continua `NaoAbriu` com a mesma `registradaEm` |
| 4 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 7 | Uma confirmação só, `Acs`, `PessoaVista`, 10h50 |
| 5 | Mover o relógio para 10h51 e chamar `GET /api/radar?microarea=3` | Casa 7 fora de `pendentes`; `verdes` a inclui |

**Cobre:** INV-Visita-1, DESF-NaoAbriu, DESF-EstavaBem, IDEM-Cadastro-RegistrarVisita, P11

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Correcao_EOutraVisita` (proposto)

### TC-F17-06

**App reenvia a visita com o mesmo id e recebe 200 com a visita que existe**

**Feature:** F17

**Tipo:** Positivo

**Objetivo:** Provar que o reenvio da fila do app devolve a visita gravada e chama o Motor de novo sem efeito duplicado.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 6 (`Sozinho`) vermelha desde 10h00
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- `POST /api/visitas` com `id` X e `EstavaBem` às 10h12
- O mesmo corpo reenviado às 10h14

**Saídas esperadas:**

- Primeira resposta 201, segunda 200, com o mesmo `id` e a mesma `registradaEm`
- Uma visita e uma confirmação
- Casa 6 conta em `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h12 e enviar a visita X | 201 com `id` X e `registradaEm` |
| 2 | Mover o relógio para 10h14 e reenviar o mesmo corpo | 200 com `id` X e a mesma `registradaEm` da primeira resposta |
| 3 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 6 | Uma confirmação `Acs`, `PessoaVista`, 10h12 |
| 4 | Mover o relógio para 10h15 e chamar `GET /api/radar?microarea=3` | Casa 6 em `verdes` |

**Cobre:** `POST /visitas`, INV-Visita-2, P18, IDEM-Cadastro-RegistrarVisita

**Caso do modelo:** CT-37

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso37_MesmoId_Devolve200` (proposto)

### TC-F17-07

**Reenvio com o mesmo id e outro desfecho não altera nem duplica a visita**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que a chave é o `id`: um corpo diferente com `id` repetido devolve a visita gravada e não muda o Motor.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 6 com a visita X, `EstavaBem` às 10h12, já gravada (TC-F17-06)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `POST /api/visitas` com `id` X, `NaoAbriu` e `ocorridaEm` 10h20

**Saídas esperadas:**

- 200 com `id` X e a `registradaEm` original
- `cadastro.visita` com uma linha X, desfecho `EstavaBem`, `ocorrida_em` 10h12
- Uma confirmação na linha do tempo
- Casa 6 continua em `verdes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h20 e enviar o corpo com `id` X e `NaoAbriu` | 200 com `id` X e a `registradaEm` da primeira gravação |
| 2 | Contar as linhas de `cadastro.visita` com `id` X | Uma linha, desfecho `EstavaBem`, `ocorrida_em` 10h12 |
| 3 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 6 | Continua uma confirmação `Acs`, `PessoaVista`, 10h12 |
| 4 | Mover o relógio para 10h21 e chamar `GET /api/radar?microarea=3` | Casa 6 em `verdes`, fora de `pendentes` |

**Cobre:** `POST /visitas`, INV-Visita-1, INV-Visita-2, P18, IDEM-Cadastro-RegistrarVisita

**Caso do modelo:** CT-37

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso37_MesmoIdOutroCorpo_NaoAltera` (proposto)

### TC-F17-08

**Visita com token revogado é recusada com TOKEN_INATIVO**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que a visita só existe para idoso com token ativo na hora do registro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 40 usada só neste caso, com o token anotado de `tokens.local.json`
- Consentimento da casa 40 revogado por `DELETE /api/idosos/{id}/consentimento` (204)
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `POST /api/visitas` com `EstavaBem` e o token antigo da casa 40

**Saídas esperadas:**

- 422 com `codigo` `TOKEN_INATIVO`, sem dado pessoal na mensagem
- Nenhuma linha nova em `cadastro.visita`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Revogar o consentimento da casa 40 | 204 |
| 2 | Enviar `POST /api/visitas` com o token antigo | 422 `{"codigo": "TOKEN_INATIVO", ...}`; a mensagem não traz nome nem endereço |
| 3 | Procurar o `id` enviado em `cadastro.visita` | Nenhuma linha |
| 4 | Chamar `GET /api/casas/{token}/linha-do-tempo` com o token antigo | 404 `CASA_DESCONHECIDA` |

**Cobre:** `POST /visitas`, INV-Visita-4

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::TokenRevogado_Recusa422` (proposto)

### TC-F17-09

**Visita dentro da tolerância e visita de ontem são gravadas com 201**

**Feature:** F17

**Tipo:** Positivo

**Objetivo:** Provar a tolerância de 2 minutos sobre a hora deslocada e que a visita de mais de 24 horas é mantida.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`) ativa
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo em 10h00 de 18/11
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Visita `EstavaBem` com `ocorridaEm` 1 minuto à frente da hora real
- Visita `EstavaBem` com `ocorridaEm` 25 horas antes da hora real, como a fila do app que subiu atrasada

**Saídas esperadas:**

- As duas respondem 201
- Linha do tempo com a confirmação de 10h01
- `cadastro.visita` com as duas linhas

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a visita com `ocorridaEm` 1 minuto à frente | 201 |
| 2 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 13 | Confirmação `Acs`, `PessoaVista`, `quando` 10h01, na hora deslocada |
| 3 | Enviar a visita com `ocorridaEm` 25 horas antes | 201 com `id` e `registradaEm` |
| 4 | Ler `cadastro.visita` da casa 13 | Duas linhas, uma com `ocorrida_em` 2026-11-17T09:00-03:00 |

**Cobre:** `POST /visitas`, INV-Visita-3, INV-Visita-7

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::DentroDaToleranciaEVisitaAntiga_Gravam` (proposto)

### TC-F17-10

**Visita no futuro é recusada e visita de ontem não confirma a casa**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que `ocorridaEm` além da tolerância não grava nada e que a visita de mais de 24 horas não chega ao radar.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 14 (`Sozinho`) vermelha desde 10h00
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo em 10h05 de 18/11
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Visita `EstavaBem` com `ocorridaEm` 5 minutos à frente da hora real
- Visita `EstavaBem` com `ocorridaEm` 25 horas antes da hora real

**Saídas esperadas:**

- A primeira recebe 422 `VISITA_NO_FUTURO` e não é gravada
- A segunda recebe 201, mas a linha do tempo não ganha confirmação
- Casa 14 continua em `pendentes`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a visita com `ocorridaEm` 5 minutos à frente | 422 `VISITA_NO_FUTURO` |
| 2 | Procurar o `id` em `cadastro.visita` e abrir a linha do tempo da casa 14 | Nenhuma linha; nenhuma confirmação |
| 3 | Enviar a visita com `ocorridaEm` 25 horas antes | 201 |
| 4 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 14 | Nenhuma confirmação, porque o Motor recusa a de mais de 24 horas |
| 5 | Chamar o radar às 10h06 | Casa 14 em `pendentes`, `Vermelho`, desde 10h00, `atendida` falso |

**Cobre:** `POST /visitas`, INV-Visita-3, INV-Visita-7

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::NoFuturoEAntiga_NaoConfirmam` (proposto)

### TC-F17-11

**CPF na observação da visita é gravado e logado só mascarado**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que a observação não guarda documento em claro: o interceptador mascara antes do banco e do log.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 ativa
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)
- Acesso ao log da API

**Entradas:**

- Visita `EstavaBem` às 10h30 com `observacao` "cuidadora informou o CPF 123.456.789-00"

**Saídas esperadas:**

- 201
- `cadastro.visita.observacao` e o log com `***.456.***-00`, nunca `123.456.789-00`
- `GET /api/painel` com um evento novo de mascaramento, sem o valor

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h30 e enviar a visita com o CPF na observação | 201 |
| 2 | Ler `cadastro.visita.observacao` pelo `id` | Texto com `***.456.***-00`; o resto da frase igual |
| 3 | Procurar `123.456.789-00` no log da API | Nenhuma ocorrência |
| 4 | Chamar `GET /api/painel` | Evento novo do tipo mascaramento na entrada, com o tipo de dado e sem o número |

**Cobre:** INV-Visita-5

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::ObservacaoComCpf_GravaMascarada` (proposto)

### TC-F17-12

**Visita com pessoa vista às 09h não cobre a janela da tarde**

**Feature:** F17

**Tipo:** Negativo

**Objetivo:** Provar que `EstavaBem` e `Encaminhada` valem como sinal de vida só na janela em que ocorreram.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casas 13 e 14 (`Sozinho`) sem sinal de vida desde a abertura
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita `EstavaBem` na casa 13 e `Encaminhada` na casa 14, as duas às 09h00
- Radar às 09h30, 10h00 e 15h00

**Saídas esperadas:**

- 09h30: as duas em `verdes`
- 10h00: as duas em `aguardando`, nenhuma em `verdes`
- 15h00: as duas em `pendentes`, `Vermelho`, desde 15h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h00 e registrar as duas visitas | 201 nas duas |
| 2 | Mover o relógio para 09h30 e chamar `GET /api/radar?microarea=3` | Casas 13 e 14 em `verdes` |
| 3 | Mover o relógio para 10h00 e chamar `GET /api/radar?microarea=3` | Casas 13 e 14 em `aguardando`; nenhuma em `verdes` nem em `pendentes` |
| 4 | Mover o relógio para 15h00 e chamar `GET /api/radar?microarea=3` | Casas 13 e 14 em `pendentes`, `Vermelho`, `SemMovimentoNaJanela`, desde 15h00 |

**Cobre:** DESF-EstavaBem, DESF-Encaminhada

**Caso do modelo:** CT-9

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RegistrarVisitaTestes.cs::Caso09_PessoaVista_ValeSoNaJanela` (proposto)

## F18 Aviso neutro ao vizinho e resposta

### TC-F18-01

**Casas vermelhas às 10h com vizinho autorizado ganham um aviso cada**

**Feature:** F18

**Tipo:** Positivo

**Objetivo:** Provar que o vigia pede contato para cada vermelho pendente e que o Cadastro registra e envia o aviso neutro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casas 5, 6 e 7 (C, D e E da cena das 10h), autorizadas e com vizinho, sem sinal de vida
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso ao log da API

**Entradas:**

- Relógio em 09h59 e em 10h00, mais um ciclo do vigia (5 s na demo)

**Saídas esperadas:**

- Às 09h59, nenhum aviso das casas 5, 6 e 7
- Depois das 10h00, um aviso por casa em `GET /api/avisos?microarea=3`, `SemMovimentoNaJanela`, desde 10h00, sem resposta
- App mostra "vizinho avisado às 10h00"
- Uma linha de log por envio simulado
- Nenhum bloqueio de saída no canal `Vizinho` no painel

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 09h59 e chamar `GET /api/avisos?microarea=3` | 200 sem aviso das casas 5, 6 e 7 |
| 2 | Mover o relógio para 10h00 e esperar 5 s | Radar com as casas 5, 6 e 7 em `pendentes`, `Vermelho`, desde 10h00 |
| 3 | Chamar `GET /api/avisos?microarea=3` | 200 com três avisos, um por token, `motivo` `SemMovimentoNaJanela`, `desde` 10h00, `registradoEm` 10h00, `resposta` nula |
| 4 | Abrir o card da casa 5 no app | Texto "vizinho avisado às 10h00" |
| 5 | Ler o log da API | Três linhas de envio simulado, uma por aviso |
| 6 | Chamar `GET /api/painel` | Nenhum evento `BloqueioDeSaida` com o canal `Vizinho` |

**Cobre:** `GET /avisos?microarea={id}`, P10, P19, INV-AvisoAoVizinho-2, INV-AvisoAoVizinho-4, CANAL-Vizinho

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::PedidoDeContato_ComAutorizacaoEVizinho_RegistraAviso` (proposto)

### TC-F18-02

**Pedido de contato repetido nos ciclos seguintes não cria outro aviso**

**Feature:** F18

**Tipo:** Negativo

**Objetivo:** Provar que o episódio `(casaToken, motivo, desde)` gera um aviso só, mesmo com o pedido repetido a cada ciclo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casas 5, 6 e 7 com aviso das 10h00 (TC-F18-01)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)
- Acesso ao log da API

**Entradas:**

- Vigia rodando por 1 minuto, cerca de 12 ciclos, com as casas ainda pendentes

**Saídas esperadas:**

- `GET /api/avisos` com os mesmos três avisos, mesmo `id` e mesmo `registradoEm`
- Uma linha por casa em `cadastro.aviso_ao_vizinho`
- Nenhuma linha nova de envio simulado no log

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Anotar `id` e `registradoEm` dos três avisos | Três avisos |
| 2 | Esperar 1 minuto com o relógio correndo a partir de 10h00 | Radar continua com as três casas em `pendentes`, desde 10h00 |
| 3 | Chamar `GET /api/avisos?microarea=3` | Os mesmos três avisos, com os mesmos `id` e `registradoEm` |
| 4 | Contar `cadastro.aviso_ao_vizinho` por token da casa 5 | 1 |
| 5 | Ler o log da API depois das 10h00min05s | Nenhuma linha nova de envio simulado |

**Cobre:** INV-AvisoAoVizinho-1, IDEM-Cadastro-PedidoDeContato, P19

**Caso do modelo:** CT-35

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::Caso35_PedidoRepetido_UmAviso` (proposto)

### TC-F18-03

**Cinza com turno aberto e alerta novo das 15h abrem episódios novos**

**Feature:** F18

**Tipo:** Positivo

**Objetivo:** Provar que o cinza pendente com turno gera pedido e que outro `desde` é outro episódio, com outro aviso.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 10 (`Sozinho`, autorizada, com vizinho) com nó parado às 09h10
- Casa 7 vermelha desde 10h00, com aviso das 10h00 e visita `NaoAbriu` às 10h40
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 09h11 mais um ciclo do vigia
- Relógio em 15h00 mais um ciclo do vigia

**Saídas esperadas:**

- Aviso da casa 10 com `motivo` `SemHeartbeat` e `desde` 09h10
- Casa 7 com dois avisos: `desde` 10h00 e `desde` 15h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Parar o nó da casa 10 às 09h10, mover o relógio para 09h11 e esperar 5 s | Radar com a casa 10 em `pendentes`, `Cinza`, desde 09h10 |
| 2 | Chamar `GET /api/avisos?microarea=3` | Um aviso da casa 10, `SemHeartbeat`, `desde` 09h10 |
| 3 | Mover o relógio para 15h00 e esperar 5 s | Radar com a casa 7 em `pendentes`, `Vermelho`, desde 15h00 |
| 4 | Chamar `GET /api/avisos?microarea=3` | Dois avisos da casa 7, com `desde` 10h00 e `desde` 15h00 |

**Cobre:** `GET /avisos?microarea={id}`, P10, P19, INV-AvisoAoVizinho-1, IDEM-Cadastro-PedidoDeContato

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::EpisodioNovo_CriaOutroAviso` (proposto)

### TC-F18-04

**Casa sem autorização ou sem vizinho indicado não gera aviso**

**Feature:** F18

**Tipo:** Negativo

**Objetivo:** Provar que o aviso só existe com a autorização `AvisarVizinho` e o vizinho indicado.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 16 (`Sozinho`, sem `avisarVizinho`) e casa 19 (`Sozinho`, autorizou e não indicou vizinho)
- As duas sem sinal de vida desde a abertura
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Relógio em 10h00 mais 1 minuto de ciclos do vigia

**Saídas esperadas:**

- Casas 16 e 19 em `pendentes`, `Vermelho`, desde 10h00
- Nenhum aviso dessas casas em `GET /api/avisos` nem no banco

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h00 e chamar `GET /api/radar?microarea=3` | Casas 16 e 19 em `pendentes`, `Vermelho`, desde 10h00 |
| 2 | Esperar 1 minuto e chamar `GET /api/avisos?microarea=3` | Nenhum item com o token da casa 16 ou da casa 19 |
| 3 | Contar `cadastro.aviso_ao_vizinho` pelos dois tokens | 0 |

**Cobre:** INV-AvisoAoVizinho-2, P19

**Caso do modelo:** CT-36

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::Caso36_SemAutorizacaoOuSemVizinho_NenhumAviso` (proposto)

### TC-F18-05

**Cinza fora do turno não chama o vizinho, mas a energia chama**

**Feature:** F18

**Tipo:** Negativo

**Objetivo:** Provar que o pedido de contato sai para todo vermelho pendente e para o cinza pendente só com turno aberto.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 10 (`Sozinho`, com vizinho) e casa 1 (`Sozinho`, eletrodependente, com vizinho)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Nós das casas 10 e 1 parados às 03h00
- Relógio em 03h02 mais um ciclo do vigia

**Saídas esperadas:**

- Casa 10 em `pendentes`, `Cinza`, sem aviso
- Casa 1 em `pendentes`, `Vermelho`, `EnergiaEletrodependente`, com aviso desde 03h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 03h00 e parar os nós das casas 10 e 1 | Nenhum pacote novo das duas casas |
| 2 | Mover o relógio para 03h02 e chamar `GET /api/radar?microarea=3` | `turnoAberto` falso; casa 10 `Cinza` e casa 1 `Vermelho`, `EnergiaEletrodependente`, as duas desde o último pacote, perto de 03h00 |
| 3 | Esperar 5 s e chamar `GET /api/avisos?microarea=3` | Nenhum aviso da casa 10; um aviso da casa 1, `EnergiaEletrodependente`, com o mesmo `desde` do radar |

**Cobre:** P10

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::CinzaSemTurno_NaoPedeContato` (proposto)

### TC-F18-06

**Vizinho responde EstaBem e a casa recebe confirmação com pessoa vista**

**Feature:** F18

**Tipo:** Positivo

**Objetivo:** Provar que a resposta `EstaBem` vira confirmação `Vizinho` e `PessoaVista` e que repeti-la não muda nada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 5 (C) vermelha desde 10h00, com aviso aberto (TC-F18-01)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `POST /api/avisos/{id}/resposta` com `{"resposta": "EstaBem"}` às 10h05
- A mesma resposta repetida às 10h07

**Saídas esperadas:**

- 200 com `resposta` `EstaBem` e `respondidoEm` 10h05 nas duas chamadas
- Uma confirmação `Vizinho`, `PessoaVista`, 10h05
- Casa 5 conta em `verdes`
- `GET /api/avisos` mostra a resposta e a hora

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Pegar o `id` do aviso da casa 5 em `GET /api/avisos?microarea=3` | Aviso com `resposta` nula |
| 2 | Mover o relógio para 10h05 e enviar a resposta `EstaBem` | 200 com o `id`, `resposta` `EstaBem` e `respondidoEm` 10h05 |
| 3 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 5 | Uma confirmação `Vizinho`, `PessoaVista`, `quando` 10h05 |
| 4 | Mover o relógio para 10h06 e chamar `GET /api/radar?microarea=3` | Casa 5 fora de `pendentes`; `verdes` a inclui |
| 5 | Mover o relógio para 10h07 e repetir a resposta `EstaBem` | 200 com `respondidoEm` 10h05 |
| 6 | Abrir a linha do tempo e `GET /api/avisos?microarea=3` | Continua uma confirmação; o aviso mostra `EstaBem` e 10h05 |

**Cobre:** `POST /avisos/{id}/resposta`, `GET /avisos?microarea={id}`, P20, INV-AvisoAoVizinho-3, IDEM-Cadastro-RegistrarResposta

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/AvisoAoVizinhoTestes.cs::EstaBem_ConfirmaPessoaVista` (proposto)

### TC-F18-07

**Resposta diferente depois de EstaBem é recusada com AVISO_JA_RESPONDIDO**

**Feature:** F18

**Tipo:** Negativo

**Objetivo:** Provar que o aviso recebe uma resposta só e que a segunda, diferente, não muda o aviso.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Aviso da casa 5 respondido com `EstaBem` às 10h05 (TC-F18-06)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `POST /api/avisos/{id}/resposta` com `{"resposta": "PrecisaDeAjuda"}` às 10h10

**Saídas esperadas:**

- 409 `AVISO_JA_RESPONDIDO`
- Aviso continua `EstaBem` às 10h05
- Linha do tempo sem mudança

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h10 e enviar `PrecisaDeAjuda` para o mesmo aviso | 409 `{"codigo": "AVISO_JA_RESPONDIDO", ...}` |
| 2 | Chamar `GET /api/avisos?microarea=3` | Aviso da casa 5 com `resposta` `EstaBem` e `respondidoEm` 10h05 |
| 3 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 5 | Uma confirmação `Vizinho`, `PessoaVista`, 10h05, e nada mais |

**Cobre:** `POST /avisos/{id}/resposta`, INV-AvisoAoVizinho-3, IDEM-Cadastro-RegistrarResposta

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/AvisoAoVizinhoTestes.cs::RespostaDiferente_Recusa409` (proposto)

### TC-F18-08

**Vizinho responde PrecisaDeAjuda e o Motor não recebe confirmação**

**Feature:** F18

**Tipo:** Negativo

**Objetivo:** Provar que `PrecisaDeAjuda` fica registrado no aviso, mas não confirma nada e a casa continua pendente.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 7 (E) vermelha desde 10h00, com aviso aberto
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `POST /api/avisos/{id}/resposta` com `{"resposta": "PrecisaDeAjuda"}` às 10h05

**Saídas esperadas:**

- 200 com `resposta` `PrecisaDeAjuda`
- Nenhuma confirmação na linha do tempo
- Casa 7 em `pendentes`
- App mostra "vizinho pediu ajuda"

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 10h05 e enviar `PrecisaDeAjuda` para o aviso da casa 7 | 200 com `respondidoEm` 10h05 |
| 2 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 7 | Nenhuma confirmação |
| 3 | Mover o relógio para 10h06 e chamar `GET /api/radar?microarea=3` | Casa 7 em `pendentes`, `Vermelho`, desde 10h00, `atendida` falso |
| 4 | Abrir o card da casa 7 no app | Texto "vizinho pediu ajuda" |

**Cobre:** P20

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/AvisoAoVizinhoTestes.cs::PrecisaDeAjuda_NaoChamaOMotor` (proposto)

### TC-F18-09

**Aviso e lista de avisos não carregam nome, endereço, telefone nem aviso velho**

**Feature:** F18

**Tipo:** Negativo

**Objetivo:** Provar que o aviso é neutro em toda saída observável e que a lista traz só as últimas 24 horas.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 5 com aviso das 10h00 (TC-F18-01)
- Nome, endereço e telefone do vizinho da casa 5 lidos na carga
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)
- Acesso ao log da API

**Entradas:**

- `GET /api/avisos?microarea=3` às 10h01 de 18/11 e às 10h01 de 19/11

**Saídas esperadas:**

- Itens só com `id`, `casaToken`, `motivo`, `desde`, `registradoEm`, `resposta` e `respondidoEm`
- Nem o log nem `cadastro.aviso_ao_vizinho` têm nome, endereço ou telefone
- Em 19/11 às 10h01, o aviso de 18/11 não aparece

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar `GET /api/avisos?microarea=3` às 10h01 | 200; nenhum campo com nome, endereço ou telefone |
| 2 | Procurar "Madalena Paineira", "João-de-barro" e "(00) 90000-0005" no log da API | Nenhuma ocorrência na linha do envio simulado |
| 3 | Ler a linha do aviso em `cadastro.aviso_ao_vizinho` | Só token, microárea, motivo, `desde`, `registrado_em` e resposta; nenhum texto, nome ou telefone |
| 4 | Mover o relógio para 10h01 de 19/11 e chamar `GET /api/avisos?microarea=3` | 200 sem o aviso de 18/11 |

**Cobre:** `GET /avisos?microarea={id}`, INV-AvisoAoVizinho-4, CANAL-Vizinho

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/ReceptorDeContatoTestes.cs::Aviso_SemDadoPessoal` (proposto)

## F19 Dia crítico e turno

### TC-F19-01

**Declarar o dia crítico às 05h abre o turno e acende o âmbar das casas ativas**

**Feature:** F19

**Tipo:** Positivo

**Objetivo:** Provar que a declaração válida cria o dia crítico, abre o turno e publica `DIA_CRITICO` para cada casa ativa.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Nós simulados das 40 casas publicando só `HEARTBEAT` a cada 20 s
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `PUT /api/dias-criticos/2026-11-18` às 05h00 com `{"probabilidade": 0.81, "motivo": "mínima de 24 °C pela segunda noite", "origem": "MODELO"}`

**Saídas esperadas:**

- 200 com `data`, `abertoEm` 05h00, a previsão enviada e `encerradoEm` nulo
- Um `{"tipo": "DIA_CRITICO"}` no tópico de comando de cada uma das 40 casas
- Radar com `turnoAberto` verdadeiro

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 05h00 e enviar a declaração | 200 com `data` 2026-11-18, `abertoEm` 05h00, `probabilidade` 0.81, `origem` `MODELO`, `encerradoEm` nulo |
| 2 | Ler as mensagens do cliente MQTT | Um `DIA_CRITICO` em `corredor/v1/{casaToken}/comando` para cada casa ativa; o nó da casa 1 acende o LED âmbar |
| 3 | Chamar `GET /api/radar?microarea=3` | `turnoAberto` verdadeiro |

**Cobre:** `PUT /dias-criticos/{data}`, P8, INV-DiaCritico-1, INV-DiaCritico-2, INV-DiaCritico-3

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::Declarar_AbreTurnoEPublicaDiaCritico` (proposto)

### TC-F19-02

**Declarar de novo antes do encerramento atualiza a previsão**

**Feature:** F19

**Tipo:** Positivo

**Objetivo:** Provar que a segunda declaração da mesma data é aceita e troca probabilidade, motivo e origem.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- `PUT /api/dias-criticos/2026-11-18` às 05h01 com `probabilidade` 0.85, outro `motivo` e `origem` `MANUAL`

**Saídas esperadas:**

- 200 com `probabilidade` 0.85, o motivo novo, `origem` `MANUAL` e `abertoEm` 05h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 05h01 e enviar a segunda declaração | 200 com `probabilidade` 0.85, o motivo novo, `origem` `MANUAL` |
| 2 | Conferir `abertoEm` e `encerradoEm` na resposta | `abertoEm` 05h00 e `encerradoEm` nulo |

**Cobre:** `PUT /dias-criticos/{data}`, INV-DiaCritico-4, INV-DiaCritico-5, IDEM-Motor-DeclararDiaCritico

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::DeclararDeNovo_AtualizaPrevisao` (proposto)

### TC-F19-03

**Declarar de novo às 05h01 não abre outro turno nem desloca a janela da manhã**

**Feature:** F19

**Tipo:** Negativo

**Objetivo:** Provar que a repetição mantém um dia crítico e `abertoEm` 05h00, então um movimento de 05h00min30s conta.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 13 (`Sozinho`)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Declaração às 05h00
- `MOVIMENTO` da casa 13 às 05h00min30s
- Segunda declaração às 05h01
- Radar às 09h00

**Saídas esperadas:**

- Uma linha em `motor.dia_critico` para 2026-11-18, com `aberto_em` 05h00
- Casa 13 em `verdes` às 09h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 05h00 e declarar o dia crítico | 200 com `abertoEm` 05h00 |
| 2 | Mover o relógio para 05h00min30s e publicar `MOVIMENTO` da casa 13 | Pacote aceito |
| 3 | Mover o relógio para 05h01 e declarar de novo | 200 com `abertoEm` 05h00 |
| 4 | Contar `motor.dia_critico` com data 2026-11-18 | 1 linha, `aberto_em` 05h00 |
| 5 | Mover o relógio para 09h00 e chamar `GET /api/radar?microarea=3` | Casa 13 em `verdes`, porque a janela da manhã continua começando às 05h00 |

**Cobre:** INV-DiaCritico-1, INV-DiaCritico-4, IDEM-Motor-DeclararDiaCritico

**Caso do modelo:** CT-29

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::Caso29_DeclararDuasVezes_UmTurno` (proposto)

### TC-F19-04

**Declaração com data errada, probabilidade fora da faixa ou motivo inválido é recusada**

**Feature:** F19

**Tipo:** Negativo

**Objetivo:** Provar que só a data de hoje e a previsão válida abrem turno, e que a recusa não publica comando.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Nós simulados das 40 casas publicando só `HEARTBEAT` a cada 20 s
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo em 05h00 de 18/11
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `PUT` para 2026-11-17 e para 2026-11-19
- `probabilidade` 1.2 e -0.1
- `motivo` vazio e `motivo` com 201 caracteres

**Saídas esperadas:**

- Data fora de hoje: 422 `DATA_NAO_E_HOJE`
- Probabilidade fora: 422 `PROBABILIDADE_FORA_DA_FAIXA`
- Motivo inválido: recusa; a tabela 6.3 não dá o código deste erro
- Nenhum turno aberto, nenhuma linha em `motor.dia_critico` e nenhum `DIA_CRITICO` publicado

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `PUT /api/dias-criticos/2026-11-17` e `PUT /api/dias-criticos/2026-11-19` com corpo válido | 422 `DATA_NAO_E_HOJE` nas duas |
| 2 | Enviar `PUT /api/dias-criticos/2026-11-18` com `probabilidade` 1.2 e depois -0.1 | 422 `PROBABILIDADE_FORA_DA_FAIXA` nas duas |
| 3 | Enviar `PUT /api/dias-criticos/2026-11-18` com `motivo` vazio e depois com 201 caracteres | Erro com `codigo` e `mensagem`, sem 200 |
| 4 | Chamar o radar e contar `motor.dia_critico` | `turnoAberto` falso; 0 linhas |
| 5 | Ler as mensagens do cliente MQTT | Nenhum `DIA_CRITICO` |

**Cobre:** `PUT /dias-criticos/{data}`, INV-DiaCritico-2, INV-DiaCritico-3, P8

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::DeclaracaoInvalida_Recusa422` (proposto)

### TC-F19-05

**ACS encerra o turno às 17h30 e recebe o resumo de cinco números**

**Feature:** F19

**Tipo:** Positivo

**Objetivo:** Provar que o encerramento grava o resumo com o indicador de impacto e desliga as regras que exigem turno.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Nós simulados das 40 casas publicando só `HEARTBEAT` a cada 20 s
- Casa 13 com `MOVIMENTO` às 07h00 e casa 14 com `MOVIMENTO` às 08h00; nenhuma outra casa com sinal de vida
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Radar às 17h29
- `POST /api/dias-criticos/2026-11-18/encerramento` às 17h30, sem corpo

**Saídas esperadas:**

- 200 com `encerradoEm` 17h30 e `resumo` com `casasAtivas` 40 e `comSinalDeVidaAte10h` 2
- `atendidas`, `vermelhasPendentes` e `cinzasPendentes` iguais às listas do radar das 17h29
- Radar às 17h31 com `turnoAberto` falso e a casa 13 em `aguardando`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 17h29 e chamar `GET /api/radar?microarea=3` | Anotar quantas casas estão em `atendidas` e quantas `Vermelho` e `Cinza` estão em `pendentes` |
| 2 | Mover o relógio para 17h30 e enviar o encerramento | 200 com `data`, `encerradoEm` 17h30 e os cinco números; `casasAtivas` 40 e `comSinalDeVidaAte10h` 2 |
| 3 | Comparar os outros três números com o radar das 17h29 | Iguais |
| 4 | Mover o relógio para 17h31 e chamar `GET /api/radar?microarea=3` | `turnoAberto` falso; casa 13 em `aguardando`, sem alerta de silêncio |
| 5 | Ler `motor.dia_critico` de 2026-11-18 | `encerrado_em` 17h30 e as colunas do resumo preenchidas |

**Cobre:** `POST /dias-criticos/{data}/encerramento`, INV-DiaCritico-7, IDEM-Motor-EncerrarTurno

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::Encerrar_GravaResumo` (proposto)

### TC-F19-06

**Encerrar de novo devolve o mesmo resumo e o vigia das 18h não reencerra**

**Feature:** F19

**Tipo:** Negativo

**Objetivo:** Provar que existe um encerramento por data e que o resumo gravado não muda com fatos posteriores.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Turno encerrado às 17h30 com o resumo anotado (TC-F19-05)
- Casa 7 vermelha e pendente
- Nós simulados das 40 casas publicando só `HEARTBEAT` a cada 20 s
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo

**Entradas:**

- Visita `EstavaBem` na casa 7 às 17h40
- Segundo encerramento às 17h45
- Relógio em 18h00 mais um ciclo do vigia

**Saídas esperadas:**

- Segundo encerramento 200 com `encerradoEm` 17h30 e os mesmos cinco números
- Depois das 18h00, `encerradoEm` continua 17h30

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 17h40 e registrar a visita na casa 7 | 201 |
| 2 | Mover o relógio para 17h45 e enviar o encerramento de novo | 200 com `encerradoEm` 17h30 e o resumo igual ao anotado |
| 3 | Mover o relógio para 18h00, esperar 5 s e enviar o encerramento | 200 com `encerradoEm` 17h30 e o mesmo resumo |

**Cobre:** `POST /dias-criticos/{data}/encerramento`, INV-DiaCritico-7, IDEM-Motor-EncerrarTurno

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::EncerrarDeNovo_MesmoResumo` (proposto)

### TC-F19-07

**Encerrar um dia sem dia crítico responde DIA_CRITICO_DESCONHECIDO**

**Feature:** F19

**Tipo:** Negativo

**Objetivo:** Provar que o encerramento exige um dia crítico declarado para a data.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Relógio de demo em 17h30 de 18/11
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `POST /api/dias-criticos/2026-11-18/encerramento`
- `POST /api/dias-criticos/2026-11-17/encerramento`

**Saídas esperadas:**

- 404 `DIA_CRITICO_DESCONHECIDO` nas duas
- Nenhuma linha em `motor.dia_critico`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o encerramento de 2026-11-18 | 404 `{"codigo": "DIA_CRITICO_DESCONHECIDO", ...}` |
| 2 | Enviar o encerramento de 2026-11-17 | 404 `DIA_CRITICO_DESCONHECIDO` |
| 3 | Contar `motor.dia_critico` | 0 linhas |

**Cobre:** `POST /dias-criticos/{data}/encerramento`

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::EncerrarSemDiaCritico_Recusa404` (proposto)

### TC-F19-08

**Declarar depois do encerramento é recusado e o turno não reabre**

**Feature:** F19

**Tipo:** Negativo

**Objetivo:** Provar que o turno encerrado não reabre e que a recusa não muda a previsão nem publica comando.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Turno encerrado às 17h30
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `PUT /api/dias-criticos/2026-11-18` às 17h35 com `probabilidade` 0.9

**Saídas esperadas:**

- 409 `TURNO_ENCERRADO`
- `turnoAberto` falso
- `motor.dia_critico` com a probabilidade antiga e `encerrado_em` 17h30
- Nenhum `DIA_CRITICO` publicado depois das 17h30

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 17h35 e enviar a declaração | 409 `{"codigo": "TURNO_ENCERRADO", ...}` |
| 2 | Chamar o radar | `turnoAberto` falso |
| 3 | Ler `motor.dia_critico` de 2026-11-18 | Probabilidade 0.81, `aberto_em` 05h00, `encerrado_em` 17h30 |
| 4 | Ler as mensagens do cliente MQTT depois das 17h30 | Nenhum `DIA_CRITICO` |

**Cobre:** `PUT /dias-criticos/{data}`, INV-DiaCritico-5

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/DiaCriticoTestes.cs::DeclararDepoisDoEncerramento_Recusa409` (proposto)

## F20 Pedido de toque às 14h30

### TC-F20-01

**Às 14h30 a casa Sozinho sem sinal de vida desde as 10h recebe um PEDIR_TOQUE**

**Feature:** F20

**Tipo:** Positivo

**Objetivo:** Provar que o vigia pede o toque às 14h30 às casas `Sozinho` sem sinal de vida desde as 10h, inclusive com sinal antes das 10h.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`) sem sinal de vida desde a abertura
- Casa 14 (`Sozinho`) com `MOVIMENTO` às 09h50 e nada depois
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Relógio em 14h29 e em 14h30 mais um ciclo do vigia

**Saídas esperadas:**

- Nenhum `PEDIR_TOQUE` às 14h29
- Um `{"tipo": "PEDIR_TOQUE"}` no tópico de comando das casas 13 e 14
- `motor.dia_critico.toque_pedido_em` 14h30

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 14h29 e esperar 5 s | Nenhum `PEDIR_TOQUE` no cliente MQTT |
| 2 | Mover o relógio para 14h30 e esperar 5 s | Um `PEDIR_TOQUE` em `corredor/v1/{token da casa 13}/comando` e um no da casa 14 |
| 3 | Ler `motor.dia_critico` de 2026-11-18 | `toque_pedido_em` 14h30 |

**Cobre:** P12, INV-DiaCritico-6, IDEM-Motor-PedirToque

**Caso do modelo:** CT-30

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::Caso30_PedirToque_As14h30` (proposto)

### TC-F20-02

**O toque já pedido no dia não sai de novo, nem depois de reiniciar a API**

**Feature:** F20

**Tipo:** Negativo

**Objetivo:** Provar que `ToquePedidoEm` preenchido impede um segundo `PEDIR_TOQUE` no mesmo dia.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Toque pedido às 14h30 para a casa 13 (TC-F20-01)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Vigia rodando de 14h30 a 14h31
- Relógio em 14h45 e em 16h00
- API reiniciada às 16h00

**Saídas esperadas:**

- Nenhum segundo `PEDIR_TOQUE` para nenhuma casa
- `toque_pedido_em` continua 14h30

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Esperar 1 minuto depois das 14h30 | Nenhum `PEDIR_TOQUE` novo |
| 2 | Mover o relógio para 14h45 e depois para 16h00, esperando 5 s em cada | Nenhum `PEDIR_TOQUE` novo |
| 3 | Reiniciar o contêiner da API e esperar 10 s | Nenhum `PEDIR_TOQUE` novo |
| 4 | Ler `motor.dia_critico` de 2026-11-18 | `toque_pedido_em` 14h30 |

**Cobre:** P12, INV-DiaCritico-6, IDEM-Motor-PedirToque

**Caso do modelo:** CT-30

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::Caso30_PedirToque_UmaVezPorDia` (proposto)

### TC-F20-03

**Casa com sinal depois das 10h, Acamado ou Coabitado não recebe toque**

**Feature:** F20

**Tipo:** Negativo

**Objetivo:** Provar que o toque vai só para a casa `Sozinho` sem sinal de vida desde as 10h.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 14 (`Sozinho`) com `MOVIMENTO` às 11h00
- Casa 9 (`Sozinho`) com `BOTAO_BEM` às 14h20
- Casa 11 (`Acamado`) e casa 22 (`Coabitado`) sem confirmação
- Casa 13 (`Sozinho`) sem sinal de vida, como controle
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 14h30 mais um ciclo do vigia

**Saídas esperadas:**

- `PEDIR_TOQUE` só no tópico da casa 13
- Nenhum comando nos tópicos das casas 14, 9, 11 e 22

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o `MOVIMENTO` da casa 14 às 11h00 e o `BOTAO_BEM` da casa 9 às 14h20 | Pacotes aceitos |
| 2 | Mover o relógio para 14h30 e esperar 5 s | Um `PEDIR_TOQUE` para a casa 13 |
| 3 | Filtrar as mensagens pelos tokens das casas 14, 9, 11 e 22 | Nenhum `PEDIR_TOQUE` |

**Cobre:** P12

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::PedirToque_SoSozinhoSemSinalDesde10h` (proposto)

### TC-F20-04

**Sem dia crítico declarado nenhum toque sai às 14h30**

**Feature:** F20

**Tipo:** Negativo

**Objetivo:** Provar que o pedido de toque pertence ao dia crítico: num dia comum, nada sai.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 13 (`Sozinho`) sem sinal de vida desde 00h00
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Cliente MQTT assinando `corredor/v1/+/comando` desde antes do primeiro passo
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Relógio em 14h30 mais 1 minuto de ciclos do vigia

**Saídas esperadas:**

- Nenhum `PEDIR_TOQUE` em nenhum tópico

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 14h30 e esperar 1 minuto | Nenhum `PEDIR_TOQUE` no cliente MQTT |
| 2 | Chamar o radar | `turnoAberto` falso; casa 13 em `aguardando` |

**Cobre:** P12

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/VigiaDoRadarTestes.cs::PedirToque_DiaComum_NaoPede` (proposto)

## F21 Relógio de demo

### TC-F21-01

**Mover o relógio para 09h58 e depois 10h00 grava a janela das 10h sem esperar**

**Feature:** F21

**Tipo:** Positivo

**Objetivo:** Provar que `PUT /api/demo/relogio` guarda o deslocamento e que o radar passa a calcular com a hora nova.

**Preparação:**

- API no ambiente `Development`
- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`) sem sinal de vida
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados

**Entradas:**

- `PUT /api/demo/relogio` com `{"agora": "2026-11-18T09:58:00-03:00"}`
- Depois, `agora` 10h00

**Saídas esperadas:**

- 200 com `agora` 09h58 e `deslocamentoEmSegundos` igual à diferença para a hora real
- Radar com `agora` 09h58
- Às 10h00, casa 13 em `pendentes`, `Vermelho`, desde 10h00

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `PUT /api/demo/relogio` com 09h58 | 200 com `agora` 09h58 e `deslocamentoEmSegundos` |
| 2 | Chamar `GET /api/radar?microarea=3` | `agora` 09h58, mais os segundos passados desde o `PUT`; casa 13 em `aguardando` |
| 3 | Enviar `PUT /api/demo/relogio` com 10h00 e chamar o radar | `agora` 10h00; casa 13 em `pendentes`, `Vermelho`, desde 10h00 |

**Cobre:** `PUT /demo/relogio`, NC-10

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/RelogioDeDemoTestes.cs::Mover_GuardaDeslocamentoERadarUsaHoraNova` (proposto)

### TC-F21-02

**Pacote e visita com hora real caem na hora deslocada e são aceitos**

**Feature:** F21

**Tipo:** Positivo

**Objetivo:** Provar que o mesmo deslocamento vale para o `ts` do nó e para a `ocorridaEm` da visita.

**Preparação:**

- API no ambiente `Development`
- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casas 13 e 14 (`Sozinho`)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo em 10h30, horas antes da hora real

**Entradas:**

- `MOVIMENTO` da casa 13 com `ts` igual à hora real em UTC
- Visita `EstavaBem` na casa 14 com `ocorridaEm` igual à hora real

**Saídas esperadas:**

- Evento aceito com `ocorridoEm` perto de 10h30
- Confirmação da casa 14 com `quando` perto de 10h30
- Casas 13 e 14 em `verdes` às 10h31

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar o `MOVIMENTO` em `corredor/v1/{token da casa 13}/evento` com `ts` real | Linha do tempo da casa 13 com o evento em 10h30, não na hora real |
| 2 | Enviar `POST /api/visitas` para a casa 14 com `ocorridaEm` real | 201; linha do tempo da casa 14 com confirmação em 10h30 |
| 3 | Mover o relógio para 10h31 e chamar `GET /api/radar?microarea=3` | Casas 13 e 14 em `verdes` |

**Cobre:** `PUT /demo/relogio`, NC-10

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/TradutorDePacoteTestes.cs::RelogioDeslocado_SomaDeslocamentoAoTs` (proposto)

### TC-F21-03

**Com o relógio deslocado, pacote 3 minutos adiantado continua recusado**

**Feature:** F21

**Tipo:** Negativo

**Objetivo:** Provar que o deslocamento não desliga a tolerância de 2 minutos do `ts`.

**Preparação:**

- API no ambiente `Development`
- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Casa 13 (`Sozinho`) com o heartbeat parado durante o passo
- Relógio de demo em 10h30

**Entradas:**

- `MOVIMENTO` da casa 13 com `ts` 3 minutos à frente da hora real

**Saídas esperadas:**

- Pacote recusado
- Nenhum evento novo na linha do tempo
- `ultimoPacoteEm` não muda

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Anotar `ultimoPacoteEm` da casa 13 na linha do tempo | Hora do último heartbeat |
| 2 | Publicar o `MOVIMENTO` com `ts` real mais 3 minutos | Nenhuma resposta ao nó; o Motor descarta |
| 3 | Abrir `GET /api/casas/{token}/linha-do-tempo` da casa 13 | Nenhum evento novo; `ultimoPacoteEm` igual ao anotado |

**Cobre:** `PUT /demo/relogio`, NC-10

**Caso do modelo:** CT-28

**Teste automatizado:** `api/tests/Corredor.Motor.Testes/TradutorDePacoteTestes.cs::RelogioDeslocado_TsAdiantado_Recusa` (proposto)

### TC-F21-04

**Em Production a rota do relógio não existe e a hora não muda**

**Feature:** F21

**Tipo:** Negativo

**Objetivo:** Provar que o relógio de demo só existe em `Development` e que em produção não há deslocamento.

**Preparação:**

- API iniciada com `ASPNETCORE_ENVIRONMENT=Production`
- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Casa 13 ativa

**Entradas:**

- `PUT /api/demo/relogio` com `agora` 09h58
- Visita na casa 13 com `ocorridaEm` igual à hora real

**Saídas esperadas:**

- 404, como qualquer rota que não existe
- Radar com `agora` na hora real
- Confirmação da visita na hora real

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar `PUT /api/demo/relogio` com 09h58 | 404 |
| 2 | Chamar `GET /api/radar?microarea=3` | `agora` igual à hora real |
| 3 | Registrar a visita e abrir a linha do tempo da casa 13 | 201; confirmação com `quando` na hora real |

**Cobre:** `PUT /demo/relogio`, NC-10

### TC-F21-05

**Relógio movido para trás com o reinício da demo volta a detectar silêncio**

**Feature:** F21

**Tipo:** Positivo

**Objetivo:** Provar o procedimento da seção 8.4 do modelo de dados: depois do reinício, nenhuma hora gravada fica no futuro.

**Preparação:**

- API no ambiente `Development`
- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Demo já rodada até 15h00, com eventos, visitas e avisos
- Nós simulados das 40 casas publicando só `HEARTBEAT` a cada 20 s
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)
- Acesso ao Postgres como `postgres` para `consultas/reiniciar_demo.sql`

**Entradas:**

- Simulador parado
- `PUT /api/demo/relogio` com 09h58
- `reiniciar_demo.sql` com a hora nova em UTC

**Saídas esperadas:**

- `evento_sensor`, `confirmacao`, `dia_critico`, `aviso_ao_vizinho` e `evento_de_seguranca` vazios
- `ultimo_pacote_em` nulo e preenchido de novo em até 20 s
- Visitas, idosos e tokens continuam
- Casa com nó parado fica `Cinza` 1 minuto depois

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Parar o simulador e enviar `PUT /api/demo/relogio` com 09h58 | 200 com `agora` 09h58 |
| 2 | Rodar `reiniciar_demo.sql` com a hora nova | COMMIT sem erro |
| 3 | Contar as cinco tabelas apagadas e `cadastro.visita` | Cinco tabelas vazias; `cadastro.visita` com as linhas de antes |
| 4 | Ligar os nós e esperar 20 s | `ultimo_pacote_em` das casas ativas preenchido perto de 09h58 |
| 5 | Parar o nó da casa 13 e chamar o radar 1 minuto depois | Casa 13 `Cinza` desde o último pacote; `turnoAberto` falso até o job declarar de novo |

**Cobre:** `PUT /demo/relogio`, NC-10

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes_expurgo.sql::reinicio da demo: sem eventos, confirmacoes, dia critico, avisos e eventos de seguranca, ultimo pacote vazio` (existe)

## F22 Retenção de 24 horas

### TC-F22-01

**Expurgo apaga movimento e confirmação com mais de 24 horas e mantém a visita**

**Feature:** F22

**Tipo:** Positivo

**Objetivo:** Provar que o expurgo apaga por data no Motor e não toca a visita do Cadastro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 13 (`Sozinho`, não eletrodependente)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `MOVIMENTO` às 02h00 de 18/11
- Visita `EstavaBem` às 02h10 de 18/11
- `MOVIMENTO` às 02h30 de 19/11
- Relógio em 03h00 de 19/11 e uma rodada do expurgo

**Saídas esperadas:**

- Movimento de 18/11 e confirmação de 02h10 fora do banco
- Movimento de 19/11 no banco
- Visita de 02h10 em `cadastro.visita`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Com o relógio em 02h00 de 18/11, publicar o `MOVIMENTO`; em 02h10, registrar a visita | Pacote aceito; visita 201 |
| 2 | Mover o relógio para 02h30 de 19/11 e publicar outro `MOVIMENTO` | Pacote aceito |
| 3 | Mover o relógio para 03h00 de 19/11. Esperar a rodada seguinte do `Expurgo` (até 1 h, `IntervaloDoExpurgo` também na demo) | Rodada concluída |
| 4 | Ler `motor.evento_sensor` e `motor.confirmacao` da casa 13 | Só o movimento de 02h30 de 19/11; nenhuma confirmação |
| 5 | Ler `cadastro.visita` pelo `id` da visita | A visita de 02h10 de 18/11 continua, `EstavaBem` |

**Cobre:** P7, IDEM-Motor-Expurgo, INV-Visita-6

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes_expurgo.sql::leitura normal e movimento com mais de 24 h somem` (existe)

### TC-F22-02

**Expurgo não apaga a leitura baixa sem confirmação da ACS e a casa segue vermelha**

**Feature:** F22

**Tipo:** Negativo

**Objetivo:** Provar o caso 31: a trava da energia sobrevive às 24 horas, e a resposta do vizinho não a solta.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 (`Sozinho`, eletrodependente, com vizinho)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `TELEMETRIA` 0 V às 02h40 de 18/11 e 127 V às 02h45
- Vizinho responde `EstaBem` ao aviso às 02h55
- Nenhuma visita da ACS
- Relógio em 03h41 de 19/11 e uma rodada do expurgo

**Saídas esperadas:**

- Leitura de 0 V das 02h40 de 18/11 continua em `motor.evento_sensor`
- Leitura de 127 V e confirmação do vizinho apagadas
- Casa 1 em `pendentes`, `Vermelho`, `EnergiaEletrodependente`, desde 02h40 de 18/11

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as duas telemetrias da casa 1 às 02h40 e às 02h45 de 18/11 | Pacotes aceitos; radar com a casa 1 `Vermelho` |
| 2 | Responder o aviso da casa 1 com `EstaBem` às 02h55 | 200 |
| 3 | Mover o relógio para 03h41 de 19/11. Esperar a rodada seguinte do `Expurgo` (até 1 h, `IntervaloDoExpurgo` também na demo) | Rodada concluída |
| 4 | Ler `motor.evento_sensor` e `motor.confirmacao` da casa 1 | A leitura de 0 V de 02h40 de 18/11 fica; a de 127 V e a confirmação `Vizinho` somem |
| 5 | Chamar `GET /api/radar?microarea=3` | Casa 1 em `pendentes`, `Vermelho`, `EnergiaEletrodependente`, `desde` 2026-11-18T02:40-03:00 |

**Cobre:** P7

**Caso do modelo:** CT-31

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes_expurgo.sql::caso 31: leitura baixa de 25 h sem confirmacao da ACS fica` (existe)

### TC-F22-03

**Leitura baixa liberada pela ACS e leitura baixa de casa comum saem no expurgo**

**Feature:** F22

**Tipo:** Positivo

**Objetivo:** Provar que a exceção vale só para casa eletrodependente sem visita da ACS depois da leitura.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 1 (eletrodependente) e casa 13 (não eletrodependente)
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- `TELEMETRIA` 0 V às 02h40 de 18/11 nas casas 1 e 13, e 127 V às 02h45
- Visita `EstavaBem` da ACS na casa 1 às 07h30 de 18/11
- Relógio em 07h31 de 19/11 e uma rodada do expurgo

**Saídas esperadas:**

- Leituras de 0 V das duas casas apagadas
- Confirmação de 07h30 apagada na mesma rodada
- Casa 1 sem alerta de energia, em `aguardando`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Publicar as telemetrias das casas 1 e 13 às 02h40 e às 02h45 de 18/11 | Pacotes aceitos |
| 2 | Mover o relógio para 07h30 de 18/11 e registrar a visita na casa 1 | 201; a trava solta |
| 3 | Mover o relógio para 07h31 de 19/11. Esperar a rodada seguinte do `Expurgo` (até 1 h, `IntervaloDoExpurgo` também na demo) | Rodada concluída |
| 4 | Ler `motor.evento_sensor` e `motor.confirmacao` das casas 1 e 13 | Nenhuma leitura de 18/11; nenhuma confirmação de 18/11 |
| 5 | Chamar o radar | Casa 1 em `aguardando`, sem `EnergiaEletrodependente` |

**Cobre:** P7, IDEM-Motor-Expurgo

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes_expurgo.sql::leitura baixa com confirmacao da ACS depois dela some, mesmo com a confirmacao vencendo na mesma rodada` (existe)

### TC-F22-04

**Rodar o expurgo duas vezes não apaga mais nada**

**Feature:** F22

**Tipo:** Negativo

**Objetivo:** Provar que o expurgo decide pelo estado que encontra: a segunda rodada não muda o banco.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Estado do TC-F22-02 depois da primeira rodada
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Segunda rodada do expurgo, com o relógio parado em 03h41 de 19/11

**Saídas esperadas:**

- Contagens de `motor.evento_sensor` com `ocorrido_em` antes de 03h41 de 18/11 e de `motor.confirmacao` iguais antes e depois
- Leitura de 0 V da casa 1 continua
- Casa 1 continua `Vermelho`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Contar `motor.evento_sensor` com `ocorrido_em` antes de 2026-11-18T03:41-03:00 e `motor.confirmacao` | Anotar os dois números |
| 2 | Esperar a rodada seguinte do `Expurgo` (até 1 h, `IntervaloDoExpurgo` também na demo) | Rodada concluída |
| 3 | Contar de novo | Os mesmos números; a leitura de 0 V da casa 1 está lá |
| 4 | Chamar o radar | Casa 1 em `pendentes`, `Vermelho`, `EnergiaEletrodependente` |

**Cobre:** IDEM-Motor-Expurgo, P7

**Caso do modelo:** CT-31

**Teste automatizado:** `docs/arquitetura/referencia/dados/testes_expurgo.sql::rodar duas vezes deixa 3 eventos e 1 confirmacao` (existe)

### TC-F22-05

**Expurgo e revogação não apagam visita de três dias**

**Feature:** F22

**Tipo:** Negativo

**Objetivo:** Provar que a visita sobrevive ao expurgo de 24 horas e à revogação do consentimento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Nenhum dia crítico declarado hoje (dia comum)
- Casa 40 (`Sozinho`) usada só neste caso
- Nós simulados das casas do caso publicando só `HEARTBEAT` a cada 20 s, para os aparelhos estarem ligados
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Visitas pelo app ou pelo Swagger autenticado como `acs-demo-01`, com `ocorridaEm` igual à hora real do envio; a API soma o deslocamento e a visita cai na hora de demo do passo
- Acesso de leitura ao Postgres como observador (`SELECT` nos schemas `motor` e `cadastro`)

**Entradas:**

- Visita `EstavaBem` na casa 40 com `ocorridaEm` 3 dias antes da hora real
- Uma rodada do expurgo
- Revogação por `DELETE /api/idosos/{id}/consentimento`

**Saídas esperadas:**

- Visita 201
- Visita em `cadastro.visita` depois do expurgo e depois da revogação

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a visita com `ocorridaEm` 3 dias antes | 201 |
| 2 | Esperar a rodada seguinte do `Expurgo` (até 1 h, `IntervaloDoExpurgo` também na demo) | Rodada concluída |
| 3 | Ler `cadastro.visita` pelo `id` | A visita continua |
| 4 | Revogar o consentimento da casa 40 | 204 |
| 5 | Ler `cadastro.visita` pelo `id` | A visita continua, com o mesmo desfecho e a mesma `ocorrida_em` |

**Cobre:** INV-Visita-6

**Caso do modelo:** CT-33

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/RevogarConsentimentoTestes.cs::Caso33_Revogar_MantemVisitas` (proposto)

## F23 Mascaramento de PII na entrada

### TC-F23-01

**CPF digitado em condições é gravado e logado só como máscara**

**Feature:** F23

**Tipo:** Positivo

**Objetivo:** Provar que o interceptador troca o CPF pela máscara antes do banco e do log, e grava um mascaramento no canal `Banco`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Terminal na VM com `journalctl -u corredor-api -f` aberto
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/idosos` com `nome` `Benedita Aroeira`, `endereco` `Rua do Bem-te-vi, 30 - Vila Exemplo`, perfil `Sozinho` e `condicoes` `CPF 123.456.789-00, usa bengala`

**Saídas esperadas:**

- 201
- `cadastro.idoso` guarda `***.456.***-00` em condições, com nome e endereço inteiros
- Nenhuma linha do journald tem `123.456.789-00`
- Um `MascaramentoNaEntrada` com tipos `Cpf`, canal `Banco`, severidade 4 e rota `/api/idosos`
- Farol `Amarelo`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` com o CPF em `condicoes` | API responde 201 com o `id` do idoso |
| 2 | Ler a linha do idoso em `cadastro.idoso` | Condições têm `***.456.***-00`; nome `Benedita Aroeira` e endereço inteiros |
| 3 | Procurar `123.456.789-00` no journald da `corredor-api` | Nenhuma linha encontrada; se a requisição foi logada, aparece a máscara |
| 4 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `MascaramentoNaEntrada`, tipos `{Cpf}`, canal `Banco`, severidade 4, rota `/api/idosos` |
| 5 | Chamar `GET /api/painel` | `cor` `Amarelo` e `pontos24h` 4 (severidade 4 vezes exposição 1) |

**Cobre:** P15, CANAL-Banco, INV-EventoDeSeguranca-2

**Caso do modelo:** CT-38

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::Caso38_CpfEmCondicoes_MascaradoNoBancoENoLog` (proposto)

### TC-F23-02

**Evento de mascaramento grava tipo de dado, canal, módulo e o modelo da rota**

**Feature:** F23

**Tipo:** Positivo

**Objetivo:** Provar que o evento de segurança guarda o que o painel precisa: tipo, tipos de dado, canal, módulo, rota modelo e severidade, mesmo vindo de uma requisição autenticada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Token da casa 11 lido em `api/semente/tokens.local.json`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/visitas` com `id` novo, o token da casa 11, `desfecho` `EstavaBem`, `ocorridaEm` agora e `observacao` `vizinha passou o CPF 123.456.789-00 por engano`

**Saídas esperadas:**

- 201
- Um evento com tipo `MascaramentoNaEntrada`, tipos `{Cpf}`, canal `Banco`, módulo preenchido, rota `/api/visitas` e severidade 4

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/visitas` | API responde 201 |
| 2 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Uma linha com `ocorrido_em` da requisição, tipo `MascaramentoNaEntrada`, tipos `{Cpf}`, canal `Banco` |
| 3 | Ler as colunas `modulo`, `rota` e `severidade` da mesma linha | Módulo não vazio, rota `/api/visitas`, severidade 4 |
| 4 | Ler a observação gravada em `cadastro.visita` | A observação tem `***.456.***-00` no lugar do CPF |

**Cobre:** P15, INV-EventoDeSeguranca-1, INV-EventoDeSeguranca-4

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::Mascaramento_RegistraEventoComTipoCanalModuloERota` (proposto)

### TC-F23-03

**Evento de mascaramento não guarda o valor, trecho do texto, ACS nem token**

**Feature:** F23

**Tipo:** Negativo

**Objetivo:** Provar que nenhuma coluna do evento carrega o CPF, a máscara, um pedaço da observação, a ACS ou um identificador da casa.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Evento do TC-F23-02 gravado
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- A linha mais nova de `guarda.evento_de_seguranca`
- Termos procurados: `123.456.789-00`, `***.456.***-00`, `vizinha passou`, `acs-demo-01`, o token da casa 11

**Saídas esperadas:**

- Nenhum termo aparece em nenhuma coluna do evento
- A tabela não tem coluna de valor, de ACS, de IP nem de agente

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Listar as colunas de `guarda.evento_de_seguranca` com `\d guarda.evento_de_seguranca` | Só `id`, `ocorrido_em`, `tipo`, `tipos_de_dado`, `canal`, `modulo`, `rota`, `severidade`, `mensagem_texto` e `mensagem_origem` |
| 2 | Converter a linha mais nova em texto (`SELECT e::text`) e procurar cada termo | Nenhum dos termos aparece |
| 3 | Conferir a coluna `rota` | Traz o modelo `/api/visitas`, sem token, sem `Guid` e sem query string |

**Cobre:** INV-EventoDeSeguranca-1, INV-EventoDeSeguranca-4

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::Evento_SemValorSemTrechoSemAcs` (proposto)

### TC-F23-04

**Chave senha no corpo é mascarada e o evento registra credencial com severidade 5**

**Feature:** F23

**Tipo:** Positivo

**Objetivo:** Provar que a regra de credencial casa com o nome inteiro da chave e que a severidade do evento é a maior entre os tipos achados.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Terminal na VM com `journalctl -u corredor-api -f` aberto
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/idosos` com nome, endereço e perfil válidos, `condicoes` `CPF 123.456.789-00` e uma chave extra `senha` com o valor `chave-de-teste`

**Saídas esperadas:**

- 201
- Nenhuma linha do journald tem `chave-de-teste` nem `123.456.789-00`
- O evento da requisição lista `Cpf` e `Credencial` e tem severidade 5
- Farol `Amarelo`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` com a chave `senha` | API responde 201 |
| 2 | Procurar `chave-de-teste` e `123.456.789-00` no journald | Nenhuma linha encontrada |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `MascaramentoNaEntrada`, tipos com `Cpf` e `Credencial`, canal `Banco`, severidade 5 |
| 4 | Chamar `GET /api/painel` | `cor` `Amarelo`, porque o evento é de entrada |

**Cobre:** P15, CANAL-Banco, INV-EventoDeSeguranca-2

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::ChaveSenha_MascaradaComoCredencial_SeveridadeMaior` (proposto)

### TC-F23-05

**casaToken de 32 hexadecimais não é tratado como credencial e a visita segue**

**Feature:** F23

**Tipo:** Negativo

**Objetivo:** Provar que o interceptador nunca mascara `casaToken`, para que `POST /visitas` não pare.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Token da casa 11 lido em `api/semente/tokens.local.json`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/visitas` com `id` novo, o token da casa 11, `desfecho` `EstavaBem`, `ocorridaEm` agora e `observacao` `tudo certo`

**Saídas esperadas:**

- 201
- A visita gravada tem o token inteiro
- Nenhum evento novo em `guarda.evento_de_seguranca`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Contar as linhas de `guarda.evento_de_seguranca` | Contagem anotada |
| 2 | Enviar o `POST /api/visitas` | API responde 201; não volta `TOKEN_INATIVO` |
| 3 | Ler a visita em `cadastro.visita` | O token gravado é igual ao enviado, sem asteriscos |
| 4 | Contar de novo os eventos | A contagem não mudou; nenhum evento com tipo `Credencial` |

**Cobre:** P15, CANAL-Banco

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::CasaToken_NuncaMascarado` (proposto)

### TC-F23-06

**Nome, endereço, saúde e mora sozinho entram inteiros no banco e não geram evento**

**Feature:** F23

**Tipo:** Negativo

**Objetivo:** Provar que só CPF, RG, CNS e credencial mudam o corpo; os outros tipos viram rótulo e o canal `Banco` aceita todos, sem bloqueio.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/idosos` com `nome` `Benedita Aroeira`, `endereco` `Rua do Bem-te-vi, 30 - Vila Exemplo`, perfil `Sozinho`, `condicoes` `mora sozinha` e `medicamentos` `diurético`

**Saídas esperadas:**

- 201
- Nome, endereço, condições e medicamentos gravados como enviados
- Nenhum `MascaramentoNaEntrada` e nenhuma `BloqueioDeSaida` no canal `Banco`
- Farol `Verde`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` | API responde 201 |
| 2 | Ler a linha do idoso em `cadastro.idoso` | Os quatro campos têm o texto enviado, sem colchetes nem asteriscos |
| 3 | Consultar `guarda.evento_de_seguranca` | Nenhuma linha nova |
| 4 | Chamar `GET /api/painel` | `cor` `Verde` e lista `eventos` vazia |

**Cobre:** P15, CANAL-Banco

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::NomeEnderecoSaude_SoRotulo_SemMascaraSemEvento` (proposto)

### TC-F23-07

**Requisição sem CPF, RG, CNS nem credencial não grava evento com lista vazia**

**Feature:** F23

**Tipo:** Negativo

**Objetivo:** Provar que o evento só nasce com pelo menos um tipo de dado achado.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Token da casa 11 lido em `api/semente/tokens.local.json`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/visitas` com `id` novo, o token da casa 11, `desfecho` `EstavaBem` e `observacao` `pressão boa, almoçou bem`

**Saídas esperadas:**

- 201
- Nenhum evento novo
- Nenhuma linha de `guarda.evento_de_seguranca` tem `tipos_de_dado` vazio

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/visitas` | API responde 201 |
| 2 | Contar os eventos | Nenhum evento novo; a observação só ganhou o rótulo `Saude`, que não muda o corpo |
| 3 | Consultar `SELECT count(*) FROM guarda.evento_de_seguranca WHERE cardinality(tipos_de_dado) = 0` | Resultado 0 |

**Cobre:** P15, INV-EventoDeSeguranca-2

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::SemAchado_NaoRegistraEvento` (proposto)

### TC-F23-08

**Mesmo CPF enviado duas vezes grava dois eventos de mascaramento**

**Feature:** F23

**Tipo:** Positivo

**Objetivo:** Provar que `Registrar` não tem chave de idempotência: cada mascaramento é um fato novo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- Duas vezes o mesmo `POST /api/idosos` do TC-F23-01, com `123.456.789-00` em `condicoes`

**Saídas esperadas:**

- Dois 201 com `id` diferentes
- Dois `MascaramentoNaEntrada` com `id` e `ocorrido_em` diferentes
- `pontos24h` 8

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o primeiro `POST /api/idosos` | 201 |
| 2 | Enviar o mesmo corpo de novo | 201 com outro `id` de idoso |
| 3 | Consultar os eventos das últimas 24 horas | Duas linhas `MascaramentoNaEntrada`, `Cpf`, `Banco`, com `id` diferentes |
| 4 | Chamar `GET /api/painel` | `cor` `Amarelo`, `pontos24h` 8 e dois itens em `eventos` |

**Cobre:** IDEM-Guarda-Registrar, P15

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::MesmoCpfDuasVezes_DoisEventos` (proposto)

### TC-F23-09

**Visita repetida com CPF não duplica a visita mas grava outro evento sem mudar o primeiro**

**Feature:** F23

**Tipo:** Negativo

**Objetivo:** Provar que a idempotência da visita não esconde o mascaramento repetido e que o segundo evento não altera o primeiro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Token da casa 11 lido em `api/semente/tokens.local.json`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- Duas vezes o mesmo `POST /api/visitas`, com o mesmo `id` e `observacao` com `123.456.789-00`

**Saídas esperadas:**

- 201 e depois 200
- Uma linha em `cadastro.visita`
- Dois eventos `MascaramentoNaEntrada`
- O primeiro evento continua com o mesmo `ocorrido_em` e a mesma severidade

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/visitas` e anotar o `id` e o `ocorrido_em` do evento gravado | 201 e um evento |
| 2 | Repetir o mesmo corpo | 200, a repetição da visita |
| 3 | Contar as visitas com aquele `id` | Uma |
| 4 | Consultar os eventos | Dois eventos; o primeiro tem o `id`, o `ocorrido_em` e a severidade anotados |

**Cobre:** IDEM-Guarda-Registrar, P15

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/InterceptadorDeEntradaTestes.cs::VisitaRepetida_UmaVisitaDoisEventos` (proposto)

## F24 Bloqueio de PII nas saídas

### TC-F24-01

**Idoso.Nome enviado ao log vira [NOME] e grava um bloqueio no canal Log**

**Feature:** F24

**Tipo:** Negativo

**Objetivo:** Provar que o `ProvedorDeLogSeguro` troca o nome pela máscara e registra `SaidaBloqueada` no canal `Log`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Teste de integração que lê a casa 1 (`Efigênia Jatobá`) e passa `Idoso.Nome` como argumento do `ILogger`

**Entradas:**

- `dotnet test --filter Caso40_NomeNoLog_ViraMascaraEEvento`

**Saídas esperadas:**

- A linha de log tem `[NOME]` e não tem `Efigênia Jatobá`
- Um `BloqueioDeSaida` com tipos `Nome`, canal `Log`, severidade 3
- O painel lista o evento

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o teste | Teste passa |
| 2 | Ler a saída de log capturada pelo teste | A linha mostra `[NOME]` no lugar do nome |
| 3 | Procurar `Efigênia Jatobá` na saída de log | Nenhuma ocorrência |
| 4 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{Nome}`, canal `Log`, severidade 3 |
| 5 | Chamar `GET /api/painel` | O evento aparece em `eventos`; `pontos24h` 6 (3 vezes exposição 2) |

**Cobre:** CANAL-Log, NC-9

**Caso do modelo:** CT-40

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/ProvedorDeLogSeguroTestes.cs::Caso40_NomeNoLog_ViraMascaraEEvento` (proposto)

### TC-F24-02

**Texto sem rótulo passa inteiro pelo canal Log sem gravar evento**

**Feature:** F24

**Tipo:** Positivo

**Objetivo:** Provar que a saída segura libera no log o texto que não carrega dado pessoal.

**Preparação:**

- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Teste de integração que loga `Turno aberto para a microárea 3` como `TextoRotulado` sem rótulo

**Entradas:**

- `dotnet test --filter TextoSemRotulo_SaiInteiroSemEvento`

**Saídas esperadas:**

- A linha de log tem `Turno aberto para a microárea 3` inteira
- Nenhum evento novo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o teste | Teste passa |
| 2 | Ler a saída de log capturada | O texto aparece inteiro, sem colchetes |
| 3 | Contar os eventos | Nenhum evento novo no canal `Log` |

**Cobre:** CANAL-Log

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/ProvedorDeLogSeguroTestes.cs::TextoSemRotulo_SaiInteiroSemEvento` (proposto)

### TC-F24-03

**Rota sem autenticação que devolveria Idoso.Condicoes sai bloqueada**

**Feature:** F24

**Tipo:** Negativo

**Objetivo:** Provar que o `FiltroDeResposta` barra texto rotulado em rota sem autenticação e registra o bloqueio.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Rota de demonstração sem autenticação que devolve `Idoso.Condicoes` da casa 1, montada só no teste de integração (evidência 3 da auditoria)

**Entradas:**

- Requisição sem cabeçalho de autenticação para a rota de demonstração

**Saídas esperadas:**

- O corpo não traz `usa concentrador de oxigênio`
- Um `BloqueioDeSaida` com tipos `Saude`, canal `RespostaSemAutenticacao`, severidade 4
- Farol `Vermelho`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a rota de demonstração sem autenticação | A resposta sai bloqueada |
| 2 | Procurar `concentrador` no corpo e nos cabeçalhos da resposta | Nenhuma ocorrência |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{Saude}`, canal `RespostaSemAutenticacao`, severidade 4 |
| 4 | Chamar `GET /api/painel` | `cor` `Vermelho`, `pontos24h` 12 |

**Cobre:** CANAL-RespostaSemAutenticacao, NC-9

**Caso do modelo:** CT-41

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FiltroDeRespostaTestes.cs::Caso41_RotaSemAutenticacao_CondicoesBloqueadas` (proposto)

### TC-F24-04

**GET /api/painel sem autenticação responde inteiro porque não carrega rótulo**

**Feature:** F24

**Tipo:** Positivo

**Objetivo:** Provar que uma resposta sem autenticação e sem dado pessoal sai inteira pela saída segura.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Um evento já gravado (TC-F23-01)

**Entradas:**

- `curl` em `GET /api/painel` sem cabeçalho de autenticação

**Saídas esperadas:**

- 200 com `cor`, `pontos24h` e `eventos`
- Nenhum `BloqueioDeSaida` no canal `RespostaSemAutenticacao`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Contar os eventos | Contagem anotada |
| 2 | Chamar `GET /api/painel` sem autenticação | 200 com os três campos preenchidos |
| 3 | Contar os eventos de novo | A contagem não mudou |

**Cobre:** CANAL-RespostaSemAutenticacao

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FiltroDeRespostaTestes.cs::RespostaSemRotulo_SaiInteira` (proposto)

### TC-F24-05

**Exceção com nome do idoso chega à tela do app como texto neutro**

**Feature:** F24

**Tipo:** Negativo

**Objetivo:** Provar que o `TratadorDeErros` troca a mensagem de exceção que leva dado pessoal por texto neutro.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Rota de demonstração, só no teste de integração, que lança uma exceção com `Idoso.Nome` da casa 1 na mensagem

**Entradas:**

- Chamada à rota de demonstração

**Saídas esperadas:**

- O corpo de erro não tem `Efigênia Jatobá`
- A mensagem é um texto neutro, sem dado pessoal
- Um `BloqueioDeSaida` com tipos `Nome` e canal `MensagemDeErro`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar a rota de demonstração | A API responde com erro |
| 2 | Ler o corpo do erro | Texto neutro; nenhum nome, endereço ou condição |
| 3 | Procurar `Efigênia` no corpo e no journald | Nenhuma ocorrência |
| 4 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{Nome}`, canal `MensagemDeErro`, severidade 3 |

**Cobre:** CANAL-MensagemDeErro, NC-9

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TratadorDeErrosTestes.cs::ExcecaoComNome_ViraTextoNeutro` (proposto)

### TC-F24-06

**Erro de domínio sem dado pessoal sai com código e mensagem inteiros**

**Feature:** F24

**Tipo:** Positivo

**Objetivo:** Provar que a mensagem de erro sem rótulo passa sem troca e sem evento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/idosos` sem `nome`, com endereço e perfil válidos

**Saídas esperadas:**

- 422 com `{"codigo": "NOME_OBRIGATORIO", "mensagem": ...}`
- Nenhum evento novo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` sem nome | 422 com `codigo` `NOME_OBRIGATORIO` e uma `mensagem` legível |
| 2 | Contar os eventos | Nenhum `BloqueioDeSaida` no canal `MensagemDeErro` |

**Cobre:** CANAL-MensagemDeErro

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TratadorDeErrosTestes.cs::ErroDeDominioSemRotulo_SaiInteiro` (proposto)

### TC-F24-07

**Comando DIA_CRITICO sem rótulo é publicado no tópico de comando**

**Feature:** F24

**Tipo:** Positivo

**Objetivo:** Provar que o canal `Mqtt` libera o comando que não carrega dado pessoal.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- `mosquitto_sub -t 'corredor/v1/+/comando' -v` aberto no Mosquitto local
- Nenhum dia crítico declarado hoje

**Entradas:**

- `PUT /api/dias-criticos/{hoje}` com `probabilidade` 0.81, `motivo` `mínima de 24 °C pela segunda noite`, `origem` `MODELO`

**Saídas esperadas:**

- 200
- `{"tipo": "DIA_CRITICO"}` chega no tópico de comando das casas ativas
- Nenhum evento no canal `Mqtt`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Declarar o dia crítico de hoje | 200 com `abertoEm` preenchido |
| 2 | Ler a saída do `mosquitto_sub` | Mensagens `{"tipo": "DIA_CRITICO"}`, sem nome, endereço nem condição |
| 3 | Contar os eventos | Nenhum `BloqueioDeSaida` no canal `Mqtt` |

**Cobre:** CANAL-Mqtt

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/SaidaSeguraTestes.cs::Mqtt_ComandoSemRotulo_Liberado` (proposto)

### TC-F24-08

**Texto com endereço não é publicado no MQTT e grava bloqueio no canal Mqtt**

**Feature:** F24

**Tipo:** Negativo

**Objetivo:** Provar que o canal `Mqtt` não aceita rótulo, nem o endereço que o app autenticado aceita.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- `mosquitto_sub -t 'corredor/v1/+/comando' -v` aberto no Mosquitto local
- Teste de integração que tenta publicar no tópico de comando da casa 1 um texto com `Casa.Endereco`

**Entradas:**

- `dotnet test --filter Mqtt_TextoComEndereco_Bloqueado`

**Saídas esperadas:**

- Nada com `Rua do Sabiá` chega ao `mosquitto_sub`
- Um `BloqueioDeSaida` com tipos `Endereco`, canal `Mqtt`, severidade 3
- Farol `Amarelo`, não vermelho

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o teste | Teste passa; a `SaidaSegura` devolve `Bloqueada` |
| 2 | Ler a saída do `mosquitto_sub` | Nenhuma mensagem nova |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{Endereco}`, canal `Mqtt`, severidade 3 |
| 4 | Chamar `GET /api/painel` | `cor` `Amarelo`, porque a severidade é 3 |

**Cobre:** CANAL-Mqtt, NC-9

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/SaidaSeguraTestes.cs::Mqtt_TextoComEndereco_Bloqueado` (proposto)

### TC-F24-09

**Prompt com só tipo de dado, canal e módulo chega ao LLM**

**Feature:** F24

**Tipo:** Positivo

**Objetivo:** Provar que o canal `PromptDoLlm` libera o prompt montado sem dado pessoal.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Endereço do LLM, no arquivo de variáveis da `corredor-api`, apontado para um LLM de teste local que grava cada requisição recebida e responde em 1 s com duas frases de teste; chave `chave-de-teste`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `ele mora sozinho e toma diurético`

**Saídas esperadas:**

- O LLM de teste recebe uma requisição com `MoraSozinho`, `Saude`, `GrupoDoBairro` e `Cadastro`
- Nenhum `BloqueioDeSaida` no canal `PromptDoLlm`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Verificar a mensagem | 200 com `veredito` `Bloqueada` |
| 2 | Esperar 5 s e ler a requisição gravada pelo LLM de teste | Um prompt com os tipos de dado, o canal e o módulo |
| 3 | Consultar eventos com canal `PromptDoLlm` | Nenhum |

**Cobre:** CANAL-PromptDoLlm

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::PromptSemRotulo_ChegaAoLlm` (proposto)

### TC-F24-10

**Prompt que juntasse Idoso.Condicoes é bloqueado e não chega ao LLM**

**Feature:** F24

**Tipo:** Negativo

**Objetivo:** Provar que juntar um texto rotulado ao prompt une os rótulos e a saída segura barra o envio.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Endereço do LLM, no arquivo de variáveis da `corredor-api`, apontado para um LLM de teste local que grava cada requisição recebida e responde em 1 s com duas frases de teste; chave `chave-de-teste`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Teste de integração que junta ao prompt do `Tradutor` o `Idoso.Condicoes` da casa 1

**Entradas:**

- `dotnet test --filter PromptComRotulo_BloqueadoNaSaidaSegura`

**Saídas esperadas:**

- O LLM de teste não recebe nenhuma requisição
- Um `BloqueioDeSaida` com tipos `Saude`, canal `PromptDoLlm`, severidade 4

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o teste | Teste passa; a `SaidaSegura` devolve `Bloqueada` |
| 2 | Ler as requisições gravadas pelo LLM de teste | Nenhuma requisição nova; nada com `concentrador` |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{Saude}`, canal `PromptDoLlm`, severidade 4 |

**Cobre:** CANAL-PromptDoLlm, NC-9

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::PromptComRotulo_BloqueadoNaSaidaSegura` (proposto)

### TC-F24-11

**Evento de bloqueio continua igual depois de reiniciar a API**

**Feature:** F24

**Tipo:** Positivo

**Objetivo:** Provar que o evento fica gravado sem valor e que a aplicação não o altera ao subir de novo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- Bloqueio do TC-F24-03 gravado e com mensagem anexada

**Entradas:**

- `sudo systemctl restart corredor-api`

**Saídas esperadas:**

- A linha do evento tem os mesmos valores antes e depois
- O painel lista o mesmo evento

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Copiar a linha do evento (`SELECT e::text`) | Linha anotada, sem valor pessoal |
| 2 | Reiniciar a `corredor-api` e esperar o serviço ficar ativo | `systemctl is-active` responde `active` |
| 3 | Ler a linha de novo | Igual à anotada, inclusive `mensagem_texto` e `mensagem_origem` |
| 4 | Chamar `GET /api/painel` | O evento aparece com o mesmo `ocorridoEm` e a mesma mensagem |

**Cobre:** INV-EventoDeSeguranca-5, NC-9

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/EventoDeSegurancaTestes.cs::Evento_SobreviveAoReinicio_SemMudar` (proposto)

### TC-F24-12

**Papel da Guarda no Postgres não consegue apagar nem reescrever um evento**

**Feature:** F24

**Tipo:** Negativo

**Objetivo:** Provar que o gatilho de `guarda.evento_de_seguranca` recusa `DELETE` e `UPDATE` fora do anexo da mensagem.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Evento do TC-F24-03 gravado com mensagem anexada
- `psql` conectado com o papel da Guarda, o mesmo da API

**Entradas:**

- `DELETE` do evento
- `UPDATE` da `severidade` do evento para 1
- `UPDATE` da `mensagem_texto`

**Saídas esperadas:**

- Os três comandos falham com erro do gatilho
- A linha continua igual

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o `DELETE` do evento | Erro: a tabela não aceita `DELETE` |
| 2 | Rodar o `UPDATE` da severidade | Erro do gatilho; severidade continua 4 |
| 3 | Rodar o `UPDATE` da mensagem já anexada | Erro: só aceita anexar a mensagem uma vez |
| 4 | Chamar `GET /api/painel` | O evento continua listado, sem mudança |

**Cobre:** INV-EventoDeSeguranca-5

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/EventoDeSegurancaTestes.cs::Gatilho_RecusaDeleteEUpdate` (proposto)

## F25 Mensagem para o grupo do bairro

### TC-F25-01

**Mensagem mora sozinho e toma diurético é bloqueada no grupo do bairro**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar o caso 39: a mensagem com mora sozinho e remédio é bloqueada, com explicação e versão segura.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Endereço do LLM, no arquivo de variáveis da `corredor-api`, apontado para um LLM de teste local que grava cada requisição recebida e responde em 1 s com duas frases de teste; chave `chave-de-teste`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/mensagens/verificar` com `{"canal": "GrupoDoBairro", "texto": "ele mora sozinho e toma diurético"}`

**Saídas esperadas:**

- 200 com `veredito` `Bloqueada`
- `textoMascarado` `ele [MORA_SOZINHO] e toma [SAUDE]`
- `tiposDeDado` `MoraSozinho` e `Saude`
- `explicacao` com a frase fixa, em duas frases
- `versaoSegura` `Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.`
- Um `BloqueioDeSaida` no canal `GrupoDoBairro`, módulo `Cadastro`, severidade 4
- Farol `Vermelho`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200 com `id`, `veredito` `Bloqueada` e os tipos `MoraSozinho` e `Saude` |
| 2 | Ler `textoMascarado`, `explicacao` e `versaoSegura` | Texto mascarado com colchetes; explicação em duas frases; versão segura `Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.` |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{MoraSozinho,Saude}`, canal `GrupoDoBairro`, severidade 4 |
| 4 | Chamar `GET /api/painel` | `cor` `Vermelho`, `pontos24h` 12 |

**Cobre:** `POST /mensagens/verificar`, INV-MensagemSaida-2, INV-MensagemSaida-5, P16, NC-8, CANAL-GrupoDoBairro

**Caso do modelo:** CT-39

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::Caso39_MoraSozinhoEDiuretico_Bloqueada` (proposto)

### TC-F25-02

**Mora sozinha no feminino com remédio também é bloqueada**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar que a regra de mora sozinho pega a forma feminina, como pede a lista de negativos da arquitetura.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `a senhora da casa amarela mora sozinha e toma diurético`

**Saídas esperadas:**

- 200 com `veredito` `Bloqueada`
- `tiposDeDado` com `MoraSozinho` e `Saude`
- `textoMascarado` sem `mora sozinha` e sem `diurético`
- Um `BloqueioDeSaida` no canal `GrupoDoBairro`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200 com `veredito` `Bloqueada` |
| 2 | Ler `tiposDeDado` e `textoMascarado` | `MoraSozinho` e `Saude`; as duas expressões trocadas por colchetes |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, canal `GrupoDoBairro`, severidade 4 |

**Cobre:** `POST /mensagens/verificar`, P16, CANAL-GrupoDoBairro, NC-8

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::MoraSozinhaFeminino_Bloqueada` (proposto)

### TC-F25-03

**Texto original da mensagem bloqueada não fica no banco nem no log**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar que a tabela de mensagens guarda só o texto mascarado e que o texto bloqueado não vaza por outro lugar.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Verificação do TC-F25-01 feita
- Terminal na VM com `journalctl -u corredor-api -f` aberto
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- A linha da mensagem em `cadastro.mensagem_saida`
- Termos procurados: `mora sozinho` e `diurético`

**Saídas esperadas:**

- `texto_mascarado` é `ele [MORA_SOZINHO] e toma [SAUDE]`
- Nenhum termo em `cadastro.mensagem_saida`, em `guarda.evento_de_seguranca` nem no journald

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Ler a mensagem pelo `id` da resposta | Colunas `canal`, `acs_id`, `texto_mascarado`, `veredito`, `tipos_de_dado`, `verificada_em`; nenhuma coluna de texto original |
| 2 | Procurar os termos em todas as linhas de `cadastro.mensagem_saida` (`SELECT m::text`) | Nenhuma ocorrência |
| 3 | Procurar os termos em `guarda.evento_de_seguranca` | Nenhuma ocorrência |
| 4 | Procurar os termos no journald da `corredor-api` | Nenhuma ocorrência |

**Cobre:** INV-MensagemSaida-3, NC-8

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::Bloqueada_GravaSoTextoMascarado` (proposto)

### TC-F25-04

**Versão segura verificada sai Liberada com explicação e versão segura nulas**

**Feature:** F25

**Tipo:** Positivo

**Objetivo:** Provar que um texto sem rótulo é liberado no grupo do bairro e nasce com o veredito.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.`

**Saídas esperadas:**

- 200 com `veredito` `Liberada`
- `explicacao` e `versaoSegura` `null`
- `tiposDeDado` vazio
- Linha em `cadastro.mensagem_saida` com veredito `Liberada` e o texto igual ao enviado
- Nenhum evento novo; farol `Verde`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200 com `veredito` `Liberada`, `explicacao` `null` e `versaoSegura` `null` |
| 2 | Ler a linha pelo `id` | Veredito `Liberada`, tipos vazios, `texto_mascarado` igual ao texto enviado |
| 3 | Chamar `GET /api/painel` | `cor` `Verde`, sem eventos |

**Cobre:** `POST /mensagens/verificar`, INV-MensagemSaida-1, INV-MensagemSaida-2, INV-MensagemSaida-3, P16, CANAL-GrupoDoBairro, NC-8

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::VersaoSegura_Liberada` (proposto)

### TC-F25-05

**Tela de mensagem bloqueada mostra a explicação e a versão segura para copiar**

**Feature:** F25

**Tipo:** Positivo

**Objetivo:** Provar, pelo app, o Fluxo 6: a ACS vê o que vazaria e copia a versão segura; o sistema não envia nada ao grupo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- App aberto e autenticado como `acs-demo-01`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- Texto `ele mora sozinho e toma diurético` na tela de mensagem, com destino grupo do bairro

**Saídas esperadas:**

- Tela de mensagem bloqueada com a explicação em duas frases e a versão segura
- A versão segura copiada e verificada sai `Liberada`
- Duas linhas em `cadastro.mensagem_saida`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Escrever o texto e tocar em verificar | Tela de mensagem bloqueada, com o estado também em texto, não só por cor |
| 2 | Ler a tela | Explicação em duas frases e a versão segura `Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.` com botão de copiar |
| 3 | Copiar a versão segura e verificar de novo | Tela com o texto liberado para copiar |
| 4 | Consultar `cadastro.mensagem_saida` | Uma linha `Bloqueada` e uma `Liberada`, as duas com `acs_id` `acs-demo-01` |

**Cobre:** `POST /mensagens/verificar`, INV-MensagemSaida-5, CANAL-GrupoDoBairro

### TC-F25-06

**Versão segura de mensagem com nome e rua é o texto fixo, sem a rua**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar que a versão segura é sempre o texto fixo e nunca localiza a pessoa (hotspot H42).

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `a Efigênia da Rua do Sabiá, 12 mora sozinha`

**Saídas esperadas:**

- 200 com `veredito` `Bloqueada`
- `versaoSegura` exatamente `Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.`
- Nem `versaoSegura` nem `explicacao` têm `Efigênia`, `Sabiá` ou `12`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200 com `veredito` `Bloqueada` e `MoraSozinho` em `tiposDeDado` |
| 2 | Comparar `versaoSegura` com o texto fixo | Iguais, caractere por caractere |
| 3 | Procurar `Efigênia`, `Sabiá` e `12` em `versaoSegura` e em `explicacao` | Nenhuma ocorrência |

**Cobre:** `POST /mensagens/verificar`, INV-MensagemSaida-5, CANAL-GrupoDoBairro

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::VersaoSegura_TextoFixoSemRua` (proposto)

### TC-F25-07

**Com o LLM fora do ar a verificação responde na hora com veredito e frase fixa**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar que a mensagem nunca fica pendente: o veredito e a explicação não esperam o LLM.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- LLM de teste desligado

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `ele mora sozinho e toma diurético`

**Saídas esperadas:**

- 200 em menos de 5 s com `veredito` `Bloqueada`
- `explicacao` com a frase fixa do tipo de dado
- Linha em `cadastro.mensagem_saida` já com o veredito

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação e medir o tempo de resposta | 200 em menos de 5 s |
| 2 | Ler `veredito` e `explicacao` | `Bloqueada` e a frase fixa, a mesma do TC-F25-01 |
| 3 | Ler a linha pelo `id` | `veredito` `Bloqueada`; não existe linha sem veredito na tabela |

**Cobre:** `POST /mensagens/verificar`, INV-MensagemSaida-1, INV-MensagemSaida-5

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::LlmFora_VereditoNaHora` (proposto)

### TC-F25-08

**Mesmo texto verificado duas vezes gera duas mensagens, cada uma com o próprio veredito**

**Feature:** F25

**Tipo:** Positivo

**Objetivo:** Provar que `VerificarMensagem` não tem chave de idempotência: cada verificação é uma nova `MensagemSaida`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- Duas vezes `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `ele mora sozinho e toma diurético`

**Saídas esperadas:**

- Duas respostas 200 com `id` diferentes, as duas `Bloqueada`
- Duas linhas em `cadastro.mensagem_saida`
- Dois `BloqueioDeSaida`; `pontos24h` 24

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a primeira verificação | 200, `Bloqueada`, `id` anotado |
| 2 | Enviar a mesma verificação de novo | 200, `Bloqueada`, outro `id` |
| 3 | Consultar as linhas com esse texto mascarado | Duas linhas, com `verificada_em` diferentes |
| 4 | Chamar `GET /api/painel` | Dois eventos `BloqueioDeSaida` e `pontos24h` 24 |

**Cobre:** IDEM-Cadastro-CadastrarIdoso, INV-MensagemSaida-4, `POST /mensagens/verificar`

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::MesmoTextoDuasVezes_DuasMensagens` (proposto)

### TC-F25-09

**Verificar a versão segura não muda o veredito da mensagem bloqueada anterior**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar que o veredito não muda: a nova tentativa é outra mensagem e a tabela recusa alteração.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Verificação do TC-F25-01 feita, `id` e `verificada_em` anotados
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` conectado com o papel do Cadastro, o mesmo da API

**Entradas:**

- `POST /api/mensagens/verificar` com o texto `Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.`
- `UPDATE cadastro.mensagem_saida SET veredito = 'Liberada'` na mensagem anotada

**Saídas esperadas:**

- Nova mensagem `Liberada` com outro `id`
- A mensagem anotada continua `Bloqueada`, com o mesmo `verificada_em`
- O `UPDATE` falha com erro do gatilho

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Verificar a versão segura | 200, `Liberada`, `id` diferente do anotado |
| 2 | Ler a mensagem anotada | Continua `Bloqueada`, com os mesmos tipos e o mesmo `verificada_em` |
| 3 | Rodar o `UPDATE` na mensagem anotada | Erro: a tabela só aceita `INSERT` |
| 4 | Tentar `DELETE` na mesma mensagem | Erro do gatilho; a linha continua |

**Cobre:** INV-MensagemSaida-4, IDEM-Cadastro-CadastrarIdoso

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::NovaTentativa_NaoMudaVereditoAnterior` (proposto)

### TC-F25-10

**Telefone do vizinho na mensagem ao grupo é bloqueado mesmo sendo aceito no app**

**Feature:** F25

**Tipo:** Negativo

**Objetivo:** Provar que o grupo do bairro não aceita nenhum rótulo, nem os que o canal `AppAutenticado` aceita.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `quem puder ligue para (00) 90000-0001`

**Saídas esperadas:**

- 200 com `veredito` `Bloqueada` e `Telefone` em `tiposDeDado`
- `textoMascarado` sem o número
- Um `BloqueioDeSaida` no canal `GrupoDoBairro` com severidade 3

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200, `Bloqueada`, `tiposDeDado` com `Telefone` |
| 2 | Ler `textoMascarado` | O número aparece trocado por colchetes |
| 3 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | Tipo `BloqueioDeSaida`, tipos `{Telefone}`, canal `GrupoDoBairro`, severidade 3 |
| 4 | Chamar `GET /api/painel` | `cor` `Amarelo`, porque a severidade é 3 |

**Cobre:** CANAL-GrupoDoBairro, P16, INV-MensagemSaida-2, NC-8

**Teste automatizado:** `api/tests/Corredor.Cadastro.Testes/VerificarMensagemTestes.cs::TelefoneNoGrupo_Bloqueado` (proposto)

## F26 Tradução por LLM e painel Risco Cidadão

### TC-F26-01

**Painel sem eventos nas últimas 24 horas mostra farol Verde**

**Feature:** F26

**Tipo:** Positivo

**Objetivo:** Provar que sem nenhum evento de segurança o farol é verde.

**Preparação:**

- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Navegador sem sessão aberta

**Entradas:**

- Página `/painel`
- `curl` em `GET /api/painel` sem autenticação

**Saídas esperadas:**

- `cor` `Verde`, `pontos24h` 0, `eventos` vazio
- A página mostra o farol verde, também em texto

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar `GET /api/painel` sem autenticação | 200 com `cor` `Verde`, `pontos24h` 0 e `eventos` `[]` |
| 2 | Abrir `/painel` no navegador | Farol verde, com a cor escrita, e nenhuma linha de evento |

**Cobre:** `GET /painel`, FAROL-Verde

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::SemEvento_Verde` (proposto)

### TC-F26-02

**Só mascaramento na entrada deixa o farol Amarelo**

**Feature:** F26

**Tipo:** Positivo

**Objetivo:** Provar o caso 38 no painel: o CPF mascarado na entrada leva o farol a amarelo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3

**Entradas:**

- O `POST /api/idosos` do TC-F23-01, com `123.456.789-00` em `condicoes`

**Saídas esperadas:**

- `cor` `Amarelo`, `pontos24h` 4
- Um item `MascaramentoNaEntrada` com canal `Banco` em `eventos`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` | 201 |
| 2 | Chamar `GET /api/painel` | `cor` `Amarelo`, `pontos24h` 4, um evento `MascaramentoNaEntrada`, canal `Banco`, tipos `Cpf` |
| 3 | Abrir `/painel` | Farol amarelo e a mensagem ao cidadão do evento |

**Cobre:** `GET /painel`, FAROL-Amarelo

**Caso do modelo:** CT-38

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::Caso38_SoMascaramento_Amarelo` (proposto)

### TC-F26-03

**CPF válido mascarado e bloqueio de severidade 3 nunca deixam o farol Vermelho**

**Feature:** F26

**Tipo:** Negativo

**Objetivo:** Provar que o mascaramento na entrada, mesmo de severidade 5, e o bloqueio de severidade 3 não passam de amarelo.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/idosos` com `condicoes` `CPF 123.456.789-09`, que tem dígito verificador válido
- O bloqueio do nome no log do TC-F24-01

**Saídas esperadas:**

- Mascaramento com tipos `Cpf` e severidade 5
- Bloqueio no canal `Log` com severidade 3
- `cor` `Amarelo` depois de cada passo; nunca `Vermelho`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` com o CPF válido | 201; o banco guarda a máscara, não `123.456.789-09` |
| 2 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` | `MascaramentoNaEntrada`, `Cpf`, `Banco`, severidade 5 |
| 3 | Chamar `GET /api/painel` | `cor` `Amarelo`, `pontos24h` 5 |
| 4 | Rodar o teste do TC-F24-01 contra o mesmo banco e chamar o painel | `cor` `Amarelo`, `pontos24h` 11 |

**Cobre:** FAROL-Vermelho, FAROL-Verde

**Caso do modelo:** CT-38, CT-40

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::MascaramentoSeveridade5EBloqueio3_NuncaVermelho` (proposto)

### TC-F26-04

**Mensagem bloqueada no grupo do bairro deixa o farol Vermelho com a mensagem traduzida**

**Feature:** F26

**Tipo:** Positivo

**Objetivo:** Provar o caso 39 no painel: a saída bloqueada de severidade 4 dá vermelho e o LLM anexa a mensagem ao cidadão.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Endereço do LLM, no arquivo de variáveis da `corredor-api`, apontado para um LLM de teste local que grava cada requisição recebida e responde em 1 s com duas frases de teste; chave `chave-de-teste`
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `ele mora sozinho e toma diurético`

**Saídas esperadas:**

- `cor` `Vermelho`, `pontos24h` 12
- O evento traz a mensagem devolvida pelo LLM de teste
- `mensagem_origem` `Llm`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200, `Bloqueada` |
| 2 | Esperar 5 s e chamar `GET /api/painel` | `cor` `Vermelho`, `pontos24h` 12, um `BloqueioDeSaida`, canal `GrupoDoBairro`, severidade 4 |
| 3 | Ler `mensagemAoCidadao` do evento | As duas frases de teste devolvidas pelo LLM |
| 4 | Consultar `SELECT * FROM guarda.evento_de_seguranca ORDER BY ocorrido_em DESC LIMIT 1` e ler `mensagem_origem` | `Llm` |
| 5 | Abrir `/painel` | Farol vermelho, com a cor escrita, e a mensagem traduzida do evento |

**Cobre:** `GET /painel`, FAROL-Vermelho, P17, INV-EventoDeSeguranca-3

**Caso do modelo:** CT-39

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::Caso39_BloqueioSeveridade4_Vermelho` (proposto)

### TC-F26-05

**Mascaramento junto de uma saída bloqueada de severidade 4 não deixa o farol Amarelo**

**Feature:** F26

**Tipo:** Negativo

**Objetivo:** Provar que o amarelo vale só sem saída bloqueada de severidade 4 ou 5.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3

**Entradas:**

- O `POST /api/idosos` do TC-F23-01
- Depois, a verificação de `ele mora sozinho e toma diurético` no grupo do bairro

**Saídas esperadas:**

- `cor` `Amarelo` depois do primeiro passo
- `cor` `Vermelho` depois do segundo, `pontos24h` 16

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar o `POST /api/idosos` e chamar o painel | `cor` `Amarelo`, `pontos24h` 4 |
| 2 | Verificar a mensagem e chamar o painel | `cor` `Vermelho`, `pontos24h` 16; não fica `Amarelo` |
| 3 | Ler a ordem de `eventos` | O `BloqueioDeSaida` vem antes do `MascaramentoNaEntrada`, do mais novo para o mais velho |

**Cobre:** FAROL-Amarelo, FAROL-Vermelho

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/FarolTestes.cs::MascaramentoMaisBloqueio4_Vermelho` (proposto)

### TC-F26-06

**LLM fora do ar faz o painel mostrar a frase fixa do tipo de evento**

**Feature:** F26

**Tipo:** Negativo

**Objetivo:** Provar o caso 42: sem resposta do LLM em 5 segundos, entra a frase fixa.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- LLM de teste desligado

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `ele mora sozinho e toma diurético`

**Saídas esperadas:**

- Verificação 200 normal
- Depois de 5 s, o evento tem a frase fixa de `BloqueioDeSaida`
- `mensagem_origem` `FraseFixa`
- O farol continua `Vermelho`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200, `Bloqueada`, sem atraso |
| 2 | Esperar 6 s e consultar o evento | `mensagem_texto` preenchido e `mensagem_origem` `FraseFixa` |
| 3 | Chamar `GET /api/painel` | `cor` `Vermelho` e `mensagemAoCidadao` com a frase fixa do tipo `BloqueioDeSaida` |

**Cobre:** P17, INV-EventoDeSeguranca-3

**Caso do modelo:** CT-42

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::Caso42_LlmFora_FraseFixa` (proposto)

### TC-F26-07

**Resposta do LLM que chega depois de 5 segundos não troca a frase fixa**

**Feature:** F26

**Tipo:** Negativo

**Objetivo:** Provar que a mensagem ao cidadão é anexada uma vez só.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- `psql` na VM, só para leitura, nos schemas `cadastro` e `guarda`
- LLM de teste configurado para responder em 8 s

**Entradas:**

- `POST /api/mensagens/verificar` com canal `GrupoDoBairro` e texto `ele mora sozinho e toma diurético`

**Saídas esperadas:**

- `mensagem_origem` `FraseFixa` aos 6 s
- Aos 10 s, a mesma frase fixa e a mesma origem

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Enviar a verificação | 200, `Bloqueada` |
| 2 | Consultar o evento aos 6 s | Frase fixa, origem `FraseFixa` |
| 3 | Consultar o evento aos 10 s, depois da resposta tardia do LLM de teste | Mesmo `mensagem_texto` e origem `FraseFixa`; as frases do LLM não aparecem |
| 4 | Chamar `GET /api/painel` | `mensagemAoCidadao` com a frase fixa |

**Cobre:** INV-EventoDeSeguranca-3, P17

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::RespostaTardia_NaoTrocaFraseFixa` (proposto)

### TC-F26-08

**Prompt enviado ao LLM nunca leva o texto, o nome nem a máscara do dado**

**Feature:** F26

**Tipo:** Negativo

**Objetivo:** Provar que o prompt carrega só o tipo de dado, o canal e o módulo, e que só a saída bloqueada vai ao LLM.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Endereço do LLM, no arquivo de variáveis da `corredor-api`, apontado para um LLM de teste local que grava cada requisição recebida e responde em 1 s com duas frases de teste; chave `chave-de-teste`

**Entradas:**

- Verificação de `ele mora sozinho e toma diurético` no grupo do bairro
- O `POST /api/idosos` do TC-F23-01

**Saídas esperadas:**

- Uma requisição gravada pelo LLM de teste, a do bloqueio; o mascaramento não vai ao LLM
- O prompt não tem `mora sozinho`, `diurético`, `[MORA_SOZINHO]`, `Benedita`, `123.456.789-00`, `***.456.***-00` nem `acs-demo-01`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Fazer as duas chamadas e esperar 5 s | Uma requisição gravada pelo LLM de teste |
| 2 | Ler o prompt | Só os tipos `MoraSozinho` e `Saude`, o canal `GrupoDoBairro` e o módulo `Cadastro` |
| 3 | Procurar cada termo proibido no prompt | Nenhuma ocorrência |

**Cobre:** P17

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/TradutorTestes.cs::Prompt_SoTipoCanalModulo` (proposto)

### TC-F26-09

**GET /api/painel e a página /painel não mostram nenhum valor pessoal**

**Feature:** F26

**Tipo:** Negativo

**Objetivo:** Provar que o painel aberto não traz valor, trecho de texto, token, `id` de idoso nem ACS.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) e simulador das 39 casas parado
- Painel zerado: `consultas/reiniciar_demo.sql` rodado como `postgres` (seção 8.4 do modelo de dados), que esvazia `guarda.evento_de_seguranca`
- Swagger ou app autenticado como `acs-demo-01`, da microárea 3
- Token da casa 11 lido em `api/semente/tokens.local.json`
- Eventos dos TC-F23-01, TC-F24-01, TC-F25-01 e TC-F25-10 gravados nas últimas 24 horas

**Entradas:**

- `curl` em `GET /api/painel` sem autenticação
- Página `/painel`

**Saídas esperadas:**

- Cada evento tem só `ocorridoEm`, `tipo`, `canal`, `modulo`, `tiposDeDado`, `severidade` e `mensagemAoCidadao`
- Nenhum valor pessoal na resposta nem na página

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Chamar `GET /api/painel` e listar os campos de cada evento | Só os sete campos esperados |
| 2 | Procurar `123.456.789-00`, `***.456.***-00`, `Benedita`, `Efigênia`, `mora sozinho`, `diurético`, `90000-0001`, `acs-demo-01`, o token da casa 11 e o `id` do idoso cadastrado | Nenhuma ocorrência |
| 3 | Abrir `/painel` e repetir a busca no texto da página | Nenhuma ocorrência |

**Cobre:** `GET /painel`

**Teste automatizado:** `api/tests/Corredor.Guarda.Testes/PainelTestes.cs::Painel_SemValorPessoal` (proposto)

## F27 App da ACS sem rede: fila de desfechos e radar guardado

### TC-F27-01

**Desfecho com rede sobe com 201 e o card fica no lugar até o próximo radar**

**Feature:** F27

**Tipo:** Positivo

**Objetivo:** Provar que o desfecho enviado com sucesso vira `enviado` e que o app não muda a situação do card.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Celular com rede
- Casa 5 vermelha e pendente no radar do celular

**Entradas:**

- Desfecho `EstavaFora` na casa 5, com observação "vizinha disse que foi à feira"

**Saídas esperadas:**

- Linha do `desfecho` com `estado` `enviado`, `registrada_em` preenchida e `observacao` nula
- Card da casa 5 com o desfecho local, na mesma lista e na mesma posição até o próximo radar

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar o desfecho `EstavaFora` com a observação no card da casa 5 | O app grava a linha antes da chamada e a API responde 201 |
| 2 | Ler a linha em `desfecho` | `id` com 36 caracteres, `estado` `enviado`, `registrada_em` preenchida, `observacao` nula |
| 3 | Olhar o radar no celular | O card da casa 5 mostra o desfecho local e continua `Vermelho`, em `pendentes`, na mesma posição |
| 4 | Atualizar o radar | O card fica onde o novo radar do servidor o põe |

**Cobre:** APP-FILA-1, APP-CACHE-5

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::resposta201_marcaEnviadoEApagaObservacao` (proposto)

### TC-F27-02

**Sem rede, o desfecho fica pendente e o card não muda de situação nem de posição**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que, sem rede, o desfecho fica guardado no celular e que o app não muda nem reordena o card.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Radar baixado com a casa 6 vermelha e pendente, na terceira posição

**Entradas:**

- Celular em modo avião
- Desfecho `EstavaBem` na casa 6, com observação "pressão normal"

**Saídas esperadas:**

- Linha com `estado` `pendente`, `registrada_em` nula e `observacao` guardada
- `tentativas` soma 1 a cada tentativa sem rede e `ultimo_erro` guarda só um código
- Card da casa 6 continua `Vermelho`, em `pendentes`, na terceira posição

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Ligar o modo avião e registrar o desfecho `EstavaBem` na casa 6 | O app aceita o toque sem esperar a rede |
| 2 | Ler a linha em `desfecho` | `estado` `pendente`, `registrada_em` nula, `observacao` "pressão normal" |
| 3 | Fechar e abrir o app, ainda sem rede, e ler a linha de novo | `tentativas` subiu 1, `ultimo_erro` tem só um código, sem mensagem; `id` e `ocorrida_em` iguais |
| 4 | Olhar o radar no celular | O card mostra o desfecho local pendente e continua `Vermelho`, em `pendentes`, na terceira posição; a casa não vira verde |

**Cobre:** APP-FILA-1, APP-FILA-2, APP-CACHE-5, APP-CACHE-2

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::semRede_continuaPendenteESomaTentativa` (proposto)

### TC-F27-03

**API fora do ar com resposta 5xx: o desfecho continua pendente e repete o mesmo id**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que uma resposta 5xx não perde o desfecho e que a nova tentativa usa o mesmo `id`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Celular com rede
- API parada na VM com `sudo systemctl stop corredor-api`; o Nginx responde 5xx

**Entradas:**

- Desfecho `NaoAbriu` na casa 7

**Saídas esperadas:**

- Linha `pendente`, `tentativas` 1, `ultimo_erro` com o código da resposta
- Depois da volta da API, a mesma linha `enviado`, com o mesmo `id` e a mesma `ocorrida_em`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar o desfecho `NaoAbriu` na casa 7 | A chamada recebe 5xx |
| 2 | Ler a linha em `desfecho` | `estado` `pendente`, `tentativas` 1, `ultimo_erro` com o código, sem o texto da resposta |
| 3 | Religar a API com `sudo systemctl start corredor-api` e fechar e abrir o app | A API responde 201 ao reenvio |
| 4 | Ler a linha de novo | `estado` `enviado`, `registrada_em` preenchida; `id` e `ocorrida_em` iguais aos do primeiro toque |

**Cobre:** APP-FILA-2

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::resposta5xx_mesmoIdNaProximaTentativa` (proposto)

### TC-F27-04

**Sessão vencida com resposta 401: a fila para, os desfechos ficam pendentes e o app pede a entrada**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que o 401 não recusa nem perde desfechos e que a fila só volta depois de uma nova entrada.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Dois desfechos `EstavaBem` registrados sem rede, nas casas 5 e 6
- Sessão da ACS vencida no servidor; a API passa a responder 401 para ela

**Entradas:**

- Rede ligada de novo
- Nova entrada como `acs-demo-01`

**Saídas esperadas:**

- Os dois desfechos `pendente` enquanto a sessão está vencida
- Tela de entrada aberta
- Depois da entrada, os dois `enviado` com os mesmos `id`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Desligar o modo avião | O app tenta o desfecho mais antigo e recebe 401 |
| 2 | Ler as duas linhas em `desfecho` | As duas em `pendente`; nenhuma em `recusado` |
| 3 | Olhar o celular | O app mostra a tela de entrada |
| 4 | Pelo Swagger, ver a linha do tempo das casas 5 e 6 | Nenhuma confirmação nova nas duas casas |
| 5 | Entrar de novo como `acs-demo-01` | A fila volta e as duas linhas ficam `enviado`, com os mesmos `id` |

**Cobre:** APP-FILA-3

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::resposta401_paraAFilaSemMudarEstado` (proposto)

### TC-F27-05

**Rede volta e a fila sobe do desfecho mais antigo ao mais novo, um por vez, com os mesmos ids**

**Feature:** F27

**Tipo:** Positivo

**Objetivo:** Provar que a fila envia os pendentes em ordem, com a sessão válida, e que o servidor guarda o `id` do celular.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Sessão da ACS válida
- Três desfechos registrados sem rede: casa 5 às 10h40, casa 6 às 10h45, casa 7 às 10h50

**Entradas:**

- Rede ligada de novo
- `POST /api/visitas` repetido pelo Swagger com o `id` e o corpo do desfecho da casa 5

**Saídas esperadas:**

- Os três `enviado`, com `registrada_em` na mesma ordem de `ocorrida_em`
- Repetição no Swagger responde 200 com o mesmo `id`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Desligar o modo avião | O app envia uma linha por vez, sem pedir a entrada |
| 2 | Ler as três linhas em `desfecho` | As três `enviado`; `registrada_em` da casa 5 antes da 6, e a da 6 antes da 7; `ocorrida_em` igual à hora de cada toque |
| 3 | Repetir pelo Swagger o `POST /api/visitas` com o `id` da linha da casa 5 | API responde 200 com o mesmo `id`, sem criar outra visita |

**Cobre:** APP-FILA-2, APP-FILA-3

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::enviaDoMaisAntigoAoMaisNovo_UmPorVez` (proposto)

### TC-F27-06

**Envio com 422 TOKEN_INATIVO fica recusado, mostra Não enviado e o app não tenta de novo**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que uma recusa do servidor tira o desfecho da fila e que o app mostra o motivo em texto.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Desfecho `EstavaBem` da casa 11 registrado sem rede
- Consentimento da casa 11 revogado pelo Swagger (`DELETE /api/idosos/{id}/consentimento` responde 204)
- Radar do celular ainda não atualizado depois da revogação

**Entradas:**

- Rede ligada de novo
- Depois, um desfecho novo em outra casa

**Saídas esperadas:**

- Linha da casa 11 com `estado` `recusado`
- Card da casa 11 com "Não enviado" e o motivo em texto
- Nenhuma nova tentativa para a linha recusada

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Desligar o modo avião | O envio da casa 11 recebe 422 `TOKEN_INATIVO` |
| 2 | Ler a linha em `desfecho` | `estado` `recusado`, `registrada_em` nula |
| 3 | Olhar o card da casa 11 | Mostra "Não enviado" com o motivo em texto, não só por cor |
| 4 | Fechar e abrir o app e registrar um desfecho na casa 5 | Só o desfecho da casa 5 sobe; a linha da casa 11 continua `recusado`, com `tentativas` igual |

**Cobre:** APP-FILA-4

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::resposta422_marcaRecusadoENaoTentaDeNovo` (proposto)

### TC-F27-07

**Desfecho recusado não trava a fila e fica no celular até a ACS dispensar**

**Feature:** F27

**Tipo:** Positivo

**Objetivo:** Provar que a recusa fica visível até a ACS decidir e que os outros desfechos seguem normalmente.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Linha da casa 11 em `recusado` (TC-F27-06), radar ainda sem atualizar
- Celular com rede

**Entradas:**

- Desfechos `EstavaBem` nas casas 5 e 6
- A ACS dispensa o aviso "Não enviado" da casa 11

**Saídas esperadas:**

- Casas 5 e 6 `enviado`
- Linha da casa 11 apagada só depois de a ACS dispensar

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Registrar os desfechos das casas 5 e 6 | As duas linhas ficam `enviado`; a da casa 11 continua `recusado` |
| 2 | Fechar e abrir o app | A linha da casa 11 continua lá, com "Não enviado" |
| 3 | Dispensar o aviso "Não enviado" da casa 11 | A linha da casa 11 some de `desfecho` |

**Cobre:** APP-FILA-4, APP-FILA-1

**Teste automatizado:** `app/src/banco/__tests__/fila.test.ts::recusadoNaoTravaAFila` (proposto)

### TC-F27-08

**Radar recém-baixado guarda a ordem do servidor, junta os nomes e mostra a hora sem aviso de conexão**

**Feature:** F27

**Tipo:** Positivo

**Objetivo:** Provar que a resposta 200 troca o cache inteiro, na ordem do servidor, e que o radar novo não se declara velho.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Celular com rede
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo, em 10h31

**Entradas:**

- Atualização do radar e da lista de nomes no app às 10h31 do celular
- `GET /api/radar?microarea=3` pelo Swagger

**Saídas esperadas:**

- `radar_card` com a mesma ordem das listas da resposta
- Cards com nome e endereço
- Título com a hora do radar, sem "sem conexão"

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Atualizar o radar e a lista no app | Cada recurso ganha `baixado_em` novo em `sincronizacao` |
| 2 | Comparar `radar_card` com a resposta do Swagger | Para cada card, `lista` e `ordem` iguais à posição em `pendentes`, `atendidas` ou `aguardando` |
| 3 | Olhar o radar no celular | Primeiro as pendentes, depois as atendidas, depois as aguardando, cada grupo na ordem do servidor; cada card com nome e endereço |
| 4 | Olhar o título antes de 2 minutos | Mostra a hora do radar, sem "sem conexão" |

**Cobre:** APP-CACHE-2, APP-CACHE-3, APP-CACHE-4, APP-CACHE-6

**Teste automatizado:** `app/src/banco/__tests__/cache.test.ts::radarGravaAOrdemDoServidor` (proposto)

### TC-F27-09

**Casa que revogou o consentimento some do celular na mesma sincronização**

**Feature:** F27

**Tipo:** Positivo

**Objetivo:** Provar que a lista e o radar são trocados, não mesclados, e que nenhum dado da casa revogada sobra no celular.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Celular com rede
- Lista e radar baixados com a casa 12 presente

**Entradas:**

- `DELETE /api/idosos/{id}/consentimento` da casa 12 pelo Swagger
- Nova sincronização no app

**Saídas esperadas:**

- Nenhuma linha com o token da casa 12 em `lista_de_nomes` nem em `radar_card`
- Total de linhas em `lista_de_nomes` igual ao tamanho da nova resposta

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Revogar o consentimento da casa 12 pelo Swagger | API responde 204 |
| 2 | Atualizar a lista e o radar no app | As duas respostas são 200 |
| 3 | Procurar o token da casa 12 no banco do app | Nenhuma linha em `lista_de_nomes` nem em `radar_card` |
| 4 | Contar as linhas de `lista_de_nomes` e comparar com `GET /api/lista-de-nomes?microarea=3` | Os dois números são iguais |
| 5 | Olhar o radar no celular | Nenhum card nem nome da casa 12 |

**Cobre:** APP-CACHE-1, APP-CACHE-3

**Teste automatizado:** `app/src/banco/__tests__/cache.test.ts::trocarNaoMesclar_casaRevogadaSome` (proposto)

### TC-F27-10

**Resposta de erro ou falta de rede não toca a lista nem o radar guardados**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que só uma resposta 200 lida inteira apaga o cache, e que até lá o celular mostra o que já tinha.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Lista e radar baixados com a casa 13 presente
- Consentimento da casa 13 revogado pelo Swagger depois do download

**Entradas:**

- API parada na VM com `sudo systemctl stop corredor-api` (resposta 5xx)
- Depois, celular em modo avião

**Saídas esperadas:**

- `lista_de_nomes`, `radar_card` e `sincronizacao.baixado_em` iguais aos de antes
- A casa 13 continua no celular até uma sincronização com 200

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Guardar a contagem de linhas e o `baixado_em` de cada recurso | Valores anotados |
| 2 | Parar a API e atualizar a lista e o radar no app | As chamadas recebem 5xx |
| 3 | Ler o banco de novo | As mesmas linhas e o mesmo `baixado_em`; a casa 13 continua em `lista_de_nomes` |
| 4 | Ligar o modo avião e atualizar de novo | O banco continua igual |
| 5 | Religar a API, desligar o modo avião e atualizar | Respostas 200; agora a casa 13 some das duas tabelas |

**Cobre:** APP-CACHE-3, APP-CACHE-1

**Teste automatizado:** `app/src/banco/__tests__/cache.test.ts::respostaComErroNaoTocaOCache` (proposto)

### TC-F27-11

**Radar baixado há mais de 2 minutos, sem rede, mostra sem conexão e a hora do radar**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que o radar guardado avisa que é velho e que a contagem de verdes diz de que hora é.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Radar baixado às 10h31 do celular, com 27 verdes

**Entradas:**

- Celular em modo avião
- Espera de 3 minutos

**Saídas esperadas:**

- Título "Radar das 10h31, sem conexão"
- Contagem "27 verdes às 10h31"

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Ligar o modo avião logo depois do download | Antes de 2 minutos, o título mostra a hora sem "sem conexão" |
| 2 | Esperar 3 minutos e abrir o radar | Título "Radar das 10h31, sem conexão" |
| 3 | Olhar a contagem de verdes | "27 verdes às 10h31", nunca só "27 verdes" |
| 4 | Ler `sincronizacao` do radar | `baixado_em` continua com a hora das 10h31 |

**Cobre:** APP-CACHE-6

**Teste automatizado:** `app/src/banco/__tests__/cache.test.ts::radarComMaisDe2MinutosSeDeclaraVelho` (proposto)

### TC-F27-12

**Card cujo token não está na lista de nomes aparece com Casa sem nome. Atualize a lista.**

**Feature:** F27

**Tipo:** Negativo

**Objetivo:** Provar que um card vermelho nunca some da tela por falta do nome no celular.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- App da ACS aberto no celular de teste, autenticado como `acs-demo-01`, na microárea 3
- Cópia do `corredor.db` do app lida só como observador, depois de cada passo
- Swagger da API aberto e autenticado, para conferir o servidor
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Lista de nomes baixada antes de um idoso novo ser cadastrado e ativado pelo Swagger
- Radar baixado depois da ativação, com a casa nova `Cinza` em `pendentes`, sem lista nova

**Entradas:**

- Token da casa nova em `radar_card` e fora de `lista_de_nomes`

**Saídas esperadas:**

- Card da casa nova com o texto "Casa sem nome. Atualize a lista."
- Número de cards na tela igual ao número de linhas de `radar_card`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Procurar o token da casa nova no banco do app | Está em `radar_card` e não está em `lista_de_nomes` |
| 2 | Olhar o radar no celular | O card aparece na posição do servidor, com "Casa sem nome. Atualize a lista." e a situação em texto |
| 3 | Contar os cards na tela e as linhas de `radar_card` | Os dois números são iguais |
| 4 | Atualizar a lista de nomes | O card passa a mostrar o nome e o endereço |

**Cobre:** APP-CACHE-4

**Teste automatizado:** `app/src/banco/__tests__/cache.test.ts::cardSemNomeApareceComTexto` (proposto)

## F28 Nó do corredor no Wokwi

### TC-F28-01

**Presença no corredor acende a luz de caminho e publica MOVIMENTO**

**Feature:** F28

**Tipo:** Positivo

**Objetivo:** Provar que a passagem da idosa acende a luz no próprio nó e chega ao Motor como movimento.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/evento'`
- Swagger da API aberto e autenticado, para conferir o servidor

**Entradas:**

- Sensor de presença acionado no Wokwi

**Saídas esperadas:**

- Luz de caminho acesa no Wokwi
- Um `MOVIMENTO` com `bufferizado: false` no tópico da casa 1
- O movimento na linha do tempo da casa 1

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Acionar o sensor de presença no Wokwi | A luz de caminho acende |
| 2 | Olhar o terminal da assinatura | Chega em `corredor/v1/<token da casa 1>/evento` um pacote com `tipo` `MOVIMENTO`, `seq`, `ts` e `bufferizado: false` |
| 3 | Chamar `GET /api/casas/{token}/linha-do-tempo` da casa 1 | O `MOVIMENTO` aparece com `ocorridoEm` igual ao `ts` publicado |

**Cobre:** P5

### TC-F28-02

**Sem Wi-Fi a luz de caminho acende, e o movimento chega depois com a hora original**

**Feature:** F28

**Tipo:** Negativo

**Objetivo:** Provar que a luz não depende da rede e que o nó guarda o evento e reenvia com `bufferizado: true`.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/evento'`
- Swagger da API aberto e autenticado, para conferir o servidor
- Relógio de demo sem deslocamento

**Entradas:**

- Wi-Fi do Wokwi cortado
- Sensor de presença acionado às 10h20
- Wi-Fi religado às 10h25

**Saídas esperadas:**

- Luz acesa sem rede
- Nada publicado enquanto o Wi-Fi está cortado
- Depois da volta, o `MOVIMENTO` com `bufferizado: true` e `ts` 10h20, uma vez só na linha do tempo

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Cortar o Wi-Fi do Wokwi | A assinatura para de receber pacotes da casa 1 |
| 2 | Acionar o sensor de presença às 10h20 | A luz de caminho acende; nada chega na assinatura |
| 3 | Religar o Wi-Fi às 10h25 | O monitor serial mostra o reenvio; chega um `MOVIMENTO` com `bufferizado: true` e `ts` de 10h20 |
| 4 | Chamar a linha do tempo da casa 1 | O movimento aparece uma vez, com `ocorridoEm` 10h20, `recebidoEm` depois de 10h25 e `bufferizado: true` |

**Cobre:** P5

### TC-F28-03

**Sem presença a luz não acende e o nó não publica MOVIMENTO, com ou sem Wi-Fi**

**Feature:** F28

**Tipo:** Negativo

**Objetivo:** Provar que o nó não inventa movimento: só o heartbeat mostra que ele está ligado.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/evento'`
- Swagger da API aberto e autenticado, para conferir o servidor

**Entradas:**

- Sensor de presença parado por 2 minutos
- Depois, Wi-Fi cortado por 1 minuto, sem presença, e religado

**Saídas esperadas:**

- Luz de caminho apagada
- Só `HEARTBEAT` a cada 20 s na assinatura, nenhum `MOVIMENTO`
- Nenhum `MOVIMENTO` com `bufferizado: true` depois da volta do Wi-Fi

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Deixar o sensor de presença parado por 2 minutos | A luz continua apagada |
| 2 | Olhar o terminal da assinatura | Só pacotes `HEARTBEAT` da casa 1, cerca de um a cada 20 s; nenhum `MOVIMENTO` |
| 3 | Cortar o Wi-Fi por 1 minuto, sem presença, e religar | A luz continua apagada; nenhum `MOVIMENTO` chega depois da volta |
| 4 | Chamar a linha do tempo da casa 1 | Nenhum `MOVIMENTO` novo no período do teste |

**Cobre:** P5

### TC-F28-04

**Dia crítico declarado: toda casa ativa recebe DIA_CRITICO e o LED âmbar do nó acende**

**Feature:** F28

**Tipo:** Positivo

**Objetivo:** Provar que a declaração do dia crítico vira um comando para cada casa ativa e acende o âmbar no Wokwi.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Nenhum dia crítico declarado hoje
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/comando'`
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo, em 05h00

**Entradas:**

- `PUT /api/dias-criticos/2026-11-18` com `probabilidade` 0.81 e `origem` `MANUAL`

**Saídas esperadas:**

- Resposta 200 com `abertoEm`
- Um `{"tipo": "DIA_CRITICO"}` em `corredor/v1/<token>/comando` para cada uma das 40 casas ativas
- LED âmbar aceso no Wokwi

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 05h00 e declarar o dia crítico | API responde 200 com `abertoEm` e `encerradoEm` nulo |
| 2 | Olhar o terminal da assinatura | Chega um `DIA_CRITICO` no tópico de comando de cada uma das 40 casas ativas, a casa 1 entre elas |
| 3 | Olhar o nó no Wokwi | O LED âmbar acende |

**Cobre:** P8

### TC-F28-05

**Sem dia crítico o âmbar fica apagado, casa revogada não recebe e o comando não fica retido**

**Feature:** F28

**Tipo:** Negativo

**Objetivo:** Provar que o `DIA_CRITICO` só sai na declaração, só para casa ativa e não fica guardado no broker.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Nenhum dia crítico declarado hoje
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/comando'`
- Swagger da API aberto e autenticado, para conferir o servidor
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo

**Entradas:**

- Dia comum até 09h00
- Consentimento da casa 12 revogado antes da declaração
- Dia crítico declarado às 09h00
- Assinatura nova aberta depois da declaração

**Saídas esperadas:**

- Nenhum comando e LED âmbar apagado no dia comum
- Nenhum `DIA_CRITICO` no tópico da casa 12
- A assinatura nova não recebe nenhum `DIA_CRITICO`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio de 05h00 a 09h00 sem declarar o dia crítico | Nenhum comando na assinatura; o LED âmbar continua apagado |
| 2 | Revogar o consentimento da casa 12 pelo Swagger | API responde 204 |
| 3 | Declarar o dia crítico às 09h00 | Chegam 39 comandos `DIA_CRITICO`; nenhum no tópico de comando da casa 12 |
| 4 | Abrir outro terminal com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/comando' -W 30` | Termina em 30 s sem receber nenhum `DIA_CRITICO`, porque o comando não é retido |

**Cobre:** P8

### TC-F28-06

**Às 14h30, casa Sozinho sem sinal de vida desde as 10h recebe PEDIR_TOQUE e o buzzer toca**

**Feature:** F28

**Tipo:** Positivo

**Objetivo:** Provar que o lembrete das 14h30 chega ao nó da casa sem sinal de vida e toca o buzzer.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/comando'`
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Casa 1 (`Sozinho`) sem presença nem botão desde 10h00

**Entradas:**

- Relógio de demo em 14h30

**Saídas esperadas:**

- `{"tipo": "PEDIR_TOQUE"}` no tópico de comando da casa 1
- Buzzer do nó toca

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 14h29 sem acionar o nó | Nenhum `PEDIR_TOQUE` na assinatura |
| 2 | Mover o relógio para 14h30 | Chega `PEDIR_TOQUE` em `corredor/v1/<token da casa 1>/comando` |
| 3 | Olhar o nó no Wokwi | O buzzer toca |

**Cobre:** P12

### TC-F28-07

**Casa com sinal de vida depois das 10h ou fora do perfil Sozinho não recebe PEDIR_TOQUE**

**Feature:** F28

**Tipo:** Negativo

**Objetivo:** Provar que o pedido de toque só vai para casa `Sozinho` sem sinal de vida desde as 10h, uma vez por dia.

**Preparação:**

- Carga da demo aplicada (`Corredor.Api --semear api/semente/casas.json`) na VM `corredor-ubs`
- Dia crítico de 18/11 declarado às 05h00 (`PUT /api/dias-criticos/2026-11-18`), turno aberto até 18h00
- Projeto do nó no Wokwi rodando, com o token da casa 1 no firmware e o Wi-Fi do Wokwi ligado
- `sudo corredor status` mostra a ponte MQTT conectada a `broker.hivemq.com:1883`
- Terminal na VM com `mosquitto_sub -h 127.0.0.1 -v -t 'corredor/v1/+/comando'`
- Relógio de demo movido por `PUT /api/demo/relogio` para a hora de cada passo
- Simulador das 39 casas parado
- Casa 12 (`Coabitado`) e casa 11 (`Acamado`) sem nenhum evento desde 10h00

**Entradas:**

- Presença acionada no Wokwi às 11h00
- Relógio em 14h30 e depois em 14h45

**Saídas esperadas:**

- Nenhum `PEDIR_TOQUE` para a casa 1, a casa 12 e a casa 11
- Buzzer do nó em silêncio
- Nenhum segundo `PEDIR_TOQUE` no mesmo dia

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Mover o relógio para 11h00 e acionar a presença no Wokwi | O `MOVIMENTO` da casa 1 chega na linha do tempo |
| 2 | Mover o relógio para 14h30 | Nenhum `PEDIR_TOQUE` nos tópicos de comando das casas 1, 12 e 11; o buzzer não toca |
| 3 | Mover o relógio para 14h45 | Nenhum `PEDIR_TOQUE` novo para nenhuma casa |

**Cobre:** P12

## F29 Infraestrutura Linux que se provisiona, se vigia e se recupera

### TC-F29-01

**Provisionamento numa VM nova deixa rede, nomes, firewall e serviços no ar**

**Feature:** F29

**Tipo:** Positivo

**Objetivo:** Provar que um comando só monta a rede, os nomes, o firewall e a aplicação numa VM Ubuntu 24.04 vazia.

**Preparação:**

- VM Ubuntu 24.04 nova: `multipass launch 24.04 --name corredor-ubs --cpus 2 --memory 4G --disk 20G`
- Internet na VM para o `apt`, o GitHub e o broker público

**Entradas:**

- `sudo git clone --depth 1 https://github.com/<org>/corredor-aceso.git /opt/corredor/repo`
- `sudo bash /opt/corredor/repo/infra/provisionar.sh`
- `sudo corredor deploy v1.0.0-rc.1`

**Saídas esperadas:**

- Fim `== tudo certo ==`, com a API em `[PENDENTE]` antes da release
- Hostname `corredor-ubs`, `10.77.0.1/24` em `corredor0`, nomes resolvendo para `10.77.0.1`
- Firewall ativo com 22, 80 e 443
- Depois do deploy, `sudo corredor status` sem `[FALHA]`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Clonar o repositório e rodar o provisionamento | Cada etapa mostra `== 01-rede: N mudança(s) ==`, `02-app` e `03-monitor` com N maior que 0; a conferência final mostra `== tudo certo ==` e a API em `[PENDENTE]` |
| 2 | Rodar `hostname` e `ip addr show corredor0` | `corredor-ubs`; `inet 10.77.0.1/24` em `corredor0` |
| 3 | Rodar `getent hosts api.corredor.internal` e `getent hosts painel.corredor.internal` | Os dois nomes respondem `10.77.0.1` |
| 4 | Rodar `sudo ufw status` | `Status: active`, com `22/tcp`, `80/tcp` e `443/tcp` liberados |
| 5 | Implantar a release e rodar `sudo corredor status` | Itens de rede, serviços e aplicação em `[OK]`, fim `== tudo certo ==` |
| 6 | Rodar `curl http://api.corredor.internal/api/versao` | JSON com `versao`, `commit` e `ambiente` |
| 7 | Mostrar o `api.env` com as senhas mascaradas: `sudo sed 's/Password=[^;]*/Password=***/' /etc/corredor/api.env` | `ASPNETCORE_ENVIRONMENT=Development` e três `ConnectionStrings__`, uma por módulo, sem senha à vista |

**Cobre:** INFRA-RUB-1, INFRA-RUB-2

**Teste automatizado:** `infra/verificar.sh` (existe)

### TC-F29-02

**Segunda execução do provisionamento mostra 0 mudança e não reinicia a API nem o monitor**

**Feature:** F29

**Tipo:** Positivo

**Objetivo:** Provar que o provisionamento é idempotente e que a VM continua servindo o painel.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`

**Entradas:**

- `sudo bash /opt/corredor/repo/infra/provisionar.sh` de novo
- `http://<IP da VM>/painel/` no navegador do notebook

**Saídas esperadas:**

- `0 mudança(s)` em `01-rede`, `02-app` e `03-monitor`
- `corredor-api: não reiniciou` e `corredor-monitor: não reiniciou`, com o mesmo `InvocationID`
- Painel aberto no navegador

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Anotar `systemctl show -p InvocationID corredor-api corredor-monitor` | Dois `InvocationID` anotados |
| 2 | Rodar o provisionamento de novo | As três etapas mostram `0 mudança(s)` e o fim mostra `== tudo certo ==` |
| 3 | Ler o bloco `== processos antes e depois ==` | `corredor-api: não reiniciou` e `corredor-monitor: não reiniciou`, com o mesmo `InvocationID` antes e depois |
| 4 | Abrir `http://<IP da VM>/painel/` no navegador | A página do painel Risco Cidadão abre |

**Cobre:** INFRA-RUB-2, INFRA-RUB-6

**Teste automatizado:** `infra/provisionar.sh` (existe)

### TC-F29-03

**De fora da VM, a porta MQTT fica fechada, o /healthz responde 404 e a zona não resolve outros nomes**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que a rede da VM expõe só o necessário e que o DNS interno responde só por `corredor.internal`.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- `CORREDOR_DNS_ZONA="sim"` em `/etc/corredor/corredor.conf` e provisionamento rodado de novo

**Entradas:**

- `nc -vz -w 5 <IP da VM> 1883` no notebook
- `curl` em `http://api.corredor.internal/healthz/ready`
- `dig +short @10.77.0.1` para `api.corredor.internal` e `example.com`

**Saídas esperadas:**

- Conexão na 1883 não abre
- Mosquitto escutando só em `127.0.0.1`
- `/healthz/ready` pelo Nginx responde 404
- A zona responde `10.77.0.1` para `api.corredor.internal` e nada para `example.com`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | No notebook, rodar `nc -vz -w 5 <IP da VM> 1883` | A conexão não abre: recusada ou tempo esgotado |
| 2 | Na VM, rodar `sudo ss -ltn 'sport = :1883'` | Só `127.0.0.1:1883` escutando |
| 3 | Rodar `curl -s -o /dev/null -w '%{http_code}' http://api.corredor.internal/healthz/ready` | Imprime `404` |
| 4 | Rodar `dig +short @10.77.0.1 api.corredor.internal` | Imprime `10.77.0.1` |
| 5 | Rodar `dig +short @10.77.0.1 example.com` | Não imprime nada |

**Cobre:** INFRA-RUB-1

**Teste automatizado:** `infra/verificar.sh` (existe)

### TC-F29-04

**Provisionamento sem sudo ou em paralelo é recusado e os segredos não abrem sem root**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que o provisionamento exige root, roda um por vez e que as senhas do banco ficam fechadas.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Sessão na VM com o usuário padrão, sem root

**Entradas:**

- `bash /opt/corredor/repo/infra/provisionar.sh` sem sudo
- Dois `sudo bash /opt/corredor/repo/infra/provisionar.sh` ao mesmo tempo, em dois terminais
- `cat /etc/corredor/api.env` e `cat /etc/corredor/segredos/bd-motor.senha` sem sudo

**Saídas esperadas:**

- `[ERRO] rode com sudo: sudo bash /opt/corredor/repo/infra/provisionar.sh` e código 1
- `[ERRO] outro 'provisionamento' está em andamento nesta VM` no segundo terminal
- Leitura dos dois arquivos negada

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o provisionamento sem sudo e depois `echo $?` | `[ERRO] rode com sudo: ...` e código 1; nada muda na VM |
| 2 | Rodar o provisionamento em dois terminais quase juntos | O segundo para com `[ERRO] outro 'provisionamento' está em andamento nesta VM`; o primeiro termina normal |
| 3 | Rodar `cat /etc/corredor/api.env` sem sudo | `Permission denied` |
| 4 | Rodar `cat /etc/corredor/segredos/bd-motor.senha` sem sudo | `Permission denied` |

**Cobre:** INFRA-RUB-2

### TC-F29-05

**VM saudável: o monitor grava métricas a cada 15 s e não gera nenhum alerta**

**Feature:** F29

**Tipo:** Positivo

**Objetivo:** Provar que, com uso normal e serviços no ar, o monitor mede tudo e fica em silêncio.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Limites padrão do monitor: CPU 85% por dois ciclos, memória 85%, disco `/` 80%, ciclo de 15 s
- Um terminal com `sudo tail -f /var/log/corredor/monitor/alertas.log`

**Entradas:**

- Dois minutos de uso normal da VM

**Saídas esperadas:**

- Linhas `cpu=`, `mem=`, `disco:/=`, `mosquitto=ok`, `nginx=ok`, `postgres=ok` e `api=ok` no `monitor.log`
- Nenhuma linha nova no `alertas.log`
- `/healthz/ready` responde 200

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor logs monitor` | Uma linha a cada cerca de 15 s, com CPU, memória e disco abaixo dos limites e `mosquitto=ok nginx=ok postgres=ok api=ok` |
| 2 | Esperar dois minutos olhando o terminal do `alertas.log` | Nenhuma linha nova |
| 3 | Rodar `curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5000/healthz/ready` na VM | Imprime `200` |
| 4 | Rodar `sudo corredor status` | `[OK] Mosquitto ativo`, `[OK] Mosquitto entrega mensagem local`, `[OK] monitor mediu nos últimos 90 s` e `[OK] API pronta em /healthz/ready` |

**Cobre:** INFRA-carga, INFRA-memoria, INFRA-disco, INFRA-broker, INFRA-api, INFRA-RUB-3

**Teste automatizado:** `infra/verificar.sh` (existe)

### TC-F29-06

**Monitor roda como serviço do systemd e o watchdog não mata o laço saudável**

**Feature:** F29

**Tipo:** Positivo

**Objetivo:** Provar que o monitor é um serviço com religamento e watchdog, e que ele avisa o watchdog a cada ciclo.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`

**Entradas:**

- `systemctl status corredor-monitor`
- `cat /opt/corredor/repo/infra/systemd/corredor-monitor.service`
- `systemctl show -p MainPID -p NRestarts corredor-monitor` duas vezes, com 90 s entre elas

**Saídas esperadas:**

- Serviço `active (running)`
- Unit com `Restart=always` e `WatchdogSec=60`
- Mesmo PID e mesmo `NRestarts` depois de 90 s

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `systemctl status corredor-monitor` | `active (running)`, com o usuário `corredor-mon` |
| 2 | Ler a unit do monitor | `Type=notify`, `Restart=always` e `WatchdogSec=60` |
| 3 | Rodar `journalctl -u corredor-monitor -n 20` | Linha `monitor iniciado: a cada 15s, limites cpu=85% mem=85% disco=80%` |
| 4 | Ler `MainPID` e `NRestarts`, esperar 90 s e ler de novo | Os dois valores iguais: o watchdog não matou o monitor |

**Cobre:** INFRA-monitor, INFRA-travado, INFRA-RUB-4

**Teste automatizado:** `infra/verificar.sh` (existe)

### TC-F29-07

**Mosquitto parado: o monitor alerta, o reenvio duplo não duplica eventos e a volta é registrada**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que a queda do broker vira alerta, que o reenvio duplo grava cada evento uma vez e que a volta fica no log.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Um terminal com `sudo tail -f /var/log/corredor/monitor/alertas.log`
- Casa de teste `fa11fa11fa11fa11fa11fa11fa11fa11` ativa no Motor

**Entradas:**

- `sudo corredor teste-de-falha broker`

**Saídas esperadas:**

- `ALERTA serviço mosquitto parado` no `alertas.log`
- 3 eventos guardados e reenviados duas vezes com `bufferizado: true`
- Cada evento uma vez só na linha do tempo da casa de teste
- `NORMALIZADO servico:mosquitto voltou ao normal`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha broker` | O script mostra `mosquitto parado` e três linhas `evento ... guardado no buffer` |
| 2 | Olhar o terminal do `alertas.log` | Aparece `ALERTA serviço mosquitto parado`; o script mostra `[PASSOU] monitor gerou o alerta` |
| 3 | Esperar o religamento | `mosquitto religado`, `Motor reconectado ao broker`, `rodada 1 do reenvio feita` e `rodada 2 do reenvio feita` |
| 4 | Ler os resultados | `[PASSOU] 3 eventos do buffer reenviados duas vezes com a hora original e bufferizado: true` e `[PASSOU] o Motor gravou cada um dos 3 eventos uma vez só` |
| 5 | Chamar `curl http://127.0.0.1:5000/api/casas/fa11fa11fa11fa11fa11fa11fa11fa11/linha-do-tempo` | Cada `seq` reenviado aparece uma vez, com `bufferizado: true` |
| 6 | Olhar o `alertas.log` e rodar `systemctl is-active mosquitto` | `NORMALIZADO servico:mosquitto voltou ao normal`; o Mosquitto está `active` |

**Cobre:** INFRA-broker, INFRA-RUB-6

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_broker` (existe)

### TC-F29-08

**API morta com kill -9 volta sozinha com outro PID e sem alerta de falha definitiva**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que o systemd religa a API depois de uma morte brusca e que o religamento comum não dispara o alerta de desistência.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Um terminal com `sudo tail -f /var/log/corredor/monitor/alertas.log`

**Entradas:**

- `sudo corredor teste-de-falha api`

**Saídas esperadas:**

- API religada com outro PID
- `/healthz/ready` responde 200 em até 60 s
- Nenhum `ALERTA falha definitiva` no `alertas.log`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Anotar `systemctl show -p MainPID corredor-api` | PID anotado |
| 2 | Rodar `sudo corredor teste-de-falha api` | O script mostra `processo <PID> morto` |
| 3 | Ler o resultado | `[PASSOU] API religada e pronta em N s, novo processo <outro PID>`, com N até 60 |
| 4 | Rodar `curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5000/healthz/ready` | Imprime `200` |
| 5 | Procurar `falha definitiva` no `alertas.log` | Nenhuma linha nova |

**Cobre:** INFRA-api, INFRA-RUB-6

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_api` (existe)

### TC-F29-09

**Monitor morto com kill -9 é religado pelo systemd com outro PID**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que o monitor, como serviço com `Restart=always`, volta sozinho depois de uma morte brusca.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`

**Entradas:**

- `sudo corredor teste-de-falha monitor`

**Saídas esperadas:**

- Monitor religado com outro PID em até 30 s
- Nova linha `monitor iniciado` no journal
- `monitor.log` volta a crescer

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha monitor` | O script mostra `processo <PID> morto` |
| 2 | Ler o resultado | `[PASSOU] monitor religado em N s, novo processo <outro PID>`, com N até 30 |
| 3 | Rodar `journalctl -u corredor-monitor -n 5` | Linha `monitor iniciado: a cada 15s ...` depois da morte |
| 4 | Rodar `sudo corredor logs monitor` | Linhas novas de métricas depois do religamento |

**Cobre:** INFRA-monitor, INFRA-RUB-4

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_monitor` (existe)

### TC-F29-10

**Monitor travado com kill -STOP é morto e religado pelo watchdog depois de 60 s**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que um laço travado, mas vivo, não fica parado: o watchdog do systemd percebe e religa.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`

**Entradas:**

- `sudo corredor teste-de-falha travado`

**Saídas esperadas:**

- Processo congelado
- Monitor religado com outro PID depois de 60 s e antes de 100 s

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha travado` | O script mostra `processo <PID> congelado. Aguardando o WatchdogSec de 60 s.` |
| 2 | Durante 60 s, rodar `systemctl show -p MainPID corredor-monitor` | O PID continua o mesmo |
| 3 | Ler o resultado | `[PASSOU] watchdog religou o monitor, novo processo <outro PID>` |
| 4 | Rodar `systemctl status corredor-monitor` | `active (running)` com o PID novo |

**Cobre:** INFRA-travado, INFRA-RUB-4

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_travado` (existe)

### TC-F29-11

**CPU acima de 85% gera ALERTA no alertas.log e depois NORMALIZADO**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que a carga de CPU vira alerta e que o fim da carga é registrado.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Limites padrão do monitor: CPU 85% por dois ciclos, memória 85%, disco `/` 80%, ciclo de 15 s
- Um terminal com `sudo tail -f /var/log/corredor/monitor/alertas.log`

**Entradas:**

- `sudo corredor teste-de-falha carga`, um laço por CPU

**Saídas esperadas:**

- `ALERTA CPU em N% (limite 85%)` em até 90 s
- `NORMALIZADO cpu voltou ao normal` depois do fim da carga

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha carga` | O script mostra `N processo(s) ocupando a CPU`, um por CPU |
| 2 | Olhar o terminal do `alertas.log` | `ALERTA CPU em N% (limite 85%)`; o script mostra `[PASSOU] monitor gerou o alerta` |
| 3 | Esperar o fim da carga | `NORMALIZADO cpu voltou ao normal`; o script mostra `[PASSOU] monitor registrou a CPU de volta ao normal` |
| 4 | Rodar `journalctl -t corredor-monitor -p warning -n 5` | O mesmo `ALERTA CPU` aparece no journal |

**Cobre:** INFRA-carga, INFRA-RUB-3

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_carga` (existe)

### TC-F29-12

**Memória 5 pontos acima do limite gera ALERTA e depois NORMALIZADO, ou o cenário pula com o motivo**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que a memória alta vira alerta e que o teste não arrisca a VM quando falta folga.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Limites padrão do monitor: CPU 85% por dois ciclos, memória 85%, disco `/` 80%, ciclo de 15 s
- Um terminal com `sudo tail -f /var/log/corredor/monitor/alertas.log`

**Entradas:**

- `sudo corredor teste-de-falha memoria`

**Saídas esperadas:**

- `ALERTA memória em N% (limite 85%)` em até 60 s
- `NORMALIZADO memoria voltou ao normal` depois do fim do `stress-ng`
- Sem folga, `[PULADO]` com o motivo e nenhum `stress-ng`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha memoria` | O script mostra `stress-ng ocupando N MB` |
| 2 | Olhar o terminal do `alertas.log` | `ALERTA memória em N% (limite 85%)`; o script mostra `[PASSOU] monitor gerou o alerta` |
| 3 | Esperar o fim do `stress-ng` | `NORMALIZADO memoria voltou ao normal` |
| 4 | Numa VM em que sobrariam menos de 256 MB livres, rodar o cenário | `[PULADO] ocupar N kB deixaria menos de 256 MB livres`; nenhum alerta novo |

**Cobre:** INFRA-memoria, INFRA-RUB-3

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_memoria` (existe)

### TC-F29-13

**Disco / 1 ponto acima de 80% gera ALERTA, depois NORMALIZADO, e o arquivo de teste some**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que o disco cheio vira alerta e que o teste devolve a VM ao estado de antes.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`
- Limites padrão do monitor: CPU 85% por dois ciclos, memória 85%, disco `/` 80%, ciclo de 15 s
- Um terminal com `sudo tail -f /var/log/corredor/monitor/alertas.log`

**Entradas:**

- `sudo corredor teste-de-falha disco`

**Saídas esperadas:**

- `ALERTA disco / em N% (limite 80%)` em até 60 s
- `NORMALIZADO disco:/ voltou ao normal`
- `/var/tmp/corredor-teste-disco.img` apagado no fim

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha disco` | O script mostra `arquivo de N MB em /var/tmp/corredor-teste-disco.img` |
| 2 | Olhar o terminal do `alertas.log` | `ALERTA disco / em N% (limite 80%)`; o script mostra `[PASSOU] monitor gerou o alerta` |
| 3 | Esperar o fim do cenário | `NORMALIZADO disco:/ voltou ao normal` |
| 4 | Rodar `ls /var/tmp/corredor-teste-disco.img` e `df -P /` | O arquivo não existe; o uso do `/` volta ao valor de antes |

**Cobre:** INFRA-disco, INFRA-RUB-3

**Teste automatizado:** `infra/teste-de-falha.sh::cenario_disco` (existe)

### TC-F29-14

**Auditoria registra quem rodou cada script, com início, fim e código, e guarda a saída completa**

**Feature:** F29

**Tipo:** Positivo

**Objetivo:** Provar que cada execução de script deixa uma linha de auditoria e um arquivo com a saída inteira.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`

**Entradas:**

- `sudo bash /opt/corredor/repo/infra/provisionar.sh`
- `sudo corredor teste-de-falha monitor`

**Saídas esperadas:**

- Linhas `evento=inicio` e `evento=fim codigo=0` de cada script, com `usuario=` e `ambiente=`
- Um arquivo por execução em `/var/log/corredor/execucao`

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar o provisionamento e o cenário `monitor` | Os dois terminam com código 0 |
| 2 | Rodar `sudo corredor logs auditoria` | Para `provisionar.sh` e `teste-de-falha.sh`: `script=`, `usuario=` com quem usou o sudo, `evento=inicio args=...` e `evento=fim codigo=0 duracao_s=... mudancas=...` |
| 3 | Rodar `sudo ls -t /var/log/corredor/execucao` | Arquivos `<id>-provisionar.log` e `<id>-teste-de-falha.log` no topo |
| 4 | Abrir o arquivo do provisionamento | A mesma saída que apareceu na tela |
| 5 | Rodar `journalctl -t corredor-auditoria -n 10` | As mesmas linhas de auditoria no journal |

**Cobre:** INFRA-RUB-5

**Teste automatizado:** `infra/verificar.sh` (existe)

### TC-F29-15

**Script que termina com erro fica na auditoria com código 1**

**Feature:** F29

**Tipo:** Negativo

**Objetivo:** Provar que a auditoria registra também as execuções que falham, com o código de saída.

**Preparação:**

- VM `corredor-ubs` provisionada, com a release implantada e `sudo corredor status` terminando em `== tudo certo ==`

**Entradas:**

- `sudo corredor teste-de-falha xyz`, um cenário que não existe

**Saídas esperadas:**

- `[ERRO] cenário desconhecido: xyz` e código 1
- Linha `evento=fim codigo=1` de `teste-de-falha.sh` na auditoria
- Arquivo da execução com a mensagem de erro

**Passos:**

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Rodar `sudo corredor teste-de-falha xyz` e depois `echo $?` | `[ERRO] cenário desconhecido: xyz` e código 1; nenhum resumo |
| 2 | Rodar `sudo corredor logs auditoria` | Linhas `script=teste-de-falha.sh ... evento=inicio args="xyz"` e `evento=fim codigo=1` |
| 3 | Abrir o arquivo mais novo de `/var/log/corredor/execucao` | Contém `[ERRO] cenário desconhecido: xyz` |

**Cobre:** INFRA-RUB-5
