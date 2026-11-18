"""Features do Corredor Aceso. Fonte de docs/qa/features.md e dos CSV do Azure Boards.

Cada critério é (dado, quando, então), sem ponto final. "cobre" usa os identificadores de
docs/qa/ferramentas/registro.py. Rotas entram entre crases, como na tabela 6.3 do modelo de domínio.
"""

FEATURES = [
    dict(
        id="F01", modulo="Cadastro",
        titulo="Cadastro do idoso pela ACS",
        descricao="A ACS cadastra o idoso da microárea com nome, endereço, perfil da casa, condições e medicamentos. "
                  "O cadastro não pede CPF, RG, CNS nem data de nascimento. Nome e endereço são gravados inteiros e "
                  "levam rótulo de dado pessoal. A casa ainda não tem token: o token só nasce com o consentimento.",
        criterios=[
            ("um idoso com nome, endereço e perfil válidos", "a ACS envia `POST /api/idosos`",
             "a API responde 201 com o `id` do idoso e a casa fica sem token"),
            ("um corpo sem nome ou só com espaços", "a ACS envia `POST /api/idosos`",
             "a API responde 422 `NOME_OBRIGATORIO` e nenhum idoso é gravado"),
            ("um corpo sem endereço", "a ACS envia `POST /api/idosos`",
             "a API responde 422 `ENDERECO_OBRIGATORIO` e nenhum idoso é gravado"),
            ("o mesmo corpo enviado duas vezes", "a ACS repete `POST /api/idosos`",
             "a API cria dois idosos com `id` diferentes, porque o cadastro não tem chave de idempotência"),
            ("um corpo com um campo `cpf` ou `dataDeNascimento`", "a ACS envia `POST /api/idosos`",
             "o campo não existe no contrato e não é gravado"),
        ],
        cobre=["`POST /idosos`", "INV-Idoso-9", "INV-Idoso-10", "IDEM-Cadastro-CadastrarIdoso"],
    ),
    dict(
        id="F02", modulo="Cadastro e Motor",
        titulo="Consentimento na UBS e ativação da casa",
        descricao="O idoso autoriza o monitoramento na UBS, com a ACS presente. O consentimento registra quem colheu, "
                  "a data, a versão do termo e as autorizações de avisar o vizinho e a família. Só então nasce um token "
                  "aleatório de 128 bits, e o Cadastro ativa a casa no Motor. Repetir o consentimento vigente devolve o "
                  "mesmo token. Ao iniciar, o Cadastro reenvia a ativação de toda casa com token.",
        criterios=[
            ("um idoso sem consentimento", "a ACS envia `POST /api/idosos/{id}/consentimento`",
             "a API responde 200 com um `casaToken` de 32 hexadecimais minúsculos e a casa aparece no radar como `Cinza`"),
            ("um idoso com consentimento vigente", "a ACS envia o consentimento de novo",
             "a API devolve o mesmo `casaToken` e a casa continua uma só no Motor"),
            ("um idoso sem consentimento", "a equipe consulta a casa e a lista de nomes",
             "a casa não tem token e a pessoa não aparece em `GET /api/lista-de-nomes`"),
            ("o Motor fora do ar", "a ACS envia o consentimento",
             "a API responde 503 `MOTOR_INDISPONIVEL`, o consentimento fica gravado e repetir a chamada ativa a casa"),
            ("um idoso com token e a casa ausente no Motor", "a API reinicia",
             "a reconciliação ativa a casa no Motor sem gerar token novo"),
            ("dois idosos com o mesmo nome e endereço", "cada um recebe consentimento",
             "os tokens são diferentes, porque o token nunca deriva de nome, endereço ou documento"),
        ],
        cobre=["`POST /idosos/{id}/consentimento`", "INV-Idoso-1", "INV-Idoso-2", "INV-Idoso-3", "INV-Idoso-4",
               "INV-Idoso-5", "NC-6", "P1", "P22", "IDEM-Cadastro-RegistrarConsentimento", "IDEM-Motor-AtivarCasa",
               "INV-CasaMonitorada-1"],
    ),
    dict(
        id="F03", modulo="Cadastro e Motor",
        titulo="Revogação do consentimento",
        descricao="O idoso pode revogar o consentimento. O token sai da casa, o vizinho é removido, os avisos daquele "
                  "token são apagados e o Motor desativa a casa de vez, apagando eventos e confirmações. As visitas "
                  "ficam, porque são a proteção da ACS. Um consentimento novo gera outro token.",
        criterios=[
            ("um idoso com consentimento vigente", "a ACS envia `DELETE /api/idosos/{id}/consentimento`",
             "a API responde 204, a casa some da lista de nomes e do radar e as visitas continuam gravadas"),
            ("uma casa desativada", "chega um pacote ou uma confirmação para o token antigo",
             "o Motor recusa e nada é gravado"),
            ("um idoso já sem consentimento vigente", "a ACS revoga de novo",
             "a API responde 204 e nada muda"),
            ("um idoso revogado", "a ACS registra um consentimento novo",
             "nasce um token diferente do antigo e o token antigo nunca volta"),
        ],
        cobre=["`DELETE /idosos/{id}/consentimento`", "INV-Idoso-8", "P4", "INV-CasaMonitorada-3",
               "IDEM-Cadastro-RevogarConsentimento", "IDEM-Motor-DesativarCasa", "INV-Visita-6"],
    ),
    dict(
        id="F04", modulo="Cadastro",
        titulo="Vizinho de confiança e autorizações",
        descricao="O idoso escolhe um vizinho de confiança, que precisa ter aceitado ser avisado. O vizinho só pode ser "
                  "indicado quando o idoso autorizou avisar o vizinho. A família nunca entra entre os destinatários sem a "
                  "autorização de avisar a família.",
        criterios=[
            ("um idoso com a autorização `avisarVizinho`", "a ACS envia `PUT /api/idosos/{id}/vizinho` com nome, telefone e `aceitouEm`",
             "a API responde 204 e o vizinho aparece na lista de nomes"),
            ("um idoso sem a autorização `avisarVizinho`", "a ACS tenta indicar o vizinho",
             "a API responde 422 `AUTORIZACAO_AUSENTE` e nenhum vizinho é gravado"),
            ("um vizinho já indicado", "a ACS indica outro vizinho",
             "o novo vizinho substitui o antigo"),
            ("um idoso sem a autorização `avisarFamilia`", "o sistema monta os destinatários do aviso",
             "a família não aparece"),
        ],
        cobre=["`PUT /idosos/{id}/vizinho`", "INV-Idoso-6", "INV-Idoso-7", "IDEM-Cadastro-IndicarVizinho"],
    ),
    dict(
        id="F05", modulo="Cadastro e Motor",
        titulo="Perfil da casa",
        descricao="O perfil da casa junta o perfil de contato (`Sozinho`, `Acamado` ou `Coabitado`) e a "
                  "eletrodependência. Trocar o perfil no Cadastro troca o perfil no Motor, e o último valor vale.",
        criterios=[
            ("uma casa ativa `Sozinho` sem eletrodependência", "a ACS envia `PUT /api/idosos/{id}/perfil` com `Acamado` e eletrodependente",
             "a API responde 204 e o radar passa a mostrar o perfil novo"),
            ("o mesmo perfil enviado duas vezes", "a ACS repete a chamada",
             "o perfil fica igual e nada mais muda"),
            ("um idoso sem token", "a ACS troca o perfil",
             "o Cadastro grava o perfil e não chama o Motor"),
        ],
        cobre=["`PUT /idosos/{id}/perfil`", "P21", "IDEM-Motor-DefinirPerfil", "IDEM-Cadastro-IndicarVizinho"],
    ),
    dict(
        id="F06", modulo="Cadastro e Guarda",
        titulo="Lista de nomes para o celular da ACS",
        descricao="O app baixa token, nome, endereço e vizinho das casas ativas da microárea e guarda no SQLite. É o "
                  "único lugar em que o token encontra o nome. A lista passa pela saída segura com o canal do app "
                  "autenticado, que aceita nome, endereço e telefone. Condições e medicamentos não vão.",
        criterios=[
            ("a ACS autenticada na microárea 3", "o app chama `GET /api/lista-de-nomes?microarea=3`",
             "a resposta traz token, nome, endereço e vizinho só das casas com token ativo"),
            ("a lista pedida", "a resposta é montada",
             "nenhum item traz condições, medicamentos nem observação de visita"),
            ("uma chamada sem autenticação", "alguém pede a lista de nomes",
             "a API recusa e nenhum nome sai"),
        ],
        cobre=["`GET /lista-de-nomes?microarea={id}`", "P3", "CANAL-AppAutenticado"],
    ),
    dict(
        id="F07", modulo="Motor",
        titulo="Recebimento dos pacotes do nó",
        descricao="O nó do corredor publica movimento, botão, telemetria e heartbeat por MQTT. O Motor grava um evento "
                  "por chave `(casaToken, seq, ts)`, aceita o `seq` que volta a 1 depois do reinício, recusa `ts` "
                  "adiantado ou velho, descarta token desconhecido sem logar o conteúdo e usa a hora do servidor no "
                  "último pacote.",
        criterios=[
            ("uma casa ativa", "o nó publica um `MOVIMENTO` válido",
             "nasce um evento do sensor e o último pacote avança para a hora de chegada"),
            ("um pacote já recebido", "o mesmo `(casaToken, seq, ts)` chega de novo",
             "continua existindo um evento só e a repetição conta como sucesso"),
            ("um pacote com `ts` 3 minutos à frente da chegada", "o Motor recebe o pacote",
             "o pacote é recusado inteiro e o último pacote não muda"),
            ("um pacote de token desconhecido", "o Motor recebe o pacote",
             "nada é gravado e o log não traz o conteúdo do pacote"),
            ("um `HEARTBEAT`", "o Motor recebe o pacote",
             "o último pacote avança e nenhum evento do sensor é gravado"),
            ("uma telemetria com 85 °C", "o Motor grava o evento",
             "a temperatura fica ausente e o evento é gravado"),
        ],
        cobre=["INV-EventoSensor-1", "INV-EventoSensor-2", "INV-EventoSensor-3", "INV-EventoSensor-4",
               "INV-EventoSensor-5", "P2", "P6", "NC-1", "IDEM-Motor-RegistrarPacote", "INV-CasaMonitorada-2"],
    ),
    dict(
        id="F08", modulo="Motor",
        titulo="Sinal de vida conforme o perfil de contato",
        descricao="O que conta como \"deu sinal hoje\" depende de quem mora na casa. Na casa `Sozinho`, movimento, botão "
                  "e confirmação com pessoa vista contam. Na casa `Acamado` e na casa `Coabitado`, outra pessoa pode "
                  "passar no corredor, então só a confirmação humana com pessoa vista conta.",
        criterios=[
            ("uma casa `Sozinho` em turno aberto", "o nó registra um movimento na janela atual",
             "a casa fica `Verde`"),
            ("uma casa `Coabitado` em turno aberto", "o nó registra movimento e botão",
             "a casa continua `Aguardando`"),
            ("uma casa `Coabitado` em turno aberto", "a ACS registra visita `EstavaBem`",
             "a casa fica `Verde`"),
            ("uma casa `Acamado` em turno aberto", "o nó registra movimento",
             "o movimento não conta e a casa entra vermelha no início da janela"),
        ],
        cobre=["SV-Sozinho", "SV-Acamado", "SV-Coabitado", "NC-5"],
    ),
    dict(
        id="F09", modulo="Motor",
        titulo="Aparelho mudo fica cinza e nunca verde",
        descricao="Se o nó para de mandar pacote, a casa fica `Cinza` com o motivo \"aparelho sem sinal\". A regra do "
                  "heartbeat vem antes de qualquer regra que olhe sinal de vida, e mede a hora em que o pacote chegou ao "
                  "servidor. Uma casa ativada que nunca mandou pacote também é cinza.",
        criterios=[
            ("uma casa com movimento às 09h00 e último pacote às 09h10", "o radar é consultado às 10h00",
             "a casa aparece `Cinza`, pendente, desde 09h10, e nunca `Verde`"),
            ("uma casa ativada que nunca mandou pacote", "o radar é consultado",
             "a casa aparece `Cinza` desde a ativação"),
            ("uma casa cinza", "o nó volta e reenvia o buffer com horas antigas",
             "a casa sai do cinza, porque a hora de chegada mostra o aparelho ligado agora"),
            ("uma casa cinza atendida às 10h30", "a janela muda às 15h00",
             "a casa volta a cinza pendente"),
        ],
        cobre=["R2", "P14", "NC-2", "TV-SIT-2"],
    ),
    dict(
        id="F10", modulo="Motor",
        titulo="Silêncio na janela e saída avisada",
        descricao="No turno do dia crítico, a manhã vai da abertura às 10h e a tarde das 10h às 15h. A casa `Sozinho` "
                  "sem sinal de vida desde o início da janela vencida fica vermelha às 10h ou às 15h. A casa `Acamado` "
                  "fica vermelha no início de cada janela. Um sinal de vida depois do prazo tira o vermelho daquela "
                  "janela. O duplo toque \"hoje vou sair\" suspende esta regra até o fim do turno.",
        criterios=[
            ("uma casa `Sozinho` sem sinal de vida desde a abertura às 05h", "o relógio chega às 10h00",
             "a casa fica `Vermelho`, motivo silêncio, desde 10h00, pendente"),
            ("uma casa `Sozinho` com movimento só às 03h12", "o relógio chega às 10h00",
             "a casa fica `Vermelho`, porque o movimento antes da abertura não vale"),
            ("uma casa vermelha desde 10h00", "chega um movimento às 10h30",
             "às 10h31 a casa fica `Verde`"),
            ("uma casa `Sozinho` com saída avisada às 08h40", "o relógio passa das 10h00 e das 15h00",
             "a casa nunca fica `Vermelho` por silêncio no turno"),
            ("uma casa `Acamado` com turno aberto às 05h", "o radar é consultado às 05h01",
             "a casa fica `Vermelho` desde 05h00"),
        ],
        cobre=["R4", "P9", "TV-SIT-4"],
    ),
    dict(
        id="F11", modulo="Motor",
        titulo="Calor dentro de casa",
        descricao="Com o turno aberto, a casa cujas leituras de temperatura ficam em 34 °C ou mais, sem interrupção, "
                  "por 2 horas (2 minutos na demo) fica vermelha com o motivo calor dentro de casa, mesmo que a pessoa "
                  "esteja se mexendo.",
        criterios=[
            ("uma casa com 35 °C desde as 11h00 e movimento às 13h00", "o radar é consultado às 13h05",
             "a casa aparece `Vermelho`, motivo calor, desde 13h00, pendente"),
            ("uma casa com 35 °C interrompida por uma leitura de 33 °C", "a sequência quente recomeça",
             "o alerta só sai 2 horas depois do recomeço"),
            ("um dia comum sem turno", "a casa passa 3 horas a 36 °C",
             "a regra de calor não devolve alerta e a casa fica `Aguardando`"),
        ],
        cobre=["R3", "TV-SIT-3"],
    ),
    dict(
        id="F12", modulo="Motor",
        titulo="Energia da casa eletrodependente, com trava",
        descricao="A casa com equipamento de saúde na tomada fica vermelha em qualquer dia e a qualquer hora quando a "
                  "tensão cai abaixo de 100 V ou quando fica 60 segundos sem pacote. Depois de uma leitura baixa, a casa "
                  "continua vermelha até a ACS ver a pessoa depois daquela leitura, mesmo que a luz volte. O vizinho "
                  "não resolve o alerta de energia.",
        criterios=[
            ("uma casa eletrodependente em dia comum", "chega uma telemetria com 0 V",
             "a casa fica `Vermelho`, motivo energia, pendente, no topo do radar"),
            ("uma casa eletrodependente", "passam 61 segundos sem pacote",
             "a casa fica `Vermelho`; com 59 segundos, continua `Aguardando`"),
            ("uma casa vermelha por energia", "o vizinho responde `EstaBem`",
             "a casa continua vermelha e pendente"),
            ("a luz voltou com 127 V depois de uma leitura de 0 V", "o radar é consultado",
             "a casa continua vermelha e pendente pela trava"),
            ("a trava armada", "a ACS registra visita `EstavaBem` depois da última leitura baixa",
             "a trava solta e, sem turno, a casa fica `Aguardando`"),
        ],
        cobre=["R1", "NC-3", "TV-SIT-1"],
    ),
    dict(
        id="F13", modulo="Motor",
        titulo="Verde só com prova, Aguardando no resto",
        descricao="Verde quer dizer que existe sinal de vida na janela atual e que o turno está aberto. Sem alerta e "
                  "sem essa prova, a casa fica `Aguardando`: no dia comum, depois do encerramento, na casa `Coabitado` "
                  "sem confirmação e na casa que ainda está no prazo. Depois do encerramento só valem as regras de "
                  "energia e de heartbeat.",
        criterios=[
            ("uma casa `Sozinho` sem sinal de vida com aparelho ligado às 09h00", "o radar é consultado",
             "a casa aparece `Aguardando` e nunca `Verde`"),
            ("uma casa com movimento às 07h00", "o radar é consultado às 12h30",
             "a casa aparece `Aguardando`, porque a tarde pede sinal novo e ainda não venceu"),
            ("um turno encerrado", "o radar é consultado às 18h30",
             "nenhuma casa aparece `Verde` e só energia e heartbeat geram alerta"),
            ("um dia comum", "o radar é consultado",
             "nenhuma casa aparece `Verde`"),
        ],
        cobre=["TV-SIT-5", "TV-SIT-6", "TV-SIT-7", "NC-4", "P13", "TV-SIT-2"],
    ),
    dict(
        id="F14", modulo="Motor",
        titulo="Atendimento por confirmação humana",
        descricao="Uma casa em alerta fica atendida quando existe uma confirmação que a regra aceita, feita depois do "
                  "início do alerta e dentro da janela atual. Cada regra aceita origens e resultados diferentes. A casa "
                  "atendida continua vermelha ou cinza, sai das pendentes e nunca conta como verde.",
        criterios=[
            ("uma casa cinza pendente", "a ACS ou o vizinho confirmam pessoa vista na janela atual",
             "a casa fica cinza e atendida"),
            ("uma casa vermelha por energia", "chega confirmação do vizinho ou ausência confirmada da ACS",
             "a casa continua pendente"),
            ("uma casa vermelha por silêncio", "a ACS confirma ausência",
             "a casa fica vermelha e atendida, só na janela atual"),
            ("uma confirmação com o mesmo `Id`", "ela chega duas vezes",
             "o Motor guarda uma só"),
            ("uma confirmação com hora 5 minutos no futuro ou com mais de 24 horas", "ela chega ao Motor",
             "o Motor recusa"),
        ],
        cobre=["TV-ATD-1", "TV-ATD-2", "TV-ATD-3", "TV-ATD-4", "TV-ATD-5", "TV-ATD-6", "TV-ATD-7", "TV-ATD-8",
               "TV-ATD-9", "INV-CasaMonitorada-4", "INV-CasaMonitorada-5", "IDEM-Motor-Confirmar"],
    ),
    dict(
        id="F15", modulo="Motor",
        titulo="Radar da microárea",
        descricao="O radar mostra à ACS as casas pendentes, as atendidas e as que aguardam, em ordem, com a contagem "
                  "de verdes. Vermelho vem antes de cinza, depois a ordem da regra e depois o alerta mais antigo. O "
                  "radar não traz nome: o app junta o token ao nome no celular.",
        criterios=[
            ("casas em vermelho por energia, vermelho por silêncio e cinza", "o app chama `GET /api/radar?microarea=3`",
             "as pendentes vêm nessa ordem e cada card traz token, situação, motivo, desde, perfil e último pacote"),
            ("uma casa atendida", "o radar é montado",
             "ela aparece em `atendidas` e não soma em `verdes`"),
            ("a ACS da microárea 3", "ela pede o radar da microárea 4",
             "a API recusa"),
        ],
        cobre=["`GET /radar?microarea={id}`"],
    ),
    dict(
        id="F16", modulo="Motor",
        titulo="Linha do tempo da casa",
        descricao="A tela de detalhe mostra os eventos do sensor e as confirmações das últimas 24 horas de uma casa, "
                  "em ordem de ocorrência, com a marca de evento bufferizado. A resposta não tem nome, endereço nem "
                  "texto livre. Casa desconhecida ou desativada responde 404.",
        criterios=[
            ("uma casa ativa com eventos e confirmações", "o app chama `GET /api/casas/{token}/linha-do-tempo`",
             "a resposta traz eventos e confirmações das últimas 24 horas em ordem de `ocorridoEm`"),
            ("um token desativado", "o app pede a linha do tempo",
             "a API responde 404 `CASA_DESCONHECIDA`, igual a um token que nunca existiu"),
            ("qualquer casa", "a linha do tempo é devolvida",
             "nenhum campo traz nome, endereço, telefone ou texto livre"),
        ],
        cobre=["`GET /casas/{token}/linha-do-tempo`", "INV-CasaMonitorada-6"],
    ),
    dict(
        id="F17", modulo="Cadastro e Motor",
        titulo="Visita e desfecho",
        descricao="Na porta, a ACS registra o desfecho: estava bem, encaminhada, estava fora ou não abriu. A visita é "
                  "imutável e sobrevive ao expurgo. Só estava bem e encaminhada são sinal de vida. Estava fora atende só "
                  "o alerta de silêncio. Não abriu não confirma nada, e o app mostra \"SAMU chamado\". Repetir o mesmo "
                  "`id` não duplica.",
        criterios=[
            ("uma casa vermelha por silêncio", "a ACS envia `POST /api/visitas` com `EstavaBem`",
             "a API responde 201 e a casa fica `Verde` na janela atual"),
            ("uma casa vermelha por silêncio", "a ACS registra `NaoAbriu`",
             "a casa continua vermelha e pendente e o Motor não recebe confirmação"),
            ("uma casa vermelha por silêncio", "a ACS registra `EstavaFora`",
             "a casa fica vermelha e atendida e nunca verde"),
            ("uma visita já registrada", "o app reenvia o mesmo `id`",
             "a API responde 200 com a visita que existe e o Motor guarda uma confirmação só"),
            ("um token revogado", "a ACS registra visita",
             "a API responde 422 `TOKEN_INATIVO`"),
            ("uma `ocorridaEm` 5 minutos no futuro", "a ACS registra visita",
             "a API responde 422 `VISITA_NO_FUTURO`"),
        ],
        cobre=["`POST /visitas`", "DESF-EstavaBem", "DESF-Encaminhada", "DESF-EstavaFora", "DESF-NaoAbriu",
               "INV-Visita-1", "INV-Visita-2", "INV-Visita-3", "INV-Visita-4", "INV-Visita-5", "INV-Visita-7",
               "P11", "P18", "NC-7", "IDEM-Cadastro-RegistrarVisita"],
    ),
    dict(
        id="F18", modulo="Motor e Cadastro",
        titulo="Aviso neutro ao vizinho e resposta",
        descricao="Para cada vermelho pendente, e para cada cinza pendente com turno aberto, o Motor manda um pedido de "
                  "contato. Se o idoso autorizou e tem vizinho, o Cadastro registra um aviso por episódio e envia o "
                  "texto neutro, sem nome, endereço nem saúde. O vizinho responde `EstaBem`, que confirma pessoa vista, "
                  "ou `PrecisaDeAjuda`, que não confirma nada. O envio é simulado.",
        criterios=[
            ("uma casa autorizada com vizinho que ficou vermelha às 10h00", "o vigia do radar roda",
             "nasce um aviso e `GET /api/avisos?microarea=3` mostra \"vizinho avisado às 10h00\""),
            ("o mesmo alerta nos ciclos seguintes", "o vigia manda o pedido de novo",
             "continua um aviso só para o episódio"),
            ("um idoso sem a autorização `avisarVizinho`", "a casa fica vermelha",
             "nenhum aviso é criado"),
            ("um aviso aberto", "o vizinho responde `EstaBem` por `POST /api/avisos/{id}/resposta`",
             "a casa recebe confirmação do vizinho com pessoa vista"),
            ("um aviso respondido com `EstaBem`", "chega a resposta `PrecisaDeAjuda`",
             "a API responde 409 `AVISO_JA_RESPONDIDO`"),
        ],
        cobre=["`GET /avisos?microarea={id}`", "`POST /avisos/{id}/resposta`", "INV-AvisoAoVizinho-1",
               "INV-AvisoAoVizinho-2", "INV-AvisoAoVizinho-3", "INV-AvisoAoVizinho-4", "P10", "P19", "P20",
               "IDEM-Cadastro-PedidoDeContato", "IDEM-Cadastro-RegistrarResposta", "CANAL-Vizinho"],
    ),
    dict(
        id="F19", modulo="Motor",
        titulo="Dia crítico e turno",
        descricao="Às 05h, o job de previsão declara o dia crítico com a probabilidade, o motivo e a origem. Isso abre "
                  "o turno e acende o LED âmbar dos nós. Repetir a declaração atualiza a previsão sem mexer na abertura. "
                  "A ACS encerra o turno, e o resumo do turno guarda o indicador de impacto. Às 18h, o turno fecha "
                  "sozinho se ninguém encerrou.",
        criterios=[
            ("hoje é 18/11", "o job envia `PUT /api/dias-criticos/2026-11-18` com probabilidade 0,81",
             "a API responde 200, o turno abre e cada casa ativa recebe o comando `DIA_CRITICO`"),
            ("um turno aberto às 05h00", "o job repete a declaração às 05h01",
             "continua um turno e `abertoEm` continua 05h00"),
            ("uma data diferente de hoje ou probabilidade 1,2", "o job declara",
             "a API responde 422 `DATA_NAO_E_HOJE` ou `PROBABILIDADE_FORA_DA_FAIXA`"),
            ("um turno aberto", "a ACS envia `POST /api/dias-criticos/2026-11-18/encerramento`",
             "a API responde 200 com o resumo de cinco números e repetir devolve o mesmo resumo"),
            ("um turno encerrado", "o job declara de novo",
             "a API responde 409 `TURNO_ENCERRADO` e o turno não reabre"),
        ],
        cobre=["`PUT /dias-criticos/{data}`", "`POST /dias-criticos/{data}/encerramento`", "INV-DiaCritico-1",
               "INV-DiaCritico-2", "INV-DiaCritico-3", "INV-DiaCritico-4", "INV-DiaCritico-5", "INV-DiaCritico-7",
               "P8", "IDEM-Motor-DeclararDiaCritico", "IDEM-Motor-EncerrarTurno"],
    ),
    dict(
        id="F20", modulo="Motor e nó",
        titulo="Pedido de toque às 14h30",
        descricao="Às 14h30 do dia crítico, o Motor toca o buzzer das casas `Sozinho` sem sinal de vida desde as 10h, "
                  "para lembrar a pessoa de dar sinal antes do prazo das 15h. O pedido sai uma vez por dia.",
        criterios=[
            ("uma casa `Sozinho` sem sinal de vida desde as 10h", "o relógio chega às 14h30",
             "a casa recebe um `PEDIR_TOQUE`"),
            ("o toque já pedido hoje", "o vigia roda de novo",
             "nenhum segundo `PEDIR_TOQUE` sai no dia"),
            ("uma casa `Sozinho` com movimento às 11h00", "o relógio chega às 14h30",
             "a casa não recebe `PEDIR_TOQUE`"),
        ],
        cobre=["P12", "INV-DiaCritico-6", "IDEM-Motor-PedirToque"],
    ),
    dict(
        id="F21", modulo="Motor",
        titulo="Relógio de demo",
        descricao="Para exercitar as janelas das 10h e das 15h sem esperar, a equipe move a hora do Motor por "
                  "`PUT /api/demo/relogio`. O mesmo deslocamento vale para o `ts` dos pacotes e para a hora das visitas. "
                  "A rota só existe no ambiente `Development`.",
        criterios=[
            ("a API em `Development`", "a equipe envia `PUT /api/demo/relogio` com 09h58",
             "a API responde 200 com o deslocamento e o radar passa a calcular com 09h58"),
            ("o relógio movido", "o nó publica um pacote com a hora real",
             "o pacote cai na hora deslocada e não é recusado como adiantado"),
            ("a API em `Production`", "alguém chama `PUT /api/demo/relogio`",
             "a rota não existe e a hora não muda"),
        ],
        cobre=["`PUT /demo/relogio`", "NC-10"],
    ),
    dict(
        id="F22", modulo="Motor",
        titulo="Retenção de 24 horas",
        descricao="De hora em hora, o expurgo apaga eventos do sensor e confirmações com mais de 24 horas. Ele guarda a "
                  "leitura de tensão baixa da casa ativa e eletrodependente enquanto a ACS não viu a pessoa depois dela, "
                  "porque a trava depende dessa leitura. As visitas nunca são apagadas.",
        criterios=[
            ("um movimento com 25 horas", "o expurgo roda",
             "o evento some e a linha do tempo não o mostra"),
            ("uma leitura baixa com 25 horas sem confirmação da ACS depois", "o expurgo roda",
             "a leitura fica e a casa continua vermelha"),
            ("o expurgo já rodou", "ele roda de novo",
             "nada muda"),
            ("uma visita com 3 dias", "o expurgo roda",
             "a visita continua no Cadastro"),
        ],
        cobre=["P7", "IDEM-Motor-Expurgo", "INV-Visita-6"],
    ),
    dict(
        id="F23", modulo="Guarda",
        titulo="Mascaramento de PII na entrada",
        descricao="Toda requisição passa pelo interceptador da Guarda. CPF, RG, CNS e credencial são mascarados em "
                  "qualquer campo antes do log e do banco, e cada mascaramento vira um evento de segurança sem o valor. "
                  "Nome, endereço, saúde e mora sozinho não mudam o corpo: viram rótulo do texto.",
        criterios=[
            ("uma observação com `123.456.789-00`", "a ACS envia a requisição",
             "o banco e o log guardam `***.456.***-00` e o painel ganha um mascaramento no canal `Banco`"),
            ("um corpo com a chave `senha`", "a requisição chega",
             "o valor é mascarado e o evento registra o tipo credencial"),
            ("um corpo com `casaToken` de 32 hexadecimais", "a requisição chega",
             "o token não é mascarado e a visita segue"),
            ("um mascaramento registrado", "a equipe lê o evento no banco",
             "o evento tem tipo de dado, canal, módulo e rota modelo, sem o valor, sem trecho do texto e sem a ACS"),
        ],
        cobre=["P15", "CANAL-Banco", "INV-EventoDeSeguranca-1", "INV-EventoDeSeguranca-2", "INV-EventoDeSeguranca-4",
               "IDEM-Guarda-Registrar"],
    ),
    dict(
        id="F24", modulo="Guarda",
        titulo="Bloqueio de PII nas saídas",
        descricao="Um texto com rótulo de dado pessoal só sai pela saída segura, que consulta a política de cada canal. "
                  "Log, resposta sem autenticação, mensagem de erro, MQTT e prompt do LLM não aceitam rótulo. Cada "
                  "bloqueio grava um evento de segurança que a aplicação não altera nem apaga.",
        criterios=[
            ("o código tenta gravar `Idoso.Nome` no log", "a linha de log é escrita",
             "o log recebe `[NOME]` e o painel ganha um bloqueio no canal `Log`"),
            ("uma rota sem autenticação que devolveria `Idoso.Condicoes`", "a rota responde",
             "a resposta sai bloqueada"),
            ("uma exceção cuja mensagem leva um nome", "o erro vai para a tela do app",
             "a mensagem é trocada por texto neutro"),
            ("um texto sem rótulo", "ele passa pela saída segura no canal `Log`",
             "o texto sai inteiro e nenhum evento é gravado"),
        ],
        cobre=["CANAL-Log", "CANAL-RespostaSemAutenticacao", "CANAL-MensagemDeErro", "CANAL-Mqtt",
               "CANAL-PromptDoLlm", "NC-9", "INV-EventoDeSeguranca-5"],
    ),
    dict(
        id="F25", modulo="Cadastro e Guarda",
        titulo="Mensagem para o grupo do bairro",
        descricao="Antes de pedir ajuda no grupo do bairro, a ACS verifica a mensagem no app. Qualquer rótulo de dado "
                  "pessoal bloqueia o envio: a resposta explica em duas frases o que vazaria e oferece uma versão segura "
                  "para copiar. O sistema não envia nada ao grupo e nunca grava o texto original.",
        criterios=[
            ("o texto \"ele mora sozinho e toma diurético\"", "a ACS envia `POST /api/mensagens/verificar` com o canal `GrupoDoBairro`",
             "a API responde 200 com veredito `Bloqueada`, tipos `MoraSozinho` e `Saude`, a frase fixa e a versão segura"),
            ("uma mensagem bloqueada", "a equipe lê a tabela de mensagens",
             "só existe o texto mascarado"),
            ("a versão segura", "a ACS verifica esse texto",
             "o veredito é `Liberada` e a explicação sai nula"),
            ("uma mensagem já verificada", "a ACS verifica o mesmo texto de novo",
             "nasce outra mensagem com o próprio veredito, sem mudar a primeira"),
        ],
        cobre=["`POST /mensagens/verificar`", "INV-MensagemSaida-1", "INV-MensagemSaida-2", "INV-MensagemSaida-3",
               "INV-MensagemSaida-4", "INV-MensagemSaida-5", "P16", "NC-8", "CANAL-GrupoDoBairro",
               "IDEM-Cadastro-CadastrarIdoso"],
    ),
    dict(
        id="F26", modulo="Guarda",
        titulo="Tradução por LLM e painel Risco Cidadão",
        descricao="Cada bloqueio é traduzido por um LLM numa mensagem de duas frases para um cidadão leigo. O prompt "
                  "leva só o tipo de dado, o canal e o módulo. Sem resposta em 5 segundos, entra a frase fixa. O painel "
                  "aberto mostra o farol das últimas 24 horas e a mensagem de cada evento, sem nenhum valor pessoal.",
        criterios=[
            ("uma saída bloqueada de severidade 4 nas últimas 24 horas", "o cidadão abre `/painel`",
             "o farol aparece vermelho com a mensagem traduzida do evento"),
            ("só mascaramentos na entrada nas últimas 24 horas", "o cidadão abre o painel",
             "o farol aparece amarelo"),
            ("nenhum evento nas últimas 24 horas", "o cidadão abre o painel",
             "o farol aparece verde"),
            ("o LLM fora do ar", "uma saída é bloqueada",
             "a mensagem ao cidadão é a frase fixa do tipo de evento"),
            ("qualquer evento", "`GET /api/painel` responde sem autenticação",
             "nenhum campo traz o valor de um dado pessoal"),
        ],
        cobre=["`GET /painel`", "P17", "FAROL-Vermelho", "FAROL-Amarelo", "FAROL-Verde", "INV-EventoDeSeguranca-3"],
    ),
    dict(
        id="F27", modulo="App",
        titulo="App da ACS sem rede: fila de desfechos e radar guardado",
        descricao="A ACS trabalha na rua, com sinal ruim. O app grava o desfecho no SQLite antes de qualquer chamada e "
                  "envia a fila uma visita por vez, com o mesmo `id` em cada tentativa. O radar e a lista de nomes "
                  "ficam guardados e são trocados inteiros. Um radar velho se declara velho, e um card sem nome nunca "
                  "some.",
        criterios=[
            ("o celular sem rede", "a ACS registra um desfecho",
             "o desfecho fica `pendente` no celular e sobe com o mesmo `id` quando a rede volta"),
            ("o envio recebe 422 `TOKEN_INATIVO`", "o app trata a resposta",
             "o desfecho fica `recusado`, o card mostra \"Não enviado\" com o motivo e o app não tenta de novo"),
            ("o radar baixado há mais de 2 minutos", "a ACS abre o radar sem rede",
             "o título mostra \"sem conexão\" com a hora do radar"),
            ("um card cujo token não está na lista de nomes", "o radar é desenhado",
             "o card aparece com \"Casa sem nome. Atualize a lista.\""),
            ("uma resposta de erro ao baixar a lista", "o app trata a resposta",
             "a lista guardada continua igual"),
        ],
        cobre=["APP-CACHE-1", "APP-CACHE-2", "APP-CACHE-3", "APP-CACHE-4", "APP-CACHE-5", "APP-CACHE-6",
               "APP-FILA-1", "APP-FILA-2", "APP-FILA-3", "APP-FILA-4"],
    ),
    dict(
        id="F28", modulo="Nó",
        titulo="Nó do corredor no Wokwi",
        descricao="O ESP32 do corredor acende a luz de caminho quando a idosa passa, sem depender da rede, e publica o "
                  "movimento. Ele acende o LED âmbar no dia crítico e toca o buzzer no pedido de toque. Sem Wi-Fi, ele "
                  "guarda os eventos e reenvia com a hora original quando a rede volta.",
        criterios=[
            ("o nó sem Wi-Fi", "o sensor de presença dispara",
             "a luz de caminho acende e o evento fica no buffer"),
            ("o Wi-Fi volta", "o nó reenvia o buffer",
             "os eventos chegam com `bufferizado: true` e a hora original"),
            ("o Motor declara o dia crítico", "o nó recebe `DIA_CRITICO`",
             "o LED âmbar acende"),
            ("o sensor sem presença", "o tempo passa",
             "a luz não acende e nenhum `MOVIMENTO` é publicado"),
        ],
        cobre=["P5", "P8", "P12"],
    ),
    dict(
        id="F29", modulo="Infra",
        titulo="Infraestrutura Linux que se provisiona, se vigia e se recupera",
        descricao="A VM `corredor-ubs` nasce por script, hospeda a API, o painel, o Mosquitto e o Postgres, mede CPU, "
                  "memória e disco como serviço do systemd, registra quem rodou cada script e se recupera das falhas "
                  "que o teste de falha provoca.",
        criterios=[
            ("uma VM Ubuntu 24.04 nova", "a equipe roda `sudo bash infra/provisionar.sh`",
             "a VM fica com hostname, IP fixo em `corredor0`, nomes `api.corredor.internal` e `painel.corredor.internal`, serviços e painel no ar"),
            ("a VM já provisionada", "a equipe roda o provisionamento de novo",
             "cada etapa mostra 0 mudança e os serviços não reiniciam"),
            ("o Mosquitto parado", "o teste de falha reenvia 3 eventos duas vezes",
             "o monitor alerta e a linha do tempo mostra cada evento uma vez só"),
            ("a API ou o monitor mortos com `kill -9`", "o systemd percebe",
             "o systemd religa o serviço com outro PID e, no caso da API, `/healthz/ready` volta a responder"),
            ("CPU, memória ou disco acima do limite", "o monitor mede",
             "ele grava `ALERTA` e depois `NORMALIZADO` no `alertas.log`"),
        ],
        cobre=["INFRA-broker", "INFRA-api", "INFRA-monitor", "INFRA-carga", "INFRA-memoria", "INFRA-disco",
               "INFRA-travado", "INFRA-RUB-1", "INFRA-RUB-2", "INFRA-RUB-3", "INFRA-RUB-4", "INFRA-RUB-5",
               "INFRA-RUB-6"],
    ),
]
