# Features do Corredor Aceso

Arquivo gerado por `docs/qa/ferramentas/gerar.py` a partir de `docs/qa/fonte/features.py`. Edite a fonte, não este arquivo.

Cada Feature vira um work item do tipo Feature no Azure Boards, filho do épico "Corredor Aceso: radar de silêncio para a ACS em dia crítico". A importação está em [README.md](README.md). Os critérios de aceite seguem a forma dado, quando, então. A linha **Cobre** lista as regras e as rotas de `docs/arquitetura/` que a Feature cobre, com os nomes que a [rastreabilidade](rastreabilidade.md) explica.

| Feature | Módulo | Test Cases |
|---|---|---|
| [F01](#f01) Cadastro do idoso pela ACS | Cadastro | 6 |
| [F02](#f02) Consentimento na UBS e ativação da casa | Cadastro e Motor | 7 |
| [F03](#f03) Revogação do consentimento | Cadastro e Motor | 5 |
| [F04](#f04) Vizinho de confiança e autorizações | Cadastro | 7 |
| [F05](#f05) Perfil da casa | Cadastro e Motor | 5 |
| [F06](#f06) Lista de nomes para o celular da ACS | Cadastro e Guarda | 4 |
| [F07](#f07) Recebimento dos pacotes do nó | Motor | 9 |
| [F08](#f08) Sinal de vida conforme o perfil de contato | Motor | 7 |
| [F09](#f09) Aparelho mudo fica cinza e nunca verde | Motor | 6 |
| [F10](#f10) Silêncio na janela e saída avisada | Motor | 8 |
| [F11](#f11) Calor dentro de casa | Motor | 4 |
| [F12](#f12) Energia da casa eletrodependente, com trava | Motor | 7 |
| [F13](#f13) Verde só com prova, Aguardando no resto | Motor | 8 |
| [F14](#f14) Atendimento por confirmação humana | Motor | 12 |
| [F15](#f15) Radar da microárea | Motor | 5 |
| [F16](#f16) Linha do tempo da casa | Motor | 5 |
| [F17](#f17) Visita e desfecho | Cadastro e Motor | 12 |
| [F18](#f18) Aviso neutro ao vizinho e resposta | Motor e Cadastro | 9 |
| [F19](#f19) Dia crítico e turno | Motor | 8 |
| [F20](#f20) Pedido de toque às 14h30 | Motor e nó | 4 |
| [F21](#f21) Relógio de demo | Motor | 5 |
| [F22](#f22) Retenção de 24 horas | Motor | 5 |
| [F23](#f23) Mascaramento de PII na entrada | Guarda | 9 |
| [F24](#f24) Bloqueio de PII nas saídas | Guarda | 12 |
| [F25](#f25) Mensagem para o grupo do bairro | Cadastro e Guarda | 10 |
| [F26](#f26) Tradução por LLM e painel Risco Cidadão | Guarda | 9 |
| [F27](#f27) App da ACS sem rede: fila de desfechos e radar guardado | App | 12 |
| [F28](#f28) Nó do corredor no Wokwi | Nó | 7 |
| [F29](#f29) Infraestrutura Linux que se provisiona, se vigia e se recupera | Infra | 15 |

## F01

**Cadastro do idoso pela ACS**

**Módulo:** Cadastro

**Descrição:** A ACS cadastra o idoso da microárea com nome, endereço, perfil da casa, condições e medicamentos. O cadastro não pede CPF, RG, CNS nem data de nascimento. Nome e endereço são gravados inteiros e levam rótulo de dado pessoal. A casa ainda não tem token: o token só nasce com o consentimento.

**Critérios de aceite:**

1. **Dado** um idoso com nome, endereço e perfil válidos, **quando** a ACS envia `POST /api/idosos`, **então** a API responde 201 com o `id` do idoso e a casa fica sem token.
2. **Dado** um corpo sem nome ou só com espaços, **quando** a ACS envia `POST /api/idosos`, **então** a API responde 422 `NOME_OBRIGATORIO` e nenhum idoso é gravado.
3. **Dado** um corpo sem endereço, **quando** a ACS envia `POST /api/idosos`, **então** a API responde 422 `ENDERECO_OBRIGATORIO` e nenhum idoso é gravado.
4. **Dado** o mesmo corpo enviado duas vezes, **quando** a ACS repete `POST /api/idosos`, **então** a API cria dois idosos com `id` diferentes, porque o cadastro não tem chave de idempotência.
5. **Dado** um corpo com um campo `cpf` ou `dataDeNascimento`, **quando** a ACS envia `POST /api/idosos`, **então** o campo não existe no contrato e não é gravado.

**Cobre:** `POST /idosos`, INV-Idoso-9, INV-Idoso-10, IDEM-Cadastro-CadastrarIdoso

**Test Cases:** [TC-F01-01](casos-de-teste.md#tc-f01-01) (positivo), [TC-F01-02](casos-de-teste.md#tc-f01-02) (negativo), [TC-F01-03](casos-de-teste.md#tc-f01-03) (negativo), [TC-F01-04](casos-de-teste.md#tc-f01-04) (positivo), [TC-F01-05](casos-de-teste.md#tc-f01-05) (negativo), [TC-F01-06](casos-de-teste.md#tc-f01-06) (negativo)

## F02

**Consentimento na UBS e ativação da casa**

**Módulo:** Cadastro e Motor

**Descrição:** O idoso autoriza o monitoramento na UBS, com a ACS presente. O consentimento registra quem colheu, a data, a versão do termo e as autorizações de avisar o vizinho e a família. Só então nasce um token aleatório de 128 bits, e o Cadastro ativa a casa no Motor. Repetir o consentimento vigente devolve o mesmo token. Ao iniciar, o Cadastro reenvia a ativação de toda casa com token.

**Critérios de aceite:**

1. **Dado** um idoso sem consentimento, **quando** a ACS envia `POST /api/idosos/{id}/consentimento`, **então** a API responde 200 com um `casaToken` de 32 hexadecimais minúsculos e a casa aparece no radar como `Cinza`.
2. **Dado** um idoso com consentimento vigente, **quando** a ACS envia o consentimento de novo, **então** a API devolve o mesmo `casaToken` e a casa continua uma só no Motor.
3. **Dado** um idoso sem consentimento, **quando** a equipe consulta a casa e a lista de nomes, **então** a casa não tem token e a pessoa não aparece em `GET /api/lista-de-nomes`.
4. **Dado** o Motor fora do ar, **quando** a ACS envia o consentimento, **então** a API responde 503 `MOTOR_INDISPONIVEL`, o consentimento fica gravado e repetir a chamada ativa a casa.
5. **Dado** um idoso com token e a casa ausente no Motor, **quando** a API reinicia, **então** a reconciliação ativa a casa no Motor sem gerar token novo.
6. **Dado** dois idosos com o mesmo nome e endereço, **quando** cada um recebe consentimento, **então** os tokens são diferentes, porque o token nunca deriva de nome, endereço ou documento.

**Cobre:** `POST /idosos/{id}/consentimento`, INV-Idoso-1, INV-Idoso-2, INV-Idoso-3, INV-Idoso-4, INV-Idoso-5, NC-6, P1, P22, IDEM-Cadastro-RegistrarConsentimento, IDEM-Motor-AtivarCasa, INV-CasaMonitorada-1

**Test Cases:** [TC-F02-01](casos-de-teste.md#tc-f02-01) (positivo), [TC-F02-02](casos-de-teste.md#tc-f02-02) (negativo), [TC-F02-03](casos-de-teste.md#tc-f02-03) (positivo), [TC-F02-04](casos-de-teste.md#tc-f02-04) (negativo), [TC-F02-05](casos-de-teste.md#tc-f02-05) (positivo), [TC-F02-06](casos-de-teste.md#tc-f02-06) (negativo), [TC-F02-07](casos-de-teste.md#tc-f02-07) (negativo)

## F03

**Revogação do consentimento**

**Módulo:** Cadastro e Motor

**Descrição:** O idoso pode revogar o consentimento. O token sai da casa, o vizinho é removido, os avisos daquele token são apagados e o Motor desativa a casa de vez, apagando eventos e confirmações. As visitas ficam, porque são a proteção da ACS. Um consentimento novo gera outro token.

**Critérios de aceite:**

1. **Dado** um idoso com consentimento vigente, **quando** a ACS envia `DELETE /api/idosos/{id}/consentimento`, **então** a API responde 204, a casa some da lista de nomes e do radar e as visitas continuam gravadas.
2. **Dado** uma casa desativada, **quando** chega um pacote ou uma confirmação para o token antigo, **então** o Motor recusa e nada é gravado.
3. **Dado** um idoso já sem consentimento vigente, **quando** a ACS revoga de novo, **então** a API responde 204 e nada muda.
4. **Dado** um idoso revogado, **quando** a ACS registra um consentimento novo, **então** nasce um token diferente do antigo e o token antigo nunca volta.

**Cobre:** `DELETE /idosos/{id}/consentimento`, INV-Idoso-8, P4, INV-CasaMonitorada-3, IDEM-Cadastro-RevogarConsentimento, IDEM-Motor-DesativarCasa, INV-Visita-6

**Test Cases:** [TC-F03-01](casos-de-teste.md#tc-f03-01) (positivo), [TC-F03-02](casos-de-teste.md#tc-f03-02) (negativo), [TC-F03-03](casos-de-teste.md#tc-f03-03) (negativo), [TC-F03-04](casos-de-teste.md#tc-f03-04) (negativo), [TC-F03-05](casos-de-teste.md#tc-f03-05) (negativo)

## F04

**Vizinho de confiança e autorizações**

**Módulo:** Cadastro

**Descrição:** O idoso escolhe um vizinho de confiança, que precisa ter aceitado ser avisado. O vizinho só pode ser indicado quando o idoso autorizou avisar o vizinho. A família nunca entra entre os destinatários sem a autorização de avisar a família.

**Critérios de aceite:**

1. **Dado** um idoso com a autorização `avisarVizinho`, **quando** a ACS envia `PUT /api/idosos/{id}/vizinho` com nome, telefone e `aceitouEm`, **então** a API responde 204 e o vizinho aparece na lista de nomes.
2. **Dado** um idoso sem a autorização `avisarVizinho`, **quando** a ACS tenta indicar o vizinho, **então** a API responde 422 `AUTORIZACAO_AUSENTE` e nenhum vizinho é gravado.
3. **Dado** um vizinho já indicado, **quando** a ACS indica outro vizinho, **então** o novo vizinho substitui o antigo.
4. **Dado** um idoso sem a autorização `avisarFamilia`, **quando** o sistema monta os destinatários do aviso, **então** a família não aparece.

**Cobre:** `PUT /idosos/{id}/vizinho`, INV-Idoso-6, INV-Idoso-7, IDEM-Cadastro-IndicarVizinho

**Test Cases:** [TC-F04-01](casos-de-teste.md#tc-f04-01) (positivo), [TC-F04-02](casos-de-teste.md#tc-f04-02) (negativo), [TC-F04-03](casos-de-teste.md#tc-f04-03) (negativo), [TC-F04-04](casos-de-teste.md#tc-f04-04) (positivo), [TC-F04-05](casos-de-teste.md#tc-f04-05) (negativo), [TC-F04-06](casos-de-teste.md#tc-f04-06) (positivo), [TC-F04-07](casos-de-teste.md#tc-f04-07) (negativo)

## F05

**Perfil da casa**

**Módulo:** Cadastro e Motor

**Descrição:** O perfil da casa junta o perfil de contato (`Sozinho`, `Acamado` ou `Coabitado`) e a eletrodependência. Trocar o perfil no Cadastro troca o perfil no Motor, e o último valor vale.

**Critérios de aceite:**

1. **Dado** uma casa ativa `Sozinho` sem eletrodependência, **quando** a ACS envia `PUT /api/idosos/{id}/perfil` com `Acamado` e eletrodependente, **então** a API responde 204 e o radar passa a mostrar o perfil novo.
2. **Dado** o mesmo perfil enviado duas vezes, **quando** a ACS repete a chamada, **então** o perfil fica igual e nada mais muda.
3. **Dado** um idoso sem token, **quando** a ACS troca o perfil, **então** o Cadastro grava o perfil e não chama o Motor.

**Cobre:** `PUT /idosos/{id}/perfil`, P21, IDEM-Motor-DefinirPerfil, IDEM-Cadastro-IndicarVizinho

**Test Cases:** [TC-F05-01](casos-de-teste.md#tc-f05-01) (positivo), [TC-F05-02](casos-de-teste.md#tc-f05-02) (positivo), [TC-F05-03](casos-de-teste.md#tc-f05-03) (negativo), [TC-F05-04](casos-de-teste.md#tc-f05-04) (negativo), [TC-F05-05](casos-de-teste.md#tc-f05-05) (negativo)

## F06

**Lista de nomes para o celular da ACS**

**Módulo:** Cadastro e Guarda

**Descrição:** O app baixa token, nome, endereço e vizinho das casas ativas da microárea e guarda no SQLite. É o único lugar em que o token encontra o nome. A lista passa pela saída segura com o canal do app autenticado, que aceita nome, endereço e telefone. Condições e medicamentos não vão.

**Critérios de aceite:**

1. **Dado** a ACS autenticada na microárea 3, **quando** o app chama `GET /api/lista-de-nomes?microarea=3`, **então** a resposta traz token, nome, endereço e vizinho só das casas com token ativo.
2. **Dado** a lista pedida, **quando** a resposta é montada, **então** nenhum item traz condições, medicamentos nem observação de visita.
3. **Dado** uma chamada sem autenticação, **quando** alguém pede a lista de nomes, **então** a API recusa e nenhum nome sai.

**Cobre:** `GET /lista-de-nomes?microarea={id}`, P3, CANAL-AppAutenticado

**Test Cases:** [TC-F06-01](casos-de-teste.md#tc-f06-01) (positivo), [TC-F06-02](casos-de-teste.md#tc-f06-02) (negativo), [TC-F06-03](casos-de-teste.md#tc-f06-03) (negativo), [TC-F06-04](casos-de-teste.md#tc-f06-04) (negativo)

## F07

**Recebimento dos pacotes do nó**

**Módulo:** Motor

**Descrição:** O nó do corredor publica movimento, botão, telemetria e heartbeat por MQTT. O Motor grava um evento por chave `(casaToken, seq, ts)`, aceita o `seq` que volta a 1 depois do reinício, recusa `ts` adiantado ou velho, descarta token desconhecido sem logar o conteúdo e usa a hora do servidor no último pacote.

**Critérios de aceite:**

1. **Dado** uma casa ativa, **quando** o nó publica um `MOVIMENTO` válido, **então** nasce um evento do sensor e o último pacote avança para a hora de chegada.
2. **Dado** um pacote já recebido, **quando** o mesmo `(casaToken, seq, ts)` chega de novo, **então** continua existindo um evento só e a repetição conta como sucesso.
3. **Dado** um pacote com `ts` 3 minutos à frente da chegada, **quando** o Motor recebe o pacote, **então** o pacote é recusado inteiro e o último pacote não muda.
4. **Dado** um pacote de token desconhecido, **quando** o Motor recebe o pacote, **então** nada é gravado e o log não traz o conteúdo do pacote.
5. **Dado** um `HEARTBEAT`, **quando** o Motor recebe o pacote, **então** o último pacote avança e nenhum evento do sensor é gravado.
6. **Dado** uma telemetria com 85 °C, **quando** o Motor grava o evento, **então** a temperatura fica ausente e o evento é gravado.

**Cobre:** INV-EventoSensor-1, INV-EventoSensor-2, INV-EventoSensor-3, INV-EventoSensor-4, INV-EventoSensor-5, P2, P6, NC-1, IDEM-Motor-RegistrarPacote, INV-CasaMonitorada-2

**Test Cases:** [TC-F07-01](casos-de-teste.md#tc-f07-01) (positivo), [TC-F07-02](casos-de-teste.md#tc-f07-02) (negativo), [TC-F07-03](casos-de-teste.md#tc-f07-03) (positivo), [TC-F07-04](casos-de-teste.md#tc-f07-04) (negativo), [TC-F07-05](casos-de-teste.md#tc-f07-05) (positivo), [TC-F07-06](casos-de-teste.md#tc-f07-06) (negativo), [TC-F07-07](casos-de-teste.md#tc-f07-07) (positivo), [TC-F07-08](casos-de-teste.md#tc-f07-08) (negativo), [TC-F07-09](casos-de-teste.md#tc-f07-09) (negativo)

## F08

**Sinal de vida conforme o perfil de contato**

**Módulo:** Motor

**Descrição:** O que conta como "deu sinal hoje" depende de quem mora na casa. Na casa `Sozinho`, movimento, botão e confirmação com pessoa vista contam. Na casa `Acamado` e na casa `Coabitado`, outra pessoa pode passar no corredor, então só a confirmação humana com pessoa vista conta.

**Critérios de aceite:**

1. **Dado** uma casa `Sozinho` em turno aberto, **quando** o nó registra um movimento na janela atual, **então** a casa fica `Verde`.
2. **Dado** uma casa `Coabitado` em turno aberto, **quando** o nó registra movimento e botão, **então** a casa continua `Aguardando`.
3. **Dado** uma casa `Coabitado` em turno aberto, **quando** a ACS registra visita `EstavaBem`, **então** a casa fica `Verde`.
4. **Dado** uma casa `Acamado` em turno aberto, **quando** o nó registra movimento, **então** o movimento não conta e a casa entra vermelha no início da janela.

**Cobre:** SV-Sozinho, SV-Acamado, SV-Coabitado, NC-5

**Test Cases:** [TC-F08-01](casos-de-teste.md#tc-f08-01) (positivo), [TC-F08-02](casos-de-teste.md#tc-f08-02) (positivo), [TC-F08-03](casos-de-teste.md#tc-f08-03) (negativo), [TC-F08-04](casos-de-teste.md#tc-f08-04) (positivo), [TC-F08-05](casos-de-teste.md#tc-f08-05) (negativo), [TC-F08-06](casos-de-teste.md#tc-f08-06) (positivo), [TC-F08-07](casos-de-teste.md#tc-f08-07) (negativo)

## F09

**Aparelho mudo fica cinza e nunca verde**

**Módulo:** Motor

**Descrição:** Se o nó para de mandar pacote, a casa fica `Cinza` com o motivo "aparelho sem sinal". A regra do heartbeat vem antes de qualquer regra que olhe sinal de vida, e mede a hora em que o pacote chegou ao servidor. Uma casa ativada que nunca mandou pacote também é cinza.

**Critérios de aceite:**

1. **Dado** uma casa com movimento às 09h00 e último pacote às 09h10, **quando** o radar é consultado às 10h00, **então** a casa aparece `Cinza`, pendente, desde 09h10, e nunca `Verde`.
2. **Dado** uma casa ativada que nunca mandou pacote, **quando** o radar é consultado, **então** a casa aparece `Cinza` desde a ativação.
3. **Dado** uma casa cinza, **quando** o nó volta e reenvia o buffer com horas antigas, **então** a casa sai do cinza, porque a hora de chegada mostra o aparelho ligado agora.
4. **Dado** uma casa cinza atendida às 10h30, **quando** a janela muda às 15h00, **então** a casa volta a cinza pendente.

**Cobre:** R2, P14, NC-2, TV-SIT-2

**Test Cases:** [TC-F09-01](casos-de-teste.md#tc-f09-01) (negativo), [TC-F09-02](casos-de-teste.md#tc-f09-02) (negativo), [TC-F09-03](casos-de-teste.md#tc-f09-03) (positivo), [TC-F09-04](casos-de-teste.md#tc-f09-04) (positivo), [TC-F09-05](casos-de-teste.md#tc-f09-05) (negativo), [TC-F09-06](casos-de-teste.md#tc-f09-06) (negativo)

## F10

**Silêncio na janela e saída avisada**

**Módulo:** Motor

**Descrição:** No turno do dia crítico, a manhã vai da abertura às 10h e a tarde das 10h às 15h. A casa `Sozinho` sem sinal de vida desde o início da janela vencida fica vermelha às 10h ou às 15h. A casa `Acamado` fica vermelha no início de cada janela. Um sinal de vida depois do prazo tira o vermelho daquela janela. O duplo toque "hoje vou sair" suspende esta regra até o fim do turno.

**Critérios de aceite:**

1. **Dado** uma casa `Sozinho` sem sinal de vida desde a abertura às 05h, **quando** o relógio chega às 10h00, **então** a casa fica `Vermelho`, motivo silêncio, desde 10h00, pendente.
2. **Dado** uma casa `Sozinho` com movimento só às 03h12, **quando** o relógio chega às 10h00, **então** a casa fica `Vermelho`, porque o movimento antes da abertura não vale.
3. **Dado** uma casa vermelha desde 10h00, **quando** chega um movimento às 10h30, **então** às 10h31 a casa fica `Verde`.
4. **Dado** uma casa `Sozinho` com saída avisada às 08h40, **quando** o relógio passa das 10h00 e das 15h00, **então** a casa nunca fica `Vermelho` por silêncio no turno.
5. **Dado** uma casa `Acamado` com turno aberto às 05h, **quando** o radar é consultado às 05h01, **então** a casa fica `Vermelho` desde 05h00.

**Cobre:** R4, P9, TV-SIT-4

**Test Cases:** [TC-F10-01](casos-de-teste.md#tc-f10-01) (negativo), [TC-F10-02](casos-de-teste.md#tc-f10-02) (negativo), [TC-F10-03](casos-de-teste.md#tc-f10-03) (positivo), [TC-F10-04](casos-de-teste.md#tc-f10-04) (negativo), [TC-F10-05](casos-de-teste.md#tc-f10-05) (positivo), [TC-F10-06](casos-de-teste.md#tc-f10-06) (negativo), [TC-F10-07](casos-de-teste.md#tc-f10-07) (positivo), [TC-F10-08](casos-de-teste.md#tc-f10-08) (negativo)

## F11

**Calor dentro de casa**

**Módulo:** Motor

**Descrição:** Com o turno aberto, a casa cujas leituras de temperatura ficam em 34 °C ou mais, sem interrupção, por 2 horas (2 minutos na demo) fica vermelha com o motivo calor dentro de casa, mesmo que a pessoa esteja se mexendo.

**Critérios de aceite:**

1. **Dado** uma casa com 35 °C desde as 11h00 e movimento às 13h00, **quando** o radar é consultado às 13h05, **então** a casa aparece `Vermelho`, motivo calor, desde 13h00, pendente.
2. **Dado** uma casa com 35 °C interrompida por uma leitura de 33 °C, **quando** a sequência quente recomeça, **então** o alerta só sai 2 horas depois do recomeço.
3. **Dado** um dia comum sem turno, **quando** a casa passa 3 horas a 36 °C, **então** a regra de calor não devolve alerta e a casa fica `Aguardando`.

**Cobre:** R3, TV-SIT-3

**Test Cases:** [TC-F11-01](casos-de-teste.md#tc-f11-01) (negativo), [TC-F11-02](casos-de-teste.md#tc-f11-02) (positivo), [TC-F11-03](casos-de-teste.md#tc-f11-03) (positivo), [TC-F11-04](casos-de-teste.md#tc-f11-04) (negativo)

## F12

**Energia da casa eletrodependente, com trava**

**Módulo:** Motor

**Descrição:** A casa com equipamento de saúde na tomada fica vermelha em qualquer dia e a qualquer hora quando a tensão cai abaixo de 100 V ou quando fica 60 segundos sem pacote. Depois de uma leitura baixa, a casa continua vermelha até a ACS ver a pessoa depois daquela leitura, mesmo que a luz volte. O vizinho não resolve o alerta de energia.

**Critérios de aceite:**

1. **Dado** uma casa eletrodependente em dia comum, **quando** chega uma telemetria com 0 V, **então** a casa fica `Vermelho`, motivo energia, pendente, no topo do radar.
2. **Dado** uma casa eletrodependente, **quando** passam 61 segundos sem pacote, **então** a casa fica `Vermelho`; com 59 segundos, continua `Aguardando`.
3. **Dado** uma casa vermelha por energia, **quando** o vizinho responde `EstaBem`, **então** a casa continua vermelha e pendente.
4. **Dado** a luz voltou com 127 V depois de uma leitura de 0 V, **quando** o radar é consultado, **então** a casa continua vermelha e pendente pela trava.
5. **Dado** a trava armada, **quando** a ACS registra visita `EstavaBem` depois da última leitura baixa, **então** a trava solta e, sem turno, a casa fica `Aguardando`.

**Cobre:** R1, NC-3, TV-SIT-1

**Test Cases:** [TC-F12-01](casos-de-teste.md#tc-f12-01) (negativo), [TC-F12-02](casos-de-teste.md#tc-f12-02) (negativo), [TC-F12-03](casos-de-teste.md#tc-f12-03) (positivo), [TC-F12-04](casos-de-teste.md#tc-f12-04) (positivo), [TC-F12-05](casos-de-teste.md#tc-f12-05) (positivo), [TC-F12-06](casos-de-teste.md#tc-f12-06) (negativo), [TC-F12-07](casos-de-teste.md#tc-f12-07) (negativo)

## F13

**Verde só com prova, Aguardando no resto**

**Módulo:** Motor

**Descrição:** Verde quer dizer que existe sinal de vida na janela atual e que o turno está aberto. Sem alerta e sem essa prova, a casa fica `Aguardando`: no dia comum, depois do encerramento, na casa `Coabitado` sem confirmação e na casa que ainda está no prazo. Depois do encerramento só valem as regras de energia e de heartbeat.

**Critérios de aceite:**

1. **Dado** uma casa `Sozinho` sem sinal de vida com aparelho ligado às 09h00, **quando** o radar é consultado, **então** a casa aparece `Aguardando` e nunca `Verde`.
2. **Dado** uma casa com movimento às 07h00, **quando** o radar é consultado às 12h30, **então** a casa aparece `Aguardando`, porque a tarde pede sinal novo e ainda não venceu.
3. **Dado** um turno encerrado, **quando** o radar é consultado às 18h30, **então** nenhuma casa aparece `Verde` e só energia e heartbeat geram alerta.
4. **Dado** um dia comum, **quando** o radar é consultado, **então** nenhuma casa aparece `Verde`.

**Cobre:** TV-SIT-5, TV-SIT-6, TV-SIT-7, NC-4, P13, TV-SIT-2

**Test Cases:** [TC-F13-01](casos-de-teste.md#tc-f13-01) (negativo), [TC-F13-02](casos-de-teste.md#tc-f13-02) (positivo), [TC-F13-03](casos-de-teste.md#tc-f13-03) (negativo), [TC-F13-04](casos-de-teste.md#tc-f13-04) (negativo), [TC-F13-05](casos-de-teste.md#tc-f13-05) (negativo), [TC-F13-06](casos-de-teste.md#tc-f13-06) (negativo), [TC-F13-07](casos-de-teste.md#tc-f13-07) (positivo), [TC-F13-08](casos-de-teste.md#tc-f13-08) (positivo)

## F14

**Atendimento por confirmação humana**

**Módulo:** Motor

**Descrição:** Uma casa em alerta fica atendida quando existe uma confirmação que a regra aceita, feita depois do início do alerta e dentro da janela atual. Cada regra aceita origens e resultados diferentes. A casa atendida continua vermelha ou cinza, sai das pendentes e nunca conta como verde.

**Critérios de aceite:**

1. **Dado** uma casa cinza pendente, **quando** a ACS ou o vizinho confirmam pessoa vista na janela atual, **então** a casa fica cinza e atendida.
2. **Dado** uma casa vermelha por energia, **quando** chega confirmação do vizinho ou ausência confirmada da ACS, **então** a casa continua pendente.
3. **Dado** uma casa vermelha por silêncio, **quando** a ACS confirma ausência, **então** a casa fica vermelha e atendida, só na janela atual.
4. **Dado** uma confirmação com o mesmo `Id`, **quando** ela chega duas vezes, **então** o Motor guarda uma só.
5. **Dado** uma confirmação com hora 5 minutos no futuro ou com mais de 24 horas, **quando** ela chega ao Motor, **então** o Motor recusa.

**Cobre:** TV-ATD-1, TV-ATD-2, TV-ATD-3, TV-ATD-4, TV-ATD-5, TV-ATD-6, TV-ATD-7, TV-ATD-8, TV-ATD-9, INV-CasaMonitorada-4, INV-CasaMonitorada-5, IDEM-Motor-Confirmar

**Test Cases:** [TC-F14-01](casos-de-teste.md#tc-f14-01) (positivo), [TC-F14-02](casos-de-teste.md#tc-f14-02) (negativo), [TC-F14-03](casos-de-teste.md#tc-f14-03) (negativo), [TC-F14-04](casos-de-teste.md#tc-f14-04) (positivo), [TC-F14-05](casos-de-teste.md#tc-f14-05) (positivo), [TC-F14-06](casos-de-teste.md#tc-f14-06) (negativo), [TC-F14-07](casos-de-teste.md#tc-f14-07) (positivo), [TC-F14-08](casos-de-teste.md#tc-f14-08) (positivo), [TC-F14-09](casos-de-teste.md#tc-f14-09) (negativo), [TC-F14-10](casos-de-teste.md#tc-f14-10) (positivo), [TC-F14-11](casos-de-teste.md#tc-f14-11) (negativo), [TC-F14-12](casos-de-teste.md#tc-f14-12) (positivo)

## F15

**Radar da microárea**

**Módulo:** Motor

**Descrição:** O radar mostra à ACS as casas pendentes, as atendidas e as que aguardam, em ordem, com a contagem de verdes. Vermelho vem antes de cinza, depois a ordem da regra e depois o alerta mais antigo. O radar não traz nome: o app junta o token ao nome no celular.

**Critérios de aceite:**

1. **Dado** casas em vermelho por energia, vermelho por silêncio e cinza, **quando** o app chama `GET /api/radar?microarea=3`, **então** as pendentes vêm nessa ordem e cada card traz token, situação, motivo, desde, perfil e último pacote.
2. **Dado** uma casa atendida, **quando** o radar é montado, **então** ela aparece em `atendidas` e não soma em `verdes`.
3. **Dado** a ACS da microárea 3, **quando** ela pede o radar da microárea 4, **então** a API recusa.

**Cobre:** `GET /radar?microarea={id}`

**Test Cases:** [TC-F15-01](casos-de-teste.md#tc-f15-01) (positivo), [TC-F15-02](casos-de-teste.md#tc-f15-02) (positivo), [TC-F15-03](casos-de-teste.md#tc-f15-03) (positivo), [TC-F15-04](casos-de-teste.md#tc-f15-04) (negativo), [TC-F15-05](casos-de-teste.md#tc-f15-05) (negativo)

## F16

**Linha do tempo da casa**

**Módulo:** Motor

**Descrição:** A tela de detalhe mostra os eventos do sensor e as confirmações das últimas 24 horas de uma casa, em ordem de ocorrência, com a marca de evento bufferizado. A resposta não tem nome, endereço nem texto livre. Casa desconhecida ou desativada responde 404.

**Critérios de aceite:**

1. **Dado** uma casa ativa com eventos e confirmações, **quando** o app chama `GET /api/casas/{token}/linha-do-tempo`, **então** a resposta traz eventos e confirmações das últimas 24 horas em ordem de `ocorridoEm`.
2. **Dado** um token desativado, **quando** o app pede a linha do tempo, **então** a API responde 404 `CASA_DESCONHECIDA`, igual a um token que nunca existiu.
3. **Dado** qualquer casa, **quando** a linha do tempo é devolvida, **então** nenhum campo traz nome, endereço, telefone ou texto livre.

**Cobre:** `GET /casas/{token}/linha-do-tempo`, INV-CasaMonitorada-6

**Test Cases:** [TC-F16-01](casos-de-teste.md#tc-f16-01) (positivo), [TC-F16-02](casos-de-teste.md#tc-f16-02) (negativo), [TC-F16-03](casos-de-teste.md#tc-f16-03) (negativo), [TC-F16-04](casos-de-teste.md#tc-f16-04) (negativo), [TC-F16-05](casos-de-teste.md#tc-f16-05) (positivo)

## F17

**Visita e desfecho**

**Módulo:** Cadastro e Motor

**Descrição:** Na porta, a ACS registra o desfecho: estava bem, encaminhada, estava fora ou não abriu. A visita é imutável e sobrevive ao expurgo. Só estava bem e encaminhada são sinal de vida. Estava fora atende só o alerta de silêncio. Não abriu não confirma nada, e o app mostra "SAMU chamado". Repetir o mesmo `id` não duplica.

**Critérios de aceite:**

1. **Dado** uma casa vermelha por silêncio, **quando** a ACS envia `POST /api/visitas` com `EstavaBem`, **então** a API responde 201 e a casa fica `Verde` na janela atual.
2. **Dado** uma casa vermelha por silêncio, **quando** a ACS registra `NaoAbriu`, **então** a casa continua vermelha e pendente e o Motor não recebe confirmação.
3. **Dado** uma casa vermelha por silêncio, **quando** a ACS registra `EstavaFora`, **então** a casa fica vermelha e atendida e nunca verde.
4. **Dado** uma visita já registrada, **quando** o app reenvia o mesmo `id`, **então** a API responde 200 com a visita que existe e o Motor guarda uma confirmação só.
5. **Dado** um token revogado, **quando** a ACS registra visita, **então** a API responde 422 `TOKEN_INATIVO`.
6. **Dado** uma `ocorridaEm` 5 minutos no futuro, **quando** a ACS registra visita, **então** a API responde 422 `VISITA_NO_FUTURO`.

**Cobre:** `POST /visitas`, DESF-EstavaBem, DESF-Encaminhada, DESF-EstavaFora, DESF-NaoAbriu, INV-Visita-1, INV-Visita-2, INV-Visita-3, INV-Visita-4, INV-Visita-5, INV-Visita-7, P11, P18, NC-7, IDEM-Cadastro-RegistrarVisita

**Test Cases:** [TC-F17-01](casos-de-teste.md#tc-f17-01) (positivo), [TC-F17-02](casos-de-teste.md#tc-f17-02) (negativo), [TC-F17-03](casos-de-teste.md#tc-f17-03) (positivo), [TC-F17-04](casos-de-teste.md#tc-f17-04) (negativo), [TC-F17-05](casos-de-teste.md#tc-f17-05) (positivo), [TC-F17-06](casos-de-teste.md#tc-f17-06) (positivo), [TC-F17-07](casos-de-teste.md#tc-f17-07) (negativo), [TC-F17-08](casos-de-teste.md#tc-f17-08) (negativo), [TC-F17-09](casos-de-teste.md#tc-f17-09) (positivo), [TC-F17-10](casos-de-teste.md#tc-f17-10) (negativo), [TC-F17-11](casos-de-teste.md#tc-f17-11) (negativo), [TC-F17-12](casos-de-teste.md#tc-f17-12) (negativo)

## F18

**Aviso neutro ao vizinho e resposta**

**Módulo:** Motor e Cadastro

**Descrição:** Para cada vermelho pendente, e para cada cinza pendente com turno aberto, o Motor manda um pedido de contato. Se o idoso autorizou e tem vizinho, o Cadastro registra um aviso por episódio e envia o texto neutro, sem nome, endereço nem saúde. O vizinho responde `EstaBem`, que confirma pessoa vista, ou `PrecisaDeAjuda`, que não confirma nada. O envio é simulado.

**Critérios de aceite:**

1. **Dado** uma casa autorizada com vizinho que ficou vermelha às 10h00, **quando** o vigia do radar roda, **então** nasce um aviso e `GET /api/avisos?microarea=3` mostra "vizinho avisado às 10h00".
2. **Dado** o mesmo alerta nos ciclos seguintes, **quando** o vigia manda o pedido de novo, **então** continua um aviso só para o episódio.
3. **Dado** um idoso sem a autorização `avisarVizinho`, **quando** a casa fica vermelha, **então** nenhum aviso é criado.
4. **Dado** um aviso aberto, **quando** o vizinho responde `EstaBem` por `POST /api/avisos/{id}/resposta`, **então** a casa recebe confirmação do vizinho com pessoa vista.
5. **Dado** um aviso respondido com `EstaBem`, **quando** chega a resposta `PrecisaDeAjuda`, **então** a API responde 409 `AVISO_JA_RESPONDIDO`.

**Cobre:** `GET /avisos?microarea={id}`, `POST /avisos/{id}/resposta`, INV-AvisoAoVizinho-1, INV-AvisoAoVizinho-2, INV-AvisoAoVizinho-3, INV-AvisoAoVizinho-4, P10, P19, P20, IDEM-Cadastro-PedidoDeContato, IDEM-Cadastro-RegistrarResposta, CANAL-Vizinho

**Test Cases:** [TC-F18-01](casos-de-teste.md#tc-f18-01) (positivo), [TC-F18-02](casos-de-teste.md#tc-f18-02) (negativo), [TC-F18-03](casos-de-teste.md#tc-f18-03) (positivo), [TC-F18-04](casos-de-teste.md#tc-f18-04) (negativo), [TC-F18-05](casos-de-teste.md#tc-f18-05) (negativo), [TC-F18-06](casos-de-teste.md#tc-f18-06) (positivo), [TC-F18-07](casos-de-teste.md#tc-f18-07) (negativo), [TC-F18-08](casos-de-teste.md#tc-f18-08) (negativo), [TC-F18-09](casos-de-teste.md#tc-f18-09) (negativo)

## F19

**Dia crítico e turno**

**Módulo:** Motor

**Descrição:** Às 05h, o job de previsão declara o dia crítico com a probabilidade, o motivo e a origem. Isso abre o turno e acende o LED âmbar dos nós. Repetir a declaração atualiza a previsão sem mexer na abertura. A ACS encerra o turno, e o resumo do turno guarda o indicador de impacto. Às 18h, o turno fecha sozinho se ninguém encerrou.

**Critérios de aceite:**

1. **Dado** hoje é 18/11, **quando** o job envia `PUT /api/dias-criticos/2026-11-18` com probabilidade 0,81, **então** a API responde 200, o turno abre e cada casa ativa recebe o comando `DIA_CRITICO`.
2. **Dado** um turno aberto às 05h00, **quando** o job repete a declaração às 05h01, **então** continua um turno e `abertoEm` continua 05h00.
3. **Dado** uma data diferente de hoje ou probabilidade 1,2, **quando** o job declara, **então** a API responde 422 `DATA_NAO_E_HOJE` ou `PROBABILIDADE_FORA_DA_FAIXA`.
4. **Dado** um turno aberto, **quando** a ACS envia `POST /api/dias-criticos/2026-11-18/encerramento`, **então** a API responde 200 com o resumo de cinco números e repetir devolve o mesmo resumo.
5. **Dado** um turno encerrado, **quando** o job declara de novo, **então** a API responde 409 `TURNO_ENCERRADO` e o turno não reabre.

**Cobre:** `PUT /dias-criticos/{data}`, `POST /dias-criticos/{data}/encerramento`, INV-DiaCritico-1, INV-DiaCritico-2, INV-DiaCritico-3, INV-DiaCritico-4, INV-DiaCritico-5, INV-DiaCritico-7, P8, IDEM-Motor-DeclararDiaCritico, IDEM-Motor-EncerrarTurno

**Test Cases:** [TC-F19-01](casos-de-teste.md#tc-f19-01) (positivo), [TC-F19-02](casos-de-teste.md#tc-f19-02) (positivo), [TC-F19-03](casos-de-teste.md#tc-f19-03) (negativo), [TC-F19-04](casos-de-teste.md#tc-f19-04) (negativo), [TC-F19-05](casos-de-teste.md#tc-f19-05) (positivo), [TC-F19-06](casos-de-teste.md#tc-f19-06) (negativo), [TC-F19-07](casos-de-teste.md#tc-f19-07) (negativo), [TC-F19-08](casos-de-teste.md#tc-f19-08) (negativo)

## F20

**Pedido de toque às 14h30**

**Módulo:** Motor e nó

**Descrição:** Às 14h30 do dia crítico, o Motor toca o buzzer das casas `Sozinho` sem sinal de vida desde as 10h, para lembrar a pessoa de dar sinal antes do prazo das 15h. O pedido sai uma vez por dia.

**Critérios de aceite:**

1. **Dado** uma casa `Sozinho` sem sinal de vida desde as 10h, **quando** o relógio chega às 14h30, **então** a casa recebe um `PEDIR_TOQUE`.
2. **Dado** o toque já pedido hoje, **quando** o vigia roda de novo, **então** nenhum segundo `PEDIR_TOQUE` sai no dia.
3. **Dado** uma casa `Sozinho` com movimento às 11h00, **quando** o relógio chega às 14h30, **então** a casa não recebe `PEDIR_TOQUE`.

**Cobre:** P12, INV-DiaCritico-6, IDEM-Motor-PedirToque

**Test Cases:** [TC-F20-01](casos-de-teste.md#tc-f20-01) (positivo), [TC-F20-02](casos-de-teste.md#tc-f20-02) (negativo), [TC-F20-03](casos-de-teste.md#tc-f20-03) (negativo), [TC-F20-04](casos-de-teste.md#tc-f20-04) (negativo)

## F21

**Relógio de demo**

**Módulo:** Motor

**Descrição:** Para gravar as janelas das 10h e das 15h sem esperar, a equipe move a hora do Motor por `PUT /api/demo/relogio`. O mesmo deslocamento vale para o `ts` dos pacotes e para a hora das visitas. A rota só existe no ambiente `Development`.

**Critérios de aceite:**

1. **Dado** a API em `Development`, **quando** a equipe envia `PUT /api/demo/relogio` com 09h58, **então** a API responde 200 com o deslocamento e o radar passa a calcular com 09h58.
2. **Dado** o relógio movido, **quando** o nó publica um pacote com a hora real, **então** o pacote cai na hora deslocada e não é recusado como adiantado.
3. **Dado** a API em `Production`, **quando** alguém chama `PUT /api/demo/relogio`, **então** a rota não existe e a hora não muda.

**Cobre:** `PUT /demo/relogio`, NC-10

**Test Cases:** [TC-F21-01](casos-de-teste.md#tc-f21-01) (positivo), [TC-F21-02](casos-de-teste.md#tc-f21-02) (positivo), [TC-F21-03](casos-de-teste.md#tc-f21-03) (negativo), [TC-F21-04](casos-de-teste.md#tc-f21-04) (negativo), [TC-F21-05](casos-de-teste.md#tc-f21-05) (positivo)

## F22

**Retenção de 24 horas**

**Módulo:** Motor

**Descrição:** De hora em hora, o expurgo apaga eventos do sensor e confirmações com mais de 24 horas. Ele guarda a leitura de tensão baixa da casa ativa e eletrodependente enquanto a ACS não viu a pessoa depois dela, porque a trava depende dessa leitura. As visitas nunca são apagadas.

**Critérios de aceite:**

1. **Dado** um movimento com 25 horas, **quando** o expurgo roda, **então** o evento some e a linha do tempo não o mostra.
2. **Dado** uma leitura baixa com 25 horas sem confirmação da ACS depois, **quando** o expurgo roda, **então** a leitura fica e a casa continua vermelha.
3. **Dado** o expurgo já rodou, **quando** ele roda de novo, **então** nada muda.
4. **Dado** uma visita com 3 dias, **quando** o expurgo roda, **então** a visita continua no Cadastro.

**Cobre:** P7, IDEM-Motor-Expurgo, INV-Visita-6

**Test Cases:** [TC-F22-01](casos-de-teste.md#tc-f22-01) (positivo), [TC-F22-02](casos-de-teste.md#tc-f22-02) (negativo), [TC-F22-03](casos-de-teste.md#tc-f22-03) (positivo), [TC-F22-04](casos-de-teste.md#tc-f22-04) (negativo), [TC-F22-05](casos-de-teste.md#tc-f22-05) (negativo)

## F23

**Mascaramento de PII na entrada**

**Módulo:** Guarda

**Descrição:** Toda requisição passa pelo interceptador da Guarda. CPF, RG, CNS e credencial são mascarados em qualquer campo antes do log e do banco, e cada mascaramento vira um evento de segurança sem o valor. Nome, endereço, saúde e mora sozinho não mudam o corpo: viram rótulo do texto.

**Critérios de aceite:**

1. **Dado** uma observação com `123.456.789-00`, **quando** a ACS envia a requisição, **então** o banco e o log guardam `***.456.***-00` e o painel ganha um mascaramento no canal `Banco`.
2. **Dado** um corpo com a chave `senha`, **quando** a requisição chega, **então** o valor é mascarado e o evento registra o tipo credencial.
3. **Dado** um corpo com `casaToken` de 32 hexadecimais, **quando** a requisição chega, **então** o token não é mascarado e a visita segue.
4. **Dado** um mascaramento registrado, **quando** a equipe lê o evento no banco, **então** o evento tem tipo de dado, canal, módulo e rota modelo, sem o valor, sem trecho do texto e sem a ACS.

**Cobre:** P15, CANAL-Banco, INV-EventoDeSeguranca-1, INV-EventoDeSeguranca-2, INV-EventoDeSeguranca-4, IDEM-Guarda-Registrar

**Test Cases:** [TC-F23-01](casos-de-teste.md#tc-f23-01) (positivo), [TC-F23-02](casos-de-teste.md#tc-f23-02) (positivo), [TC-F23-03](casos-de-teste.md#tc-f23-03) (negativo), [TC-F23-04](casos-de-teste.md#tc-f23-04) (positivo), [TC-F23-05](casos-de-teste.md#tc-f23-05) (negativo), [TC-F23-06](casos-de-teste.md#tc-f23-06) (negativo), [TC-F23-07](casos-de-teste.md#tc-f23-07) (negativo), [TC-F23-08](casos-de-teste.md#tc-f23-08) (positivo), [TC-F23-09](casos-de-teste.md#tc-f23-09) (negativo)

## F24

**Bloqueio de PII nas saídas**

**Módulo:** Guarda

**Descrição:** Um texto com rótulo de dado pessoal só sai pela saída segura, que consulta a política de cada canal. Log, resposta sem autenticação, mensagem de erro, MQTT e prompt do LLM não aceitam rótulo. Cada bloqueio grava um evento de segurança que a aplicação não altera nem apaga.

**Critérios de aceite:**

1. **Dado** o código tenta gravar `Idoso.Nome` no log, **quando** a linha de log é escrita, **então** o log recebe `[NOME]` e o painel ganha um bloqueio no canal `Log`.
2. **Dado** uma rota sem autenticação que devolveria `Idoso.Condicoes`, **quando** a rota responde, **então** a resposta sai bloqueada.
3. **Dado** uma exceção cuja mensagem leva um nome, **quando** o erro vai para a tela do app, **então** a mensagem é trocada por texto neutro.
4. **Dado** um texto sem rótulo, **quando** ele passa pela saída segura no canal `Log`, **então** o texto sai inteiro e nenhum evento é gravado.

**Cobre:** CANAL-Log, CANAL-RespostaSemAutenticacao, CANAL-MensagemDeErro, CANAL-Mqtt, CANAL-PromptDoLlm, NC-9, INV-EventoDeSeguranca-5

**Test Cases:** [TC-F24-01](casos-de-teste.md#tc-f24-01) (negativo), [TC-F24-02](casos-de-teste.md#tc-f24-02) (positivo), [TC-F24-03](casos-de-teste.md#tc-f24-03) (negativo), [TC-F24-04](casos-de-teste.md#tc-f24-04) (positivo), [TC-F24-05](casos-de-teste.md#tc-f24-05) (negativo), [TC-F24-06](casos-de-teste.md#tc-f24-06) (positivo), [TC-F24-07](casos-de-teste.md#tc-f24-07) (positivo), [TC-F24-08](casos-de-teste.md#tc-f24-08) (negativo), [TC-F24-09](casos-de-teste.md#tc-f24-09) (positivo), [TC-F24-10](casos-de-teste.md#tc-f24-10) (negativo), [TC-F24-11](casos-de-teste.md#tc-f24-11) (positivo), [TC-F24-12](casos-de-teste.md#tc-f24-12) (negativo)

## F25

**Mensagem para o grupo do bairro**

**Módulo:** Cadastro e Guarda

**Descrição:** Antes de pedir ajuda no grupo do bairro, a ACS verifica a mensagem no app. Qualquer rótulo de dado pessoal bloqueia o envio: a resposta explica em duas frases o que vazaria e oferece uma versão segura para copiar. O sistema não envia nada ao grupo e nunca grava o texto original.

**Critérios de aceite:**

1. **Dado** o texto "ele mora sozinho e toma diurético", **quando** a ACS envia `POST /api/mensagens/verificar` com o canal `GrupoDoBairro`, **então** a API responde 200 com veredito `Bloqueada`, tipos `MoraSozinho` e `Saude`, a frase fixa e a versão segura.
2. **Dado** uma mensagem bloqueada, **quando** a equipe lê a tabela de mensagens, **então** só existe o texto mascarado.
3. **Dado** a versão segura, **quando** a ACS verifica esse texto, **então** o veredito é `Liberada` e a explicação sai nula.
4. **Dado** uma mensagem já verificada, **quando** a ACS verifica o mesmo texto de novo, **então** nasce outra mensagem com o próprio veredito, sem mudar a primeira.

**Cobre:** `POST /mensagens/verificar`, INV-MensagemSaida-1, INV-MensagemSaida-2, INV-MensagemSaida-3, INV-MensagemSaida-4, INV-MensagemSaida-5, P16, NC-8, CANAL-GrupoDoBairro, IDEM-Cadastro-CadastrarIdoso

**Test Cases:** [TC-F25-01](casos-de-teste.md#tc-f25-01) (negativo), [TC-F25-02](casos-de-teste.md#tc-f25-02) (negativo), [TC-F25-03](casos-de-teste.md#tc-f25-03) (negativo), [TC-F25-04](casos-de-teste.md#tc-f25-04) (positivo), [TC-F25-05](casos-de-teste.md#tc-f25-05) (positivo), [TC-F25-06](casos-de-teste.md#tc-f25-06) (negativo), [TC-F25-07](casos-de-teste.md#tc-f25-07) (negativo), [TC-F25-08](casos-de-teste.md#tc-f25-08) (positivo), [TC-F25-09](casos-de-teste.md#tc-f25-09) (negativo), [TC-F25-10](casos-de-teste.md#tc-f25-10) (negativo)

## F26

**Tradução por LLM e painel Risco Cidadão**

**Módulo:** Guarda

**Descrição:** Cada bloqueio é traduzido por um LLM numa mensagem de duas frases para um cidadão leigo. O prompt leva só o tipo de dado, o canal e o módulo. Sem resposta em 5 segundos, entra a frase fixa. O painel aberto mostra o farol das últimas 24 horas e a mensagem de cada evento, sem nenhum valor pessoal.

**Critérios de aceite:**

1. **Dado** uma saída bloqueada de severidade 4 nas últimas 24 horas, **quando** o cidadão abre `/painel`, **então** o farol aparece vermelho com a mensagem traduzida do evento.
2. **Dado** só mascaramentos na entrada nas últimas 24 horas, **quando** o cidadão abre o painel, **então** o farol aparece amarelo.
3. **Dado** nenhum evento nas últimas 24 horas, **quando** o cidadão abre o painel, **então** o farol aparece verde.
4. **Dado** o LLM fora do ar, **quando** uma saída é bloqueada, **então** a mensagem ao cidadão é a frase fixa do tipo de evento.
5. **Dado** qualquer evento, **quando** `GET /api/painel` responde sem autenticação, **então** nenhum campo traz o valor de um dado pessoal.

**Cobre:** `GET /painel`, P17, FAROL-Vermelho, FAROL-Amarelo, FAROL-Verde, INV-EventoDeSeguranca-3

**Test Cases:** [TC-F26-01](casos-de-teste.md#tc-f26-01) (positivo), [TC-F26-02](casos-de-teste.md#tc-f26-02) (positivo), [TC-F26-03](casos-de-teste.md#tc-f26-03) (negativo), [TC-F26-04](casos-de-teste.md#tc-f26-04) (positivo), [TC-F26-05](casos-de-teste.md#tc-f26-05) (negativo), [TC-F26-06](casos-de-teste.md#tc-f26-06) (negativo), [TC-F26-07](casos-de-teste.md#tc-f26-07) (negativo), [TC-F26-08](casos-de-teste.md#tc-f26-08) (negativo), [TC-F26-09](casos-de-teste.md#tc-f26-09) (negativo)

## F27

**App da ACS sem rede: fila de desfechos e radar guardado**

**Módulo:** App

**Descrição:** A ACS trabalha na rua, com sinal ruim. O app grava o desfecho no SQLite antes de qualquer chamada e envia a fila uma visita por vez, com o mesmo `id` em cada tentativa. O radar e a lista de nomes ficam guardados e são trocados inteiros. Um radar velho se declara velho, e um card sem nome nunca some.

**Critérios de aceite:**

1. **Dado** o celular sem rede, **quando** a ACS registra um desfecho, **então** o desfecho fica `pendente` no celular e sobe com o mesmo `id` quando a rede volta.
2. **Dado** o envio recebe 422 `TOKEN_INATIVO`, **quando** o app trata a resposta, **então** o desfecho fica `recusado`, o card mostra "Não enviado" com o motivo e o app não tenta de novo.
3. **Dado** o radar baixado há mais de 2 minutos, **quando** a ACS abre o radar sem rede, **então** o título mostra "sem conexão" com a hora do radar.
4. **Dado** um card cujo token não está na lista de nomes, **quando** o radar é desenhado, **então** o card aparece com "Casa sem nome. Atualize a lista.".
5. **Dado** uma resposta de erro ao baixar a lista, **quando** o app trata a resposta, **então** a lista guardada continua igual.

**Cobre:** APP-CACHE-1, APP-CACHE-2, APP-CACHE-3, APP-CACHE-4, APP-CACHE-5, APP-CACHE-6, APP-FILA-1, APP-FILA-2, APP-FILA-3, APP-FILA-4

**Test Cases:** [TC-F27-01](casos-de-teste.md#tc-f27-01) (positivo), [TC-F27-02](casos-de-teste.md#tc-f27-02) (negativo), [TC-F27-03](casos-de-teste.md#tc-f27-03) (negativo), [TC-F27-04](casos-de-teste.md#tc-f27-04) (negativo), [TC-F27-05](casos-de-teste.md#tc-f27-05) (positivo), [TC-F27-06](casos-de-teste.md#tc-f27-06) (negativo), [TC-F27-07](casos-de-teste.md#tc-f27-07) (positivo), [TC-F27-08](casos-de-teste.md#tc-f27-08) (positivo), [TC-F27-09](casos-de-teste.md#tc-f27-09) (positivo), [TC-F27-10](casos-de-teste.md#tc-f27-10) (negativo), [TC-F27-11](casos-de-teste.md#tc-f27-11) (negativo), [TC-F27-12](casos-de-teste.md#tc-f27-12) (negativo)

## F28

**Nó do corredor no Wokwi**

**Módulo:** Nó

**Descrição:** O ESP32 do corredor acende a luz de caminho quando a idosa passa, sem depender da rede, e publica o movimento. Ele acende o LED âmbar no dia crítico e toca o buzzer no pedido de toque. Sem Wi-Fi, ele guarda os eventos e reenvia com a hora original quando a rede volta.

**Critérios de aceite:**

1. **Dado** o nó sem Wi-Fi, **quando** o sensor de presença dispara, **então** a luz de caminho acende e o evento fica no buffer.
2. **Dado** o Wi-Fi volta, **quando** o nó reenvia o buffer, **então** os eventos chegam com `bufferizado: true` e a hora original.
3. **Dado** o Motor declara o dia crítico, **quando** o nó recebe `DIA_CRITICO`, **então** o LED âmbar acende.
4. **Dado** o sensor sem presença, **quando** o tempo passa, **então** a luz não acende e nenhum `MOVIMENTO` é publicado.

**Cobre:** P5, P8, P12

**Test Cases:** [TC-F28-01](casos-de-teste.md#tc-f28-01) (positivo), [TC-F28-02](casos-de-teste.md#tc-f28-02) (negativo), [TC-F28-03](casos-de-teste.md#tc-f28-03) (negativo), [TC-F28-04](casos-de-teste.md#tc-f28-04) (positivo), [TC-F28-05](casos-de-teste.md#tc-f28-05) (negativo), [TC-F28-06](casos-de-teste.md#tc-f28-06) (positivo), [TC-F28-07](casos-de-teste.md#tc-f28-07) (negativo)

## F29

**Infraestrutura Linux que se provisiona, se vigia e se recupera**

**Módulo:** Infra

**Descrição:** A VM `corredor-ubs` nasce por script, hospeda a API, o painel, o Mosquitto e o Postgres, mede CPU, memória e disco como serviço do systemd, registra quem rodou cada script e se recupera das falhas que o teste de falha provoca.

**Critérios de aceite:**

1. **Dado** uma VM Ubuntu 24.04 nova, **quando** a equipe roda `sudo bash infra/provisionar.sh`, **então** a VM fica com hostname, IP fixo em `corredor0`, nomes `api.corredor.internal` e `painel.corredor.internal`, serviços e painel no ar.
2. **Dado** a VM já provisionada, **quando** a equipe roda o provisionamento de novo, **então** cada etapa mostra 0 mudança e os serviços não reiniciam.
3. **Dado** o Mosquitto parado, **quando** o teste de falha reenvia 3 eventos duas vezes, **então** o monitor alerta e a linha do tempo mostra cada evento uma vez só.
4. **Dado** a API ou o monitor mortos com `kill -9`, **quando** o systemd percebe, **então** o systemd religa o serviço com outro PID e, no caso da API, `/healthz/ready` volta a responder.
5. **Dado** CPU, memória ou disco acima do limite, **quando** o monitor mede, **então** ele grava `ALERTA` e depois `NORMALIZADO` no `alertas.log`.

**Cobre:** INFRA-broker, INFRA-api, INFRA-monitor, INFRA-carga, INFRA-memoria, INFRA-disco, INFRA-travado, INFRA-RUB-1, INFRA-RUB-2, INFRA-RUB-3, INFRA-RUB-4, INFRA-RUB-5, INFRA-RUB-6

**Test Cases:** [TC-F29-01](casos-de-teste.md#tc-f29-01) (positivo), [TC-F29-02](casos-de-teste.md#tc-f29-02) (positivo), [TC-F29-03](casos-de-teste.md#tc-f29-03) (negativo), [TC-F29-04](casos-de-teste.md#tc-f29-04) (negativo), [TC-F29-05](casos-de-teste.md#tc-f29-05) (positivo), [TC-F29-06](casos-de-teste.md#tc-f29-06) (positivo), [TC-F29-07](casos-de-teste.md#tc-f29-07) (negativo), [TC-F29-08](casos-de-teste.md#tc-f29-08) (negativo), [TC-F29-09](casos-de-teste.md#tc-f29-09) (negativo), [TC-F29-10](casos-de-teste.md#tc-f29-10) (negativo), [TC-F29-11](casos-de-teste.md#tc-f29-11) (negativo), [TC-F29-12](casos-de-teste.md#tc-f29-12) (negativo), [TC-F29-13](casos-de-teste.md#tc-f29-13) (negativo), [TC-F29-14](casos-de-teste.md#tc-f29-14) (positivo), [TC-F29-15](casos-de-teste.md#tc-f29-15) (negativo)
