# Event storming do Corredor Aceso

Este documento registra as três sessões de event storming do Corredor Aceso. A sessão 1 dá a visão geral e fecha os hotspots. A sessão 2 percorre cada fluxo no nível de processo. A sessão 3 decide os agregados. Os nomes de código seguem o glossário de [03-modelo-de-dominio.md](03-modelo-de-dominio.md).

## Como as sessões foram feitas

Uma pessoa escreveu as três sessões a partir da primeira versão da arquitetura, `docs/arquitetura.md`, hoje substituída por [01-visao-geral.md](01-visao-geral.md), e de [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md). As falas da ACS, da idosa e do vizinho vêm das personas simuladas da descoberta. Nenhuma ACS real participou. As perguntas do fim da sessão 1 só uma pessoa real responde.

Os exemplos usam o dia crítico de 18/11/2026, com o turno aberto às 05h. Os horários são de São Paulo. As casas de A a G são fictícias. A casa A é a do nó do Wokwi.

| Marca | Significado |
|---|---|
| E | Evento de domínio. Um fato, escrito no passado. |
| [g] | Evento gravado. Existe uma linha no banco que mostra o fato. |
| [d] | Evento derivado. Ninguém grava. O Motor conclui o fato quando avalia a casa. |
| C | Comando. Um pedido que pode ser recusado. |
| A | Ator. Uma pessoa. |
| S | Sistema externo, ou o relógio. |
| P | Política. Tem a forma "quando X acontece, então faça Y". |
| L | Modelo de leitura. O que alguém vê antes de decidir. |
| H | Hotspot. Um ponto em que as fontes se contradizem ou em que falta uma regra. |
| auditoria | Linha no log de auditoria da VM. Não é fato do domínio. |

Os eventos do nó, como `MovimentoDetectado`, `BotaoBemPressionado`, `SaidaAvisada`, `TelemetriaRecebida` e `EventoReenviado`, viram no código um `EventoSensorRegistrado` com o tipo do pacote. O `HeartbeatRecebido` só avança `UltimoPacoteEm`.

A marca [d] existe porque o produto vigia o silêncio. O silêncio é a falta de um evento. Nenhum evento chega para avisar que a casa ficou vermelha, então `CasaFicouVermelha` não tem linha no banco. O Motor chega a essa conclusão toda vez que avalia a casa.

## Sessão 1: visão geral

### Atores e sistemas externos

| Tipo | Nome | Papel |
|---|---|---|
| A | Idosa | Mora na casa. Passa no corredor, aperta o botão, dá e revoga o consentimento. Não usa app. |
| A | ACS | Cadastra, registra o consentimento, abre o radar, visita, registra o desfecho, encerra o turno e escreve no grupo do bairro. |
| A | Vizinho de confiança | Recebe o aviso neutro e responde se viu a pessoa. |
| A | Técnico | Instala o nó e grava o token nele. Na demo, é a própria equipe. |
| A | Cidadão | Lê o painel Risco Cidadão. |
| S | Nó | ESP32 no Wokwi e simulador das 39 casas. Publica pacotes e recebe comandos por MQTT. Acende a luz sem depender da rede. |
| S | Job de previsão | Roda às 05h, decide se o dia é crítico e chama a API. |
| S | Relógio | Faz as janelas vencerem às 10h e às 15h, pede o toque às 14h30 e roda o `VigiaDoRadar` e o `Expurgo`. |
| S | App da ACS | Único lugar em que token e nome se encontram. |
| S | LLM | Recebe o tipo de dado, o canal e o módulo. Devolve duas frases. |
| S | Canal do vizinho | Simulado no protótipo. |

### Linha do tempo

A coluna "Onde" usa C para Cadastro, M para Motor, G para Guarda e N para o nó.

| Fase | Evento | Marca | Onde | Quem causa |
|---|---|---|---|---|
| Antes do primeiro dia | `IdosoCadastrado` | [g] | C | ACS |
| | `ConsentimentoRegistrado`, com o token gerado | [g] | C | ACS, com a idosa presente na UBS |
| | `CasaAtivada`, com microárea e perfil | [g] | M | política P1 |
| | `VizinhoDeConfiancaIndicado` | [g] | C | ACS |
| | `PrimeiroPacoteRecebido` | [d] | M | nó instalado |
| | `ListaDeNomesConsultada` | auditoria | C | app |
| Toda noite e todo dia | `MovimentoDetectado` | [g] | M | idosa passa no corredor |
| | `LuzDeCaminhoAcesa` | só no nó | N | decisão local do nó, sem rede |
| | `HeartbeatRecebido` | sem linha, avança `UltimoPacoteEm` | M | nó, a cada 20 segundos |
| | `TelemetriaRecebida` | [g] | M | nó, a cada 5 minutos e quando a tensão cruza 100 V |
| | `EventoReenviado`, com a hora original | [g] | M | nó, depois de uma queda do broker |
| | `EventosExpurgados`, depois de 24 horas | apaga linhas | M | política P7 |
| Dia crítico | `DiaCriticoDeclarado` | [g] | M | job de previsão, às 05h |
| | `TurnoAberto` | [d] | M | consequência do dia crítico |
| | `ComandoDiaCriticoPublicado` | sem linha | M | política P8 |
| | `BotaoBemPressionado`, `SaidaAvisada` | [g] | M | idosa |
| | `RadarConsultado` | auditoria | M | ACS |
| | `JanelaEncerrada`, às 10h e às 15h | [d] | M | relógio |
| | `CasaFicouVerde`, `CasaFicouAguardando`, `CasaFicouVermelha`, `CasaFicouCinza` | [d] | M | avaliação |
| | `PedidoDeContatoEnviado` | sem linha | M | política P10 |
| | `AvisoAoVizinhoRegistrado` | [g] | C | política P19 |
| | `VizinhoRespondeu` | [g] | C | vizinho |
| | `ConfirmacaoRegistrada` | [g] | M | políticas P11 e P20 |
| | `VisitaRegistrada` | [g] | C | ACS |
| | `ToquePedido`, às 14h30 | [g] | M | política P12 |
| | `TurnoEncerrado`, com o resumo | [g] | M | ACS, ou o `VigiaDoRadar` às 18h |
| A qualquer hora | `EnergiaCaiu`, na casa do eletrodependente | [d] | M | tensão baixa ou silêncio de 60 segundos |
| | `PiiMascaradaNaEntrada` | [g] | G | `InterceptadorDeEntrada` |
| | `MensagemLiberada`, `MensagemBloqueada` | [g] | C | ACS pede a verificação |
| | `SaidaBloqueada` | [g] | G | `SaidaSegura` |
| | `BloqueioTraduzido` | [g] | G | LLM ou frase fixa |
| | `FarolMudou` | [d] | G | painel |
| Saída do programa | `ConsentimentoRevogado` | [g] | C | idosa, por meio da ACS |
| | `CasaDesativada` | [g] | M | política P4 |
| | `EventosDaCasaApagados` | apaga linhas | M | política P4 |

### Políticas

As sessões 2 e 3 citam as políticas por número.

| # | Política |
|---|---|
| P1 | Quando `ConsentimentoRegistrado`, o Cadastro chama `AtivarCasaAsync` com token, microárea e perfil. |
| P2 | Quando chega um pacote de token desconhecido ou inativo, o Motor descarta sem gravar o payload no log. |
| P3 | Quando o app pede a lista de nomes, a lista passa pela `SaidaSegura` com o canal `AppAutenticado`. |
| P4 | Quando `ConsentimentoRevogado`, o Cadastro chama `DesativarCasaAsync`. O Motor apaga os eventos e as confirmações da casa. |
| P5 | Quando a idosa passa no corredor, o nó acende a luz sem depender da rede. |
| P6 | Quando chega um `(casaToken, seq, ts)` repetido, o Motor ignora. |
| P7 | De hora em hora, o `Expurgo` apaga eventos do sensor e confirmações com mais de 24 horas. Ele não apaga leitura baixa de casa ativa e eletrodependente enquanto não existe confirmação da ACS com `PessoaVista` depois dela. Ele apaga os eventos antes das confirmações. |
| P8 | Quando `DiaCriticoDeclarado`, o Motor publica `DIA_CRITICO` para toda casa ativa. |
| P9 | Quando `SaidaAvisada`, a regra de silêncio fica suspensa para a casa até o fim do turno. |
| P10 | A cada ciclo, o `VigiaDoRadar` manda um `PedidoDeContato` pela porta `IReceptorDeContato` para cada vermelho pendente, e para cada cinza pendente com turno aberto. |
| P11 | Quando `VisitaRegistrada`, o Cadastro consulta o `TradutorDeDesfecho` e chama `ConfirmarAsync` quando a tabela manda. |
| P12 | Às 14h30, o Motor publica `PEDIR_TOQUE` para cada casa `Sozinho` sem sinal de vida desde as 10h. Uma vez por dia. |
| P13 | Depois do encerramento, só valem as regras de energia e de heartbeat. Às 18h, o `VigiaDoRadar` encerra o turno se ninguém encerrou. |
| P14 | A regra de heartbeat vem antes de qualquer regra que olhe sinal de vida. |
| P15 | Quando chega uma requisição, o `InterceptadorDeEntrada` mascara CPF, RG, CNS e credencial em qualquer campo e rotula os outros tipos de dado. |
| P16 | Quando um texto tenta sair pelo canal `GrupoDoBairro`, a `SaidaSegura` bloqueia qualquer rótulo. |
| P17 | Quando `SaidaBloqueada`, o `Tradutor` pede a mensagem ao cidadão ao LLM com tipo de dado, canal e módulo. Se o LLM falha, entra a frase fixa. |
| P18 | Quando chega uma visita com `id` repetido, o Cadastro devolve a que existe e chama o Motor de novo com a mesma confirmação. |
| P19 | Quando o `ReceptorDeContato` recebe um pedido de contato com episódio novo e o idoso autorizou, o Cadastro registra o `AvisoAoVizinho` e envia o texto neutro. |
| P20 | Quando `VizinhoRespondeu` com `EstaBem`, o Cadastro chama `ConfirmarAsync` com `Vizinho` e `PessoaVista`. |
| P21 | Quando `PerfilDaCasaAlterado` com token ativo, o Cadastro chama `DefinirPerfilAsync`. |
| P22 | Quando o Cadastro inicia, ele reenvia `AtivarCasaAsync` para todo idoso com token. |

### Hotspots e decisões

Cada hotspot tem a decisão que vale para 19/11. A coluna "Confirmar com" diz quem pode mudar a decisão.

#### Situação da casa e radar

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H1 | Às 06h de um dia crítico, uma casa ainda não deu sinal de vida. Com três situações, ela seria verde sem ninguém saber dela, ou vermelha antes do prazo. | Entra a quarta situação, `Aguardando`: sem alerta e sem sinal de vida na janela atual. `Verde` exige sinal de vida na janela atual e só existe com o turno aberto. | equipe |
| H2 | Às 10h a janela da tarde começa e pede um sinal de vida novo. A casa que deu sinal de vida às 09h50 deixa de ser verde. | Ela vai para `Aguardando`, nunca para `Vermelho`. A contagem de verdes recomeça a cada janela. O app mostra a lista de `Aguardando` abaixo das pendentes. | ACS real |
| H3 | Casa recém-ativada, ou com os eventos expurgados, sem nenhum pacote. | `Cinza`. Vira caso de teste. | equipe |
| H4 | O eletrodependente mudo fica vermelho pela regra de energia ou cinza, como diz o objetivo do protótipo? | Vermelho. No apagão, o roteador da casa também cai. O pitch mostra o cinza em outra casa. | equipe |
| H5 | O Motor não distingue apagão de queda de Wi-Fi. | Os dois dão `Vermelho` ao eletrodependente e `Cinza` às outras casas. Agrupar casas mudas por rua exige endereço e fica fora. | equipe |
| H6 | Cinza fica acima ou abaixo de vermelho no radar? | Vermelho primeiro, depois cinza, depois a ordem da regra, depois o `Desde` mais antigo. | ACS real |
| H7 | Na casa coabitada, outro morador passa no corredor e aperta o botão. Contar esses eventos daria verde sem saber da idosa. | Na casa `Coabitado`, só confirmação com `PessoaVista` é sinal de vida. A regra de silêncio não vale para ela. | equipe |
| H8 | A pessoa acamada não passa no corredor. Quem passa é outra pessoa. | No perfil `Acamado`, só confirmação com `PessoaVista` é sinal de vida. A janela dela vence no início, então ela fica vermelha desde o começo de cada janela sem confirmação. | ACS real |

#### Sinais de vida e pacotes

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H9 | O nó reenvia eventos com a hora original. O silêncio do aparelho precisa da hora de chegada. A janela precisa da hora do fato. | `EventoSensor` guarda `OcorridoEm`, que vem do `ts`, e `RecebidoEm`. O silêncio usa `UltimoPacoteEm`, que é hora do servidor. O sinal de vida usa `OcorridoEm`. | equipe |
| H10 | Um nó com o relógio adiantado manda um movimento com hora de 23h e cobre o dia inteiro. | O Motor recusa pacote com `ts` mais de 2 minutos à frente da chegada, ou com mais de 24 horas. Pacote recusado não avança `UltimoPacoteEm`, então um nó com o relógio quebrado deixa a casa cinza. | equipe do nó |
| H11 | O `seq` volta a 1 quando o ESP32 reinicia. A chave `(casaToken, seq)` descartaria os eventos novos. | Chave `(casaToken, seq, ts)`. O firmware não muda. | equipe do nó |
| H12 | O heartbeat a cada 20 segundos daria cerca de 170 mil linhas por dia com 40 casas. | O `HEARTBEAT` só avança `CasaMonitorada.UltimoPacoteEm`. Ele não vira `EventoSensor`. | equipe |
| H13 | O contrato MQTT não diz o intervalo do heartbeat. O critério de teste pede alerta do eletrodependente em menos de 1 minuto. | Heartbeat a cada 20 segundos. Eletrodependente com 60 segundos sem pacote fica `Vermelho`. O critério vira "vermelho no radar 60 segundos depois do último pacote". | equipe do nó, equipe do Boards |
| H14 | Como o token chega ao nó? | Na demo, constante no firmware e no simulador. | equipe do nó |
| H15 | O tópico com o token passa por um broker público. Quem assina `corredor/v1/#` lê o token e pode publicar um `MOVIMENTO` falso. | Fora do prazo. Token de 128 bits e pacote de token desconhecido descartado. O vídeo e o PDF de Cyber dizem a limitação. A correção é assinar o pacote com um segredo por casa. | professor de Cyber |
| H16 | `ts` vem em UTC e as janelas são de São Paulo. | O banco guarda UTC. O fuso `America/Sao_Paulo` aparece só no corte das janelas. | equipe |

#### Confirmações e visitas

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H17 | A arquitetura diz que `POST /visitas` confirma a casa no Motor. Com "não abriu, chamei SAMU", a casa ficaria verde. | `NaoAbriu` não chama o Motor. A casa continua vermelha e pendente. | ACS real |
| H18 | "Estava fora" não mostra que a pessoa está bem. | `EstavaFora` vira `AusenciaConfirmada`. Nunca é sinal de vida. Ela atende só o alerta de silêncio, e só na janela atual. | ACS real |
| H19 | Por quanto tempo vale uma confirmação humana? | Só na janela atual. Na janela seguinte, a casa volta se a condição continua. Vale para silêncio, cinza, calor, energia e acamado. Uma visita não cobre o turno. | ACS real |
| H20 | Uma casa cinza visitada continua com o nó mudo. Ela não pode virar verde nem ficar no topo das pendentes. | Ela fica `Cinza` e atendida. Sai das pendentes e entra nas atendidas, nunca na contagem de verdes. Só um pacote novo tira o cinza. | equipe |
| H21 | O vizinho pode resolver um alerta de energia? | Não. Cada regra lista as origens que aceita. A regra de energia aceita só a ACS. | equipe |
| H22 | A energia volta. O alerta some sozinho? | Não. Depois de uma leitura de tensão baixa, a casa fica vermelha até uma confirmação da ACS posterior à leitura. | equipe |
| H23 | O expurgo de 24 horas apagaria a leitura baixa e soltaria a trava sem ninguém olhar a pessoa. | O `Expurgo` não apaga leitura baixa de casa ativa e eletrodependente enquanto não existe confirmação da ACS com `PessoaVista` depois dela. A leitura baixa de outra casa sai em 24 horas. | equipe |
| H24 | A ACS registra a visita sem rede e o app sobe depois. Qual hora vale? | A hora da visita, `ocorridaEm`. O `id` gerado no celular impede a visita duplicada. | equipe |
| H25 | Depois da revogação, o que acontece com a visita? | A visita fica. A base legal é confirmada com o professor de SOA. O código de 19/11 não trata esse caso. | professor de SOA |

#### Aviso ao vizinho

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H26 | O diagrama liga o Motor direto ao vizinho, mas o Motor não tem o telefone. | O aviso mora no Cadastro. O Motor declara a porta `IReceptorDeContato` em `Corredor.Contratos`, e o Cadastro implementa. | equipe |
| H27 | A situação é calculada. Quem percebe que a casa acabou de ficar vermelha? | O `VigiaDoRadar` avalia as casas a cada 15 segundos e manda um `PedidoDeContato` por alerta pendente. O Cadastro descarta a repetição pela chave `(casaToken, motivo, desde)`. O Motor não guarda alerta. | equipe |
| H28 | Quem age às 02h40, quando a casa do eletrodependente fica vermelha? | O vizinho, pelo aviso. No protótipo, o canal é simulado e o vídeo diz isso. | ACS real |
| H29 | O telefone do vizinho é dado pessoal do vizinho. | O vizinho só entra no cadastro com a data em que aceitou. | equipe |
| H30 | Aparelho mudo às 03h de um dia comum chama o vizinho? | Não. Fora do turno, o cinza é tarefa de manutenção. O pedido de contato sai para todo vermelho pendente e para o cinza pendente com turno aberto. | ACS real |

#### Dia crítico e turno

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H31 | Quem encerra o turno? O que acontece com a casa pendente? | A ACS encerra, sem bloqueio. O resumo grava as pendentes. Sem encerramento, o `VigiaDoRadar` encerra às 18h. Existe um encerramento por data. | equipe |
| H32 | O turno é da cidade ou da microárea? | Um turno por dia crítico. A entrega tem uma microárea. Com mais de uma, o encerramento de uma ACS desligaria as regras de silêncio e de calor das outras, e o encerramento passaria a ser por microárea. | equipe |
| H33 | A ida ao banheiro às 03h12 conta para a manhã do dia crítico? | Não. A janela da manhã começa na abertura do turno. | equipe |
| H34 | O `PEDIR_TOQUE` sai às 15h, a mesma hora em que a janela fecha. | Ele sai às 14h30, para as casas `Sozinho` sem sinal de vida desde as 10h, uma vez por dia. | equipe |
| H35 | A janela das 10h, a das 15h e os 30 minutos de silêncio não cabem num vídeo. | Todo número das regras fica em `ParametrosDoMotor`, com valor de produção e de demo. O `RelogioDeDemo` desloca a hora do Motor. | equipe |
| H36 | Com o relógio de demo deslocado, o `ts` do nó do Wokwi continua na hora real e seria recusado. | O `TradutorDePacote` soma o mesmo deslocamento ao `ts` de cada pacote. O `POST /visitas` soma o mesmo deslocamento a `ocorridaEm`. | equipe do nó |
| H37 | O job de ML roda na demo com uma data histórica. Um dia crítico com data passada nunca tem turno aberto. | `DiaCritico` só aceita a data de hoje. O script usa as variáveis da data histórica e envia a data de hoje. | equipe do ML |
| H38 | O comando `DIA_CRITICO` publicado como retido acenderia o âmbar no dia seguinte. | O comando não é retido. O contrato não tem comando para apagar o âmbar, e o firmware não muda. | equipe do nó |

#### Dados pessoais e Guarda

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H39 | A arquitetura diz que o interceptador troca a PII pela máscara. Isso apagaria nome e endereço do `POST /idosos`, e a `SaidaSegura` não teria o que bloquear no texto da mensagem. | O `InterceptadorDeEntrada` mascara CPF, RG, CNS e credencial em qualquer campo. Os outros tipos de dado só ganham rótulo. Nome e endereço são gravados inteiros. | professor de Cyber |
| H40 | Gravar a mensagem bloqueada grava o vazamento. | `MensagemSaida` guarda só o texto mascarado. | professor de Cyber |
| H41 | Um CPF mascarado na entrada deixaria o farol vermelho. | Só uma saída bloqueada deixa o farol vermelho. O mascaramento na entrada vai até amarelo. | professor de Cyber |
| H42 | A versão segura "quem estiver perto da Rua X" ainda localiza a idosa. | Texto fixo, sem rua: "Preciso de ajuda para uma visita agora. Quem puder, me chame no privado." | equipe |
| H43 | "Acamado" e "eletrodependente" são dado de saúde ligado a um token. | O Motor guarda dado pseudonimizado, não anônimo. O pitch e o PDF de Cyber dizem isso. | professor de Cyber |
| H44 | A Guarda não tem onde gravar os eventos do painel. | Um terceiro schema, `guarda`, com uma tabela. | equipe |
| H45 | O Cadastro grava e depois chama o Motor, sem transação comum. | Os quatro métodos de `IMotorDeCasas` convergem quando repetidos. Ao iniciar, o Cadastro reenvia `AtivarCasaAsync` para todo idoso com token. | equipe |
| H46 | O radar filtra por microárea, mas a interface da arquitetura não leva a microárea ao Motor. | `AtivarCasaAsync` leva `MicroareaId`. Microárea não é dado pessoal. | equipe |

#### Fora da entrega

| # | Hotspot | Decisão | Confirmar com |
|---|---|---|---|
| H47 | Um PIR só não distingue ida de volta, e a regra `IdaNoturnaSemVolta` depende disso. | Primeira regra cortada. | equipe |
| H48 | Casa com dois idosos. | Fora. Um idoso por casa. | ACS real |
| H49 | Consentimento de idosa com declínio cognitivo. | Responsável legal fica fora. | professor de SOA |

### Perguntas que só uma pessoa real responde

- A ACS aceita não ligar para as casas verdes? O produto inteiro depende disso. Se ela não aceitar, o radar passa a mostrar os verdes no fim da lista, e o modelo não muda.
- A ACS quer ver a casa em `Aguardando` como card ou só como número?
- A ACS aceita voltar à tarde na casa do acamado que ela viu de manhã?
- O vizinho aceita receber o aviso às 02h40, na madrugada de um apagão?
- Os professores de SOA e de C# aceitam o mesmo repositório?

## Sessão 2: nível de processo

Cada tabela lê da esquerda para a direita: um ator dá um comando, um agregado decide, um evento registra, uma política reage e alguém lê o resultado.

Cada fluxo tem um desenho antes da tabela. A raia de cima traz a tela ou a automação que age. A do meio traz o comando e a leitura. As de baixo trazem os eventos de cada contexto. `NoDoCorredor` é o nó. `P1`, `P4` e `P20` são as políticas da sessão 1 que não têm classe com nome próprio.

### Fluxo 1: consentimento e instalação

No desenho, veja que `CasaAtivada` só aparece na raia do Motor depois de `ConsentimentoRegistrado`, pela política P1. A leitura `ListaDeNomes` vem logo depois do vizinho para as setas ficarem curtas. Na tabela, ela é o passo 7.

![Fluxo 1: consentimento e instalação](diagramas/08-event-storming-fluxo-1-consentimento.mmd)

| # | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|
| 1 | ACS | `CadastrarIdoso` | `Idoso` | `IdosoCadastrado` [g] | P15. Nome e endereço ganham rótulo e são gravados inteiros. A casa ainda não tem token. | Ficha do idoso. |
| 2 | ACS, com a idosa | `RegistrarConsentimento` | `Idoso` | `ConsentimentoRegistrado` [g] | P1 | Ficha com o token. |
| 3 | P1 | `AtivarCasa` | `CasaMonitorada` | `CasaAtivada` [g] | | Radar: a casa entra `Cinza`, porque nenhum pacote chegou. |
| 4 | ACS | `IndicarVizinho` | `Idoso` | `VizinhoDeConfiancaIndicado` [g] | Só com a autorização `AvisarVizinho` e a data em que o vizinho aceitou. | |
| 5 | Técnico | Grava o token no nó, fora do sistema. | | | | |
| 6 | Nó | Publica `HEARTBEAT` | `CasaMonitorada` | `HeartbeatRecebido`, `PrimeiroPacoteRecebido` [d] | P2 | Radar: a casa sai do cinza. |
| 7 | App | `GET /lista-de-nomes` | | `ListaDeNomesConsultada`, na auditoria | P3 | Lista de nomes no SQLite. |
| 8 | Idosa, por meio da ACS | `RevogarConsentimento` | `Idoso` | `ConsentimentoRevogado` [g] | P4. O token sai da `Casa`, o vizinho é removido e os avisos do token são apagados. As visitas ficam. | A casa some da lista. |
| 9 | P4 | `DesativarCasa` | `CasaMonitorada` | `CasaDesativada` [g], `EventosDaCasaApagados` | | A casa some do radar. |

Regras que saíram daqui: sem consentimento vigente não existe token. O token é aleatório e nunca deriva de nome, endereço ou documento. Um consentimento novo gera um token novo. Registrar de novo com um consentimento vigente devolve o mesmo token e repete P1. A política P22 conserta a casa que ficou sem ativação no Motor.

### Fluxo 2: noite comum

No desenho, veja que `LuzDeCaminhoAcesa` fica numa raia só do nó e que nenhuma tela lê o movimento da noite. O radar só mostra o aparelho sem sinal.

![Fluxo 2: noite comum](diagramas/09-event-storming-fluxo-2-noite-comum.mmd)

| # | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|
| 1 | Idosa, às 03h12 | Passa no corredor | | `LuzDeCaminhoAcesa`, só no nó | P5 | |
| 2 | Nó | Publica `MOVIMENTO` | `EventoSensor` | `MovimentoDetectado` [g] | P6 | Linha do tempo da casa. |
| 3 | Nó | Publica `HEARTBEAT` a cada 20 segundos e `TELEMETRIA` a cada 5 minutos | `CasaMonitorada`, `EventoSensor` | `HeartbeatRecebido`, `TelemetriaRecebida` [g] | O heartbeat só avança `UltimoPacoteEm`. | |
| 4 | Broker cai às 03h30 | | | `CasaFicouCinza` [d], às 04h00 | P14 | Radar: aparelho sem sinal. |
| 5 | Nó, às 04h10 | Reenvia o buffer | `EventoSensor` | `EventoReenviado` [g], com `OcorridoEm` de 03h31 e `RecebidoEm` de 04h10 | P6 | A casa sai do cinza. |
| 6 | Relógio, de hora em hora | `Expurgo` | `EventoSensor`, `CasaMonitorada` | `EventosExpurgados` | P7 | |
| 7 | ACS, de manhã | `GET /radar` | | `RadarConsultado`, na auditoria | | "Hoje não é dia crítico." Nenhuma pendência. As casas sem alerta estão `Aguardando`. |

Regras que saíram daqui: fora do turno só valem as regras de energia e de heartbeat. Fora do turno nenhuma casa é `Verde`. O movimento da noite não aparece para ninguém e some em 24 horas.

### Fluxo 3: dia crítico, das 05h ao fim do turno

O fluxo 3 tem dois desenhos. No primeiro, das 05h00 às 10h12, veja a janela das 10h. O relógio produz `JanelaEncerrada` e `CasaFicouVermelha` sem nenhum comando.

![Fluxo 3, manhã: do dia crítico declarado à primeira visita](diagramas/10-event-storming-fluxo-3-dia-critico-manha.mmd)

No segundo, das 13h00 às 17h30, veja que `TurnoEncerrado` leva o resumo, a única leitura que sobrevive ao expurgo. A resposta do vizinho da casa C e a visita `NaoAbriu` estão nos desenhos dos fluxos 8 e 7.

![Fluxo 3, tarde: calor, pedido de toque, janela das 15h e encerramento](diagramas/11-event-storming-fluxo-3-dia-critico-tarde.mmd)

| # | Hora | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|---|
| 1 | 05h00 | Job de previsão | `DeclararDiaCritico` | `DiaCritico` | `DiaCriticoDeclarado` [g], `TurnoAberto` [d] | P8. Repetir o `PUT` não abre outro turno nem muda a abertura. | Nó: LED âmbar. Radar: 36 casas `Aguardando` e as 4 `Acamado` em `Vermelho` desde a abertura (03 §5.2). |
| 2 | 06h12 | Idosa A | Passa no corredor | `EventoSensor` | `MovimentoDetectado` [g], `CasaFicouVerde` [d] | | Radar: A sai de `Aguardando`. |
| 3 | 07h30 | Idosa B | Dois toques no botão | `EventoSensor` | `SaidaAvisada` [g], `CasaFicouVerde` [d] | P9 | |
| 4 | 09h00 | ACS | `GET /radar` | | `RadarConsultado` | | 23 verdes, 12 `Aguardando`, 1 cinza pendente e as 4 `Acamado` vermelhas até a ACS ver a pessoa. |
| 5 | 10h00 | Relógio | | | `JanelaEncerrada` [d], `CasaFicouVermelha` [d] para C, D e E, `CasaFicouAguardando` [d] para as verdes | | Radar: C, D e E vermelhas e pendentes. A contagem de verdes recomeça. |
| 6 | 10h00 | `VigiaDoRadar` | `PedirContatoAsync` para C, D e E | | `PedidoDeContatoEnviado` | P10, depois P19 no Cadastro | `GET /avisos`: "vizinho avisado às 10h00". |
| 7 | 10h05 | Vizinho de C | `RegistrarResposta`, `EstaBem` | `AvisoAoVizinho` | `VizinhoRespondeu` [g] | P20 | |
| 8 | 10h05 | P20 | `Confirmar`, `Vizinho` e `PessoaVista` | `CasaMonitorada` | `ConfirmacaoRegistrada` [g], `CasaFicouVerde` [d] | | Radar: C sai das pendentes. |
| 9 | 10h12 | ACS | `RegistrarVisita`, `EstavaBem` | `Visita` | `VisitaRegistrada` [g] | P11 chama `ConfirmarAsync` com `Acs` e `PessoaVista`. | Radar: D fica verde. |
| 10 | 10h40 | ACS | `RegistrarVisita`, `NaoAbriu` | `Visita` | `VisitaRegistrada` [g] | P11 não chama o Motor. | Radar: E continua vermelha e pendente. App: "SAMU chamado às 10h40". |
| 11 | 13h00 | Nó de F | `TELEMETRIA` com 34,5 °C desde as 11h00 | `EventoSensor` | `CasaFicouVermelha` [d], por calor | P10 | Radar: F vermelha, "calor dentro de casa". |
| 12 | 13h30 | ACS | `RegistrarVisita` em F, `EstavaBem` | `Visita` | `VisitaRegistrada` [g], `ConfirmacaoRegistrada` [g] | P11 | Radar: F vermelha e atendida até as 15h. |
| 13 | 14h30 | Relógio | `PedirToque` | `DiaCritico` | `ToquePedido` [g] | P12 | Nó: buzzer nas casas sem sinal de vida desde as 10h. |
| 14 | 14h40 | Idosa G | Um toque no botão | `EventoSensor` | `BotaoBemPressionado` [g], `CasaFicouVerde` [d] | | |
| 15 | 15h00 | Relógio | | | `JanelaEncerrada` [d], `CasaFicouVermelha` [d] para as casas `Sozinho` sem sinal de vida desde as 10h | P10 | Radar: E ganha alerta novo, desde 15h00. F volta a pendente se o calor continua. C, D e G ficam `Aguardando`. |
| 16 | 17h30 | ACS | `EncerrarTurno` | `DiaCritico` | `TurnoEncerrado` [g], com o resumo | P13 | Tela de encerramento com o indicador. |

Regras que saíram daqui:

- A janela da manhã vai da abertura às 10h. A janela da tarde vai das 10h às 15h. Depois das 15h, a janela atual vai até o fim do dia.
- Um sinal de vida depois do prazo tira a casa do vermelho daquela janela.
- `Verde` exige sinal de vida na janela atual. Às 10h, a casa que só deu sinal de vida de manhã vai para `Aguardando`, nunca para `Vermelho`.
- A confirmação vale só na janela atual. Às 15h, a visita das 13h30 em F deixa de atender o alerta de calor.
- O resumo do turno é gravado no encerramento, porque os eventos somem em 24 horas e o indicador de impacto sumiria junto.

### Fluxo 4: apagão com eletrodependente

No desenho, veja as duas confirmações. A do vizinho não muda o radar. Só a da ACS, depois da volta da energia, solta a trava. O ramo 2b, sem pacote por 60 segundos, não está no desenho.

![Fluxo 4: apagão com eletrodependente](diagramas/12-event-storming-fluxo-4-apagao.mmd)

| # | Hora | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|---|
| 1 | 02h40 | Rede elétrica cai | | | | No nó, a bateria mantém o sensor ligado. | |
| 2a | 02h40 | Nó, se o broker responde | Publica `TELEMETRIA` com 0 V | `EventoSensor` | `TelemetriaRecebida` [g], `EnergiaCaiu` [d], `CasaFicouVermelha` [d] | | |
| 2b | 02h41 | Nó, se o roteador caiu | Nada chega | | `EnergiaCaiu` [d], depois de 60 segundos sem pacote | | |
| 3 | 02h41 | `VigiaDoRadar` | `PedirContatoAsync` | | `PedidoDeContatoEnviado` | P10, depois P19 | Radar, em qualquer dia: a casa no topo. |
| 4 | 02h41 | P19 | `RegistrarAviso` | `AvisoAoVizinho` | `AvisoAoVizinhoRegistrado` [g] | Texto neutro pela `SaidaSegura`, no canal `Vizinho`. Envio simulado. | |
| 5 | 02h55 | Vizinho | `RegistrarResposta`, `EstaBem` | `AvisoAoVizinho` | `VizinhoRespondeu` [g], `ConfirmacaoRegistrada` [g] | P20 | Radar: a casa continua vermelha e pendente, porque a regra de energia não aceita o vizinho. |
| 6 | 04h10 | Energia volta | O nó publica o buffer com a hora original e 127 V | `EventoSensor` | `EventoReenviado` [g] | P6 | A casa continua vermelha e pendente, pela trava. |
| 7 | 07h00 | ACS | `GET /radar` | | `RadarConsultado` | | Linha do tempo com a queda e a volta. |
| 8 | 07h30 | ACS | `RegistrarVisita`, `EstavaBem` | `Visita`, depois `CasaMonitorada` | `VisitaRegistrada` [g], `ConfirmacaoRegistrada` [g] | P11 | A trava solta. Sem turno, a casa fica `Aguardando`. |

Regras que saíram daqui: a regra de energia vale em qualquer dia e a qualquer hora, e é a primeira da lista. Às 02h40 ninguém olha o app, então o aviso ao vizinho é o único caminho em tempo real. O vizinho é avisado, mas só a ACS atende o alerta de energia. Depois de uma leitura baixa, só uma confirmação da ACS posterior à leitura tira o vermelho. No protótipo o canal do vizinho é simulado, e o vídeo diz isso.

### Fluxo 5: aparelho mudo

No desenho, veja que `CasaFicouCinza` vem do relógio, não de um pacote, e que só o `EventoReenviado` tira a casa do cinza.

![Fluxo 5: aparelho mudo](diagramas/13-event-storming-fluxo-5-aparelho-mudo.mmd)

| # | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|
| 1 | Idosa, às 08h10 | Passa no corredor | `EventoSensor` | `MovimentoDetectado` [g] | | A casa está verde. |
| 2 | Nó para às 09h10 | Nada chega | | `CasaFicouCinza` [d], às 09h40 | P14 | Card cinza: "aparelho sem sinal desde 09h10". |
| 3 | `VigiaDoRadar` | `PedirContatoAsync` | | `PedidoDeContatoEnviado` | P10, com turno aberto | |
| 4 | ACS, às 10h20 | `RegistrarVisita`, `EstavaBem` | `Visita`, depois `CasaMonitorada` | `VisitaRegistrada` [g], `ConfirmacaoRegistrada` [g] | P11 | A casa fica cinza e atendida até as 15h. Entra nas atendidas, não na contagem de verdes. |
| 5 | Nó volta às 11h00 | Publica o buffer e um `HEARTBEAT` | `EventoSensor`, `CasaMonitorada` | `EventoReenviado` [g], `CasaFicouVerde` [d] | P6 | A casa sai do cinza. A visita das 10h20 é sinal de vida na janela da tarde. |

Regra que saiu daqui: o silêncio do nó nunca vira verde. Uma casa cinza só sai das pendentes por dois caminhos: uma confirmação aceita na janela atual ou a volta do nó.

### Fluxo 6: mensagem bloqueada para o grupo do bairro

No desenho, veja a ordem das raias. `SaidaBloqueada` nasce na Guarda antes de `MensagemBloqueada` nascer no Cadastro, e o `Tradutor` só age depois. A `SaidaSegura` não tem quadro próprio. Ela decide entre o comando e o evento.

![Fluxo 6: mensagem bloqueada para o grupo do bairro](diagramas/14-event-storming-fluxo-6-mensagem-bloqueada.mmd)

| # | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|
| 1 | ACS | Escreve "ele mora sozinho e toma diurético" e toca em verificar | | | P15. O texto não tem CPF, RG, CNS nem credencial, então o corpo não muda. O `MotorDeParsing` rotula o texto com `MoraSozinho` e `Saude`. | |
| 2 | App | `VerificarMensagem`, com o canal `GrupoDoBairro` | `MensagemSaida` | | O Cadastro pede o veredito à `SaidaSegura`. | |
| 3 | `SaidaSegura` | Avalia a saída | `EventoDeSeguranca` | `SaidaBloqueada` [g], severidade 4 | P16 | |
| 4 | Cadastro | | `MensagemSaida` | `MensagemBloqueada` [g], só com o texto mascarado | | |
| 5 | App | | | | | Tela de mensagem bloqueada com a frase fixa e a versão segura. |
| 6 | `Tradutor`, por P17 | `AnexarMensagem`, depois da resposta | `EventoDeSeguranca` | `BloqueioTraduzido` [g] | O prompt passa pela `SaidaSegura` com o canal `PromptDoLlm`. | |
| 7 | Cidadão | Abre o painel | | `FarolMudou` [d] | | Farol vermelho e a mensagem ao cidadão. |
| 8 | ACS | `VerificarMensagem` com a versão segura | `MensagemSaida` | `MensagemLiberada` [g] | | Tela com o texto para copiar. |

Regras que saíram daqui: a mensagem bloqueada é um resultado normal do negócio, então a rota responde 200 com o veredito. O texto original nunca é gravado. O sistema não envia nada ao grupo. A ACS copia a versão segura. Só uma saída bloqueada deixa o farol vermelho.

### Fluxo 7: desfecho da visita

No desenho, veja que a visita toca os três contextos. A Guarda mascara, o Cadastro grava e o Motor confirma. O `Expurgo` não alcança a raia do Cadastro.

![Fluxo 7: desfecho da visita](diagramas/15-event-storming-fluxo-7-desfecho-da-visita.mmd)

| # | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|
| 1 | ACS, na porta | Escolhe o desfecho no app | | | No app, o desfecho fica no SQLite com um `id` e a hora da visita. | Card com o desfecho. |
| 2 | App, quando há rede | `RegistrarVisita`, com `id`, `casaToken`, `ocorridaEm`, `desfecho` e `observacao` | `Visita` | `VisitaRegistrada` [g] | P18 | |
| 3 | `InterceptadorDeEntrada` | | `EventoDeSeguranca` | `PiiMascaradaNaEntrada` [g], se a observação tem um CPF | P15 | Painel: farol amarelo. |
| 4 | P11 | `Confirmar` | `CasaMonitorada` | `ConfirmacaoRegistrada` [g], ou nada | P11. A tabela abaixo decide. | Radar atualizado. |
| 5 | Relógio, 24 horas depois | `Expurgo` | `EventoSensor`, `CasaMonitorada` | `EventosExpurgados` | P7 | A visita continua no Cadastro. |

| Desfecho | A pessoa foi vista? | Chamada ao Motor | Efeito no radar |
|---|---|---|---|
| `EstavaBem` | Sim | `ConfirmarAsync` com `Acs` e `PessoaVista` | Sinal de vida. Atende o calor, o cinza e a energia na janela atual. |
| `Encaminhada` | Sim | `ConfirmarAsync` com `Acs` e `PessoaVista` | Igual a `EstavaBem`. |
| `EstavaFora` | Não | `ConfirmarAsync` com `Acs` e `AusenciaConfirmada` | Atende o alerta de silêncio só na janela atual. Nunca é sinal de vida, então a casa nunca fica verde por ele. |
| `NaoAbriu` | Não | Nenhuma | A casa continua vermelha e pendente. O app mostra "SAMU chamado". |

Regras que saíram daqui: a visita é a proteção da ACS, então ela é imutável e sobrevive ao expurgo. A hora que vale é a da visita, não a do envio. O Cadastro grava a visita primeiro e chama o Motor depois. Se a chamada falhar, o app reenvia o mesmo `id`, e as duas pontas chegam ao mesmo resultado.

### Fluxo 8: pedido de contato e aviso ao vizinho

No desenho, veja a quarta fatia. O `VigiaDoRadar` repete o pedido, e o `ReceptorDeContato` não gera comando, porque o episódio já existe. Só o `Desde` novo das 15h cria outro aviso.

![Fluxo 8: pedido de contato e aviso ao vizinho](diagramas/16-event-storming-fluxo-8-pedido-de-contato.mmd)

| # | Ator | Comando | Agregado | Evento | Política | Leitura |
|---|---|---|---|---|---|---|
| 1 | `VigiaDoRadar`, a cada 15 segundos | Avalia as casas ativas | | `PedidoDeContatoEnviado` por alerta pendente | P10 | |
| 2 | `ReceptorDeContato` | Acha o idoso pelo token e confere a autorização e o vizinho | `Idoso` | | Sem autorização ou sem vizinho, não faz nada. | |
| 3 | P19 | `RegistrarAviso`, com a chave `(casaToken, motivo, desde)` | `AvisoAoVizinho` | `AvisoAoVizinhoRegistrado` [g] | Chave repetida não cria outro aviso. O texto neutro passa pela `SaidaSegura` no canal `Vizinho`. | `GET /avisos` |
| 4 | Vizinho | `RegistrarResposta`, `EstaBem` | `AvisoAoVizinho` | `VizinhoRespondeu` [g] | P20. O `Id` da confirmação é o `Id` do aviso. | |
| 5 | Vizinho | `RegistrarResposta`, `PrecisaDeAjuda` | `AvisoAoVizinho` | `VizinhoRespondeu` [g] | Não chama o Motor. | App: "vizinho pediu ajuda". |
| 6 | `VigiaDoRadar`, no ciclo seguinte | Mesmo alerta | | `PedidoDeContatoEnviado` | P19 descarta, porque o episódio já existe. | |
| 7 | Relógio, às 15h | Janela nova, alerta novo | | `PedidoDeContatoEnviado` com outro `Desde` | P19 cria outro aviso. | |

Regras que saíram daqui: o Motor não guarda alerta, aviso nem pedido. O episódio é o trio `(casaToken, motivo, desde)`, e o Cadastro é quem lembra dele. A família nunca é avisada sem a autorização `AvisarFamilia`. O canal da família fica fora da entrega.

## Sessão 3: nível de design

A pergunta desta sessão é uma só: quais fatos precisam mudar juntos, na mesma transação? Cada resposta "sim" vira um agregado. Cada resposta "não" vira dois agregados ou um cálculo.

| Candidato | Decisão | Motivo |
|---|---|---|
| Situação da casa | Não é agregado. É o resultado de uma função pura, o `AvaliadorDeCasas`. | A situação muda com a passagem do tempo, sem evento. Uma coluna ficaria velha e poderia mostrar um verde sem sinal de vida. |
| `CasaMonitorada` com as confirmações e `UltimoPacoteEm` | Um agregado. Raiz `CasaMonitorada`. | Uma confirmação só vale para uma casa ativa. `UltimoPacoteEm` só anda para frente. São poucos fatos por dia. |
| `EventoSensor` | Agregado próprio, de uma entidade imutável. | Chegam centenas por dia por casa. Carregar a casa inteira para inserir um evento não tem ganho. A regra dele é a chave `(casaToken, seq, ts)`. |
| Heartbeat como `EventoSensor` | Recusado. | Seriam cerca de 170 mil linhas por dia. O único fato que as regras usam é `UltimoPacoteEm`. |
| `DiaCritico` com o turno | Um agregado. Raiz `DiaCritico`. `TurnoRadar` e `ResumoDoTurno` são objetos de valor. | O turno existe porque o dia crítico existe, e os dois têm a mesma chave, a data. Existe um encerramento por data. |
| `TurnoRadar` como entidade com tabela | Recusado. | Mesmo ciclo de vida do `DiaCritico`. |
| Alerta, registro de alerta ou aviso dentro do Motor | Recusado. | Um alerta gravado é um segundo valor da situação, que pode discordar do calculado. O Motor não guarda alerta. |
| `AvisoAoVizinho` no Cadastro | Agregado próprio, pequeno. | Ele guarda o fato de que o aviso saiu e a resposta. A chave `(casaToken, motivo, desde)` impede o segundo aviso do mesmo episódio. O telefone do vizinho já mora no Cadastro. |
| `Idoso` com `Casa`, `Consentimento` e `VizinhoDeConfianca` | Um agregado. Raiz `Idoso`. | O token só existe com consentimento. O vizinho só existe com autorização. Revogar apaga o token e o vizinho na mesma decisão. |
| `Casa` separada de `Idoso` | Recusado. | Um token que sobrevive à revogação quebraria a regra do consentimento. Nenhum fluxo de 19/11 tem dois idosos na mesma casa. |
| `Visita` | Agregado próprio. | Ela é imutável e vive mais que os eventos e que o consentimento. |
| `MensagemSaida` | Agregado próprio. | Ela registra uma tentativa de saída e o veredito, só com o texto mascarado. Não depende de um idoso. |
| `Microarea` | Referência. | Só muda por carga inicial. |
| `EventoDeSeguranca` | Agregado da Guarda. | É o único fato que a Guarda grava. O farol é calculado sobre ele. |
| `RegraDePii`, `PoliticaDeCanal` | Não são agregados. São tabelas de configuração. | Mudam por arquivo, não por comando. |
| Radar, linha do tempo, lista de nomes, avisos do dia, farol | Modelos de leitura. | São consultas. Ninguém manda um comando para eles. |
| `Alerta`, `Avaliacao`, `JanelaDeContato`, `HistoricoDaCasa` | Objetos de valor calculados. | Saem da avaliação e não têm identidade. |
