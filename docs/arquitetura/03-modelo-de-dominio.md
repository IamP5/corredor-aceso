# Modelo de domínio do Corredor Aceso

Este documento define o modelo de domínio que a equipe constrói até 19/11/2026, 23h55. Ele parte das sessões de [02-event-storming.md](02-event-storming.md) e chega aos contextos, aos projetos .NET, aos nomes de código, às regras da situação da casa, aos contratos e aos casos de teste.

O modelo mantém as decisões fixas da primeira versão da arquitetura, `docs/arquitetura.md`, hoje substituída por [01-visao-geral.md](01-visao-geral.md): um monólito modular .NET 10 com os módulos Motor, Cadastro e Guarda, a situação da casa calculada por função pura e o contrato MQTT. A seção 9 lista cada ponto em que este modelo muda a arquitetura.

Quem programa o Motor lê as seções 3, 4.2, 5 e 6. Quem programa o Cadastro lê 3, 4.3 e 6. Quem programa a Guarda lê 3, 4.4 e 6.3. Quem programa o nó lê 6.2. Quem programa o app lê 3, 5.6 e 6.3. Todos leem a seção 8 antes de abrir o editor.

## 1. Contextos delimitados

### 1.1 Os contextos

Cada contexto leva o nome do módulo.

| Contexto | Subdomínio | Pergunta que ele responde | Dado pessoal | Projeto |
|---|---|---|---|---|
| Motor | Principal | Quem ficou sem sinal de vida e em que ordem a ACS deve ir? | Só dado pseudonimizado: token, microárea e perfil. Não é anônimo, porque o Cadastro sabe ligar o token à pessoa. | `api/src/Corredor.Motor` |
| Cadastro | De apoio | Quem é a pessoa, o que ela autorizou, o que a ACS encontrou e quem o vizinho deve olhar? | Dono único. | `api/src/Corredor.Cadastro` |
| Guarda | De apoio | Este texto pode sair por este canal? | Nunca grava o valor. Só o tipo de dado. | `api/src/Corredor.Guarda` |

`Corredor.Contratos` não é um contexto. Ele é o núcleo compartilhado mínimo entre Motor e Cadastro: tipos e interfaces, sem lógica. `Corredor.Api` também não é contexto. Ele é o host: autenticação, injeção de dependência e os middlewares da Guarda.

Fora do processo .NET existem cinco contextos externos: o nó, o job de previsão, o app da ACS, o LLM e o canal do vizinho.

A mesma casa aparece em dois contextos com dois modelos. No Cadastro ela é `Casa`, com endereço e token, dentro do `Idoso`. No Motor ela é `CasaMonitorada`, com token, microárea, perfil e último pacote. O único elo entre as duas é o `CasaToken`.

### 1.2 Mapa de contexto

No desenho, a borda tracejada marca os cinco contextos de fora do processo .NET. Veja que nenhuma seta liga o Motor à Guarda.

![Mapa de contexto do Corredor Aceso](diagramas/07-mapa-de-contextos.mmd)

| Par | Relação | Quem manda no contrato | Como aparece no código |
|---|---|---|---|
| Motor e Cadastro | Cliente e fornecedor. O Motor é o fornecedor. | Motor | O Motor define `IMotorDeCasas` e os tipos em `Corredor.Contratos`. O Cadastro chama a interface. Nenhum dos dois projetos referencia o outro. O host liga a implementação. |
| Motor e Cadastro, no pedido de contato | Inversão de dependência. O Motor declara a porta e o Cadastro implementa. | Motor | `IReceptorDeContato` em `Corredor.Contratos`. `ReceptorDeContato` no Cadastro. |
| Motor e Cadastro, nos tipos | Núcleo compartilhado mínimo | Motor, com acordo do Cadastro | `Corredor.Contratos` só tem os tipos da seção 6.1. Uma mudança ali exige acordo das duas pessoas. |
| Cadastro e Guarda | Conformista | Guarda | `Idoso.Nome` tem o tipo `TextoRotulado`. Toda saída de texto do Cadastro passa pela `SaidaSegura`. |
| Motor e Guarda | Caminhos separados | Ninguém | Nenhum projeto referencia o outro. O host aplica o `InterceptadorDeEntrada`, o `FiltroDeResposta` e o `TratadorDeErros` também nas rotas do Motor. |
| Nó e Motor | Linguagem publicada, com camada anticorrupção no Motor | O contrato MQTT v1 | `TradutorDePacote` converte o JSON em `PacoteDoNo`. Nenhuma classe de domínio conhece JSON nem MQTT. |
| Job de previsão e Motor | Serviço aberto. O job é conformista. | Motor | `PUT /api/dias-criticos/{data}`. |
| App e Motor, app e Cadastro | Serviço aberto com linguagem publicada. O app é conformista. | Motor e Cadastro | O Swagger. O app junta `casaToken` e nome no celular. |
| Guarda e LLM | Camada anticorrupção | Guarda | `Tradutor` monta o prompt com tipo de dado, canal e módulo. Tem frase fixa de reserva. |
| Cadastro e canal do vizinho | Camada anticorrupção | Cadastro | `ReceptorDeContato` monta o texto neutro e grava o envio simulado. |

## 2. Projetos .NET e referências permitidas

```
api/
  src/
    Corredor.Contratos/   tipos e interfaces compartilhados. Não referencia nada.
    Corredor.Motor/       referencia Corredor.Contratos.
    Corredor.Cadastro/    referencia Corredor.Contratos e Corredor.Guarda.
    Corredor.Guarda/      não referencia nenhum projeto da solução.
    Corredor.Api/         referencia os quatro. Único Program.cs.
  tests/
    Corredor.Motor.Testes/
    Corredor.Cadastro.Testes/
    Corredor.Guarda.Testes/
```

| Projeto | Pode referenciar | Não pode referenciar |
|---|---|---|
| `Corredor.Contratos` | nada | qualquer projeto |
| `Corredor.Motor` | `Corredor.Contratos` | `Corredor.Cadastro`, `Corredor.Guarda` |
| `Corredor.Guarda` | nada | qualquer projeto |
| `Corredor.Cadastro` | `Corredor.Contratos`, `Corredor.Guarda` | `Corredor.Motor` |
| `Corredor.Api` | os quatro | |

No desenho, a seta tracejada é referência de projeto e a seta de ponta vazada é implementação. Veja que nenhuma seta liga `Corredor.Motor` a `Corredor.Cadastro`.

![Projetos .NET e referências permitidas](diagramas/02-modulos.mmd)

O compilador garante a regra de privacidade. O Motor não tem como ler um nome, porque não referencia o Cadastro e não conhece `TextoRotulado`. O Cadastro não tem como abrir o `MotorDbContext`, porque não referencia o Motor.

Cada módulo usa as mesmas pastas:

```
Corredor.Motor/
  Dominio/      agregados, objetos de valor, regras, eventos de domínio. Sem EF Core e sem ASP.NET.
  Aplicacao/    um caso de uso por comando. Carrega, chama o agregado, grava, executa a política.
  Infra/        DbContext, migrações, MQTT, BackgroundService.
  Rotas/        endpoints do módulo.
  MotorModulo.cs   AddMotor() e MapMotor().
```

O `Program.cs` tem uma linha de registro por módulo, e só uma pessoa edita o arquivo:

```csharp
builder.Services.AddGuarda(cfg).AddMotor(cfg).AddCadastro(cfg);
app.UseGuarda();
app.MapMotor(); app.MapCadastro(); app.MapGuarda();
```

Um Postgres, três schemas: `motor`, `cadastro` e `guarda`. Três `DbContext`, cada um com as próprias migrações do EF Core. Nenhum módulo lê o schema de outro. Cada módulo conecta com um papel próprio do Postgres, que não enxerga o schema dos outros.

## 3. Linguagem ubíqua

Os nomes de domínio ficam em português, sem acento e sem cedilha no código. Os sufixos do framework ficam em inglês, como `Async` e `DbContext`. Os projetos de teste terminam em `Testes`. O nome da coluna "Nome no código" é o que aparece em classe, tabela, rota e teste.

| Termo | Significado | Nome no código | Contexto | Não use |
|---|---|---|---|---|
| ACS | Agente comunitária de saúde. Opera o app e o radar. | `AcsId` | Cadastro | agente, usuário, operador |
| Microárea | Território de uma ACS. | `Microarea`, `MicroareaId` | Cadastro, Contratos | área, região |
| Idoso | A pessoa cadastrada. Vale para a idosa e para o idoso. | `Idoso` | Cadastro | paciente, usuário, cidadão |
| Casa | O endereço em que o idoso mora, com o token. | `Casa` | Cadastro | residência, domicílio |
| Casa monitorada | A mesma casa vista pelo Motor: token, microárea, perfil e último pacote. | `CasaMonitorada` | Motor | casa, no Motor |
| Token da casa | Identificador aleatório de 128 bits. Único elo entre `Casa` e `CasaMonitorada`. | `CasaToken` | Contratos | id da casa, código |
| Consentimento | Autorização do idoso, registrada na UBS com a ACS presente. Vigente ou revogado. | `Consentimento` | Cadastro | termo, aceite |
| Autorizações | As escolhas dentro do consentimento: avisar o vizinho e avisar a família. | `Autorizacoes` | Cadastro | opt-in, permissões, escopo |
| Vizinho de confiança | Pessoa que o idoso escolheu e que aceitou ser avisada. | `VizinhoDeConfianca` | Cadastro | contato, cuidador, responsável |
| Perfil da casa | O perfil de contato mais a eletrodependência. | `PerfilDaCasa` | Contratos | tipo, categoria |
| Perfil de contato | Como a casa dá sinal de vida: `Sozinho`, `Acamado` ou `Coabitado`. | `PerfilDeContato` | Contratos | mobilidade, convívio |
| Eletrodependente | Casa com equipamento de saúde ligado na tomada. | `PerfilDaCasa.Eletrodependente` | Contratos | cliente vital |
| Nó | O ESP32 do corredor. Na tela, "aparelho". | sem classe | fora do .NET | dispositivo, placa |
| Pacote | Qualquer mensagem MQTT do nó. | `PacoteDoNo`, `TipoDePacote` | Motor | payload, ping |
| Último pacote | Hora do servidor em que chegou o último pacote aceito. | `CasaMonitorada.UltimoPacoteEm` | Motor | último heartbeat |
| Evento do sensor | Pacote gravado: movimento, botão ou telemetria. Nunca heartbeat. | `EventoSensor` | Motor | registro, mensagem |
| Leitura | Temperatura e tensão de um evento do sensor. | `TempC`, `TensaoV` | Motor | medição |
| Ocorrido em | Hora do fato no nó. Vem do `ts`. | `OcorridoEm` | Motor | timestamp |
| Recebido em | Hora do servidor em que o pacote chegou. | `RecebidoEm` | Motor | hora de chegada |
| Sinal de vida | Evidência de que a pessoa estava bem num instante. Depende do perfil de contato. Na tela, "deu sinal hoje". | `HistoricoDaCasa.TemSinalDeVidaDesde` | Motor | check-in, presença, prova |
| Saída avisada | Duplo toque do idoso, "hoje vou sair". | `TipoDePacote.BotaoSair` | Motor | ausência, aviso de saída |
| Confirmação | Um humano informa o que encontrou na casa: origem, resultado e hora. | `Confirmacao` | Contratos | check, baixa, validação |
| Origem da confirmação | Quem confirmou: `Acs` ou `Vizinho`. | `OrigemDaConfirmacao` | Contratos | fonte |
| Pessoa vista | Resultado em que alguém viu a pessoa bem. É sinal de vida. | `ResultadoDaConfirmacao.PessoaVista` | Contratos | contato feito |
| Ausência confirmada | Resultado em que a ACS foi à casa e a pessoa estava fora. Nunca é sinal de vida. | `ResultadoDaConfirmacao.AusenciaConfirmada` | Contratos | saída, fora de casa |
| Dia crítico | O dia em que o modelo, a regra base ou a UBS prevê excesso de internações de 60+. | `DiaCritico` | Motor | alerta de calor, dia de risco |
| Origem do dia crítico | `Modelo`, `RegraBase` ou `Manual`. | `OrigemDoDiaCritico` | Motor | fonte |
| Turno | O tempo do dia crítico em que as regras de calor e de silêncio valem: da abertura ao encerramento ou às 18h. | `TurnoRadar` | Motor | plantão, sessão |
| Janela | Pedaço do dia que limita a validade de um sinal de vida e de uma confirmação. | `JanelaDeContato` | Motor | período, faixa, intervalo |
| Prazo | Hora em que a janela da manhã ou a da tarde vence. | `JanelaDeContato.Prazo` | Motor | limite, deadline |
| Regra de contato | Uma das regras que podem pôr a casa em alerta. | `RegraDeContato` | Motor | validação, critério |
| Alerta | O que uma regra de contato devolve quando pega a casa: situação, motivo e desde quando. Calculado, nunca gravado. | `Alerta` | Motor | disparo, alarme, ocorrência |
| Motivo do alerta | O nome da regra que devolveu o alerta. | `MotivoDoAlerta` | Contratos | código da regra |
| Desde | Início do alerta. Com o token e o motivo, identifica o episódio. | `Alerta.Desde` | Motor | aberto em |
| Situação | `Verde`, `Aguardando`, `Vermelho` ou `Cinza`. | `SituacaoDaCasa` | Motor | estado, status, cor |
| Avaliação | A situação de uma casa num instante, com o alerta e o atendimento. | `Avaliacao` | Motor | diagnóstico |
| Atendida | A casa tem alerta e uma confirmação que a regra aceita, feita depois do `Desde` e dentro da janela atual. | `Avaliacao.Atendida` | Motor | resolvida, fechada |
| Pendente | A casa tem alerta e não está atendida. | `Avaliacao.Pendente` | Motor | aberta |
| Radar | As casas pendentes, atendidas e aguardando de uma microárea, em ordem, com a contagem de verdes. | `Radar`, `CardDoRadar` | Motor | lista, fila, painel |
| Linha do tempo | Eventos do sensor e confirmações das últimas 24 horas de uma casa. | `LinhaDoTempo` | Motor | histórico |
| Pedido de contato | O que o Motor manda pela porta quando uma casa tem alerta pendente: token, motivo e desde. | `PedidoDeContato`, `IReceptorDeContato` | Contratos | notificação |
| Pedido de toque | Comando que toca o buzzer do nó às 14h30. | `PedirToque`, `PEDIR_TOQUE` | Motor | lembrete |
| Parâmetros do Motor | Todos os números das regras, com valor de produção e de demo. | `ParametrosDoMotor` | Motor | limites, constantes soltas |
| Relógio de demo | Relógio do Motor que aceita um deslocamento em desenvolvimento. | `RelogioDeDemo` | Motor | relógio falso |
| Resumo do turno | As contagens gravadas no encerramento. É o indicador de impacto. | `ResumoDoTurno` | Motor | relatório |
| Expurgo | Apagar eventos do sensor e confirmações com mais de 24 horas. | `Expurgo` | Motor | limpeza, purga |
| Visita | A ida da ACS a uma casa, com desfecho. Não muda e não some. | `Visita` | Cadastro | atendimento, checagem |
| Desfecho | O que a ACS encontrou na visita. | `Desfecho` | Cadastro | status, conclusão |
| Aviso ao vizinho | Mensagem neutra que pede ao vizinho para olhar a pessoa. | `AvisoAoVizinho` | Cadastro | notificação, alerta |
| Resposta do vizinho | `EstaBem` ou `PrecisaDeAjuda`. | `RespostaDoVizinho` | Cadastro | retorno |
| Lista de nomes | Token, nome, endereço e vizinho das casas ativas, para o SQLite do celular. | `ListaDeNomes`, `ItemDaListaDeNomes` | Cadastro | agenda, cadastro no app |
| Mensagem de saída | Texto que a ACS quer mandar para um canal aberto, com o veredito. | `MensagemSaida` | Cadastro | post, envio |
| Versão segura | Texto fixo que substitui a mensagem bloqueada. | `VersaoSegura` | Cadastro | sugestão |
| Tipo de dado | A categoria de um dado pessoal, como CPF, saúde ou mora sozinho. É o rótulo do taint. | `TipoDeDado` | Guarda | tag, classe |
| Regra de PII | Padrão, validador, máscara e severidade de um tipo de dado. | `RegraDePii` | Guarda | filtro |
| Achado | Um dado pessoal encontrado num texto, sem o valor em claro. | `Achado` | Guarda | match, detecção |
| Texto rotulado | Um texto com os tipos de dado que ele carrega. | `TextoRotulado` | Guarda | string sensível |
| Canal | Lugar por onde um texto sai. | `Canal` | Guarda | destino, sumidouro |
| Política de canal | Tabela com os tipos de dado que cada canal aceita e a exposição dele. | `PoliticaDeCanal` | Guarda | regra de saída |
| Saída segura | A única porta por onde o valor de um texto rotulado sai. | `SaidaSegura` | Guarda | validador |
| Veredito | `Liberada` ou `Bloqueada`. | `Veredito` | Guarda | decisão da saída |
| Evento de segurança | O registro de um mascaramento na entrada ou de uma saída bloqueada, sem o valor. | `EventoDeSeguranca` | Guarda | incidente |
| Farol | O indicador verde, amarelo ou vermelho do painel Risco Cidadão. | `CorDoFarol`, `Farol` | Guarda | semáforo |
| Mensagem ao cidadão | O bloqueio explicado em duas frases simples. | `MensagemAoCidadao` | Guarda | tradução solta |

Palavras que colidem, e como a equipe usa cada uma:

- "Motor" sozinho é o módulo Motor. A classe da Guarda sempre se chama `MotorDeParsing`.
- "Verde" e "Vermelho" existem em `SituacaoDaCasa` e em `CorDoFarol`. São dois enums. Nenhum código converte um no outro.
- "Evento" sozinho não existe. Sempre `EventoSensor`, `EventoDeSeguranca` ou evento de domínio.
- "Saída" sozinho não existe. No Motor é saída avisada. Na Guarda é saída segura ou saída bloqueada.
- "Aviso" sozinho é o aviso ao vizinho.
- "Contato" sozinho não existe. Sempre regra de contato, perfil de contato, janela de contato ou pedido de contato.
- "Motivo" no radar é `MotivoDoAlerta`. No dia crítico, `DiaCritico.Motivo` é o texto que o job manda.

## 4. Design tático por contexto

### 4.1 Convenções dos três módulos

- **Raiz de agregado.** As propriedades têm `private set`. Os dados da raiz só mudam por um método com nome de comando. A criação passa por um método de fábrica estático que valida as invariantes. O EF Core usa um construtor privado sem parâmetros.
- **Objeto de valor.** É um `record` imutável que se valida na criação. Dois primitivos com sentidos diferentes ganham dois tipos. `CasaToken` e `MicroareaId` nunca são `string` ou `int` soltos dentro do domínio.
- **Tempo.** Nenhuma classe de domínio lê o relógio. Todo método que depende da hora recebe `agora`. Os casos de uso leem a hora do `TimeProvider`, que é o `RelogioDeDemo`. O banco guarda `DateTimeOffset` em UTC. O fuso `America/Sao_Paulo` aparece num lugar só, o corte das janelas.
- **Evento de domínio em código.** É um `record` imutável com nome no passado. O método do agregado devolve o evento. O caso de uso grava e depois chama a política, na mesma chamada. Não há MediatR, barramento nem outbox.
- **Erro de domínio.** Uma invariante violada lança `RegraDeDominioViolada`, com um código e uma mensagem sem dado pessoal. O `TratadorDeErros` converte em resposta 422, ou 409 quando o código é de conflito.
- **Idempotência.** Todo comando que pode ser repetido tem uma chave. A seção 4.5 lista cada uma.
- **Concorrência.** Com conflito de versão ou com chave de idempotência repetida no banco, o caso de uso recarrega e roda o comando de novo, uma vez. Todo comando com chave na seção 4.5 converge, então a segunda rodada devolve o que já existe.
- **Parâmetros.** Todo número das regras vem de `ParametrosDoMotor`, menos `ToleranciaDoRelogio`. Ela fica em `ParametrosDoRelogio`, em `Corredor.Contratos`, porque o Cadastro também usa. A seção 5.9 lista os valores.

### 4.2 Motor

No desenho, veja que a situação da casa não é campo de nenhum agregado. O `AvaliadorDeCasas` lê os três agregados e devolve a `Avaliacao`.

![Agregados do Motor](diagramas/17-agregados-motor.mmd)

#### Agregado `CasaMonitorada`

| Item | Conteúdo |
|---|---|
| Raiz | `CasaMonitorada`, identificada pelo `CasaToken`. |
| Entidade interna | `Confirmacao`, com `Id`, `Origem`, `Resultado` e `Quando`. |
| Objetos de valor | `CasaToken`, `MicroareaId`, `PerfilDaCasa`. |
| Gravado | Token, microárea, perfil, `AtivadaEm`, `DesativadaEm`, `UltimoPacoteEm` e as confirmações das últimas 24 horas. |
| Calculado | Situação, alerta, atendimento. Nada disso tem coluna. |

Invariantes:

1. O token e a microárea nunca mudam.
2. `UltimoPacoteEm` só anda para frente e recebe a hora do servidor, nunca o `ts` do pacote.
3. Uma casa desativada não aceita perfil, pacote nem confirmação. A desativação é definitiva. A linha fica, sem eventos nem confirmações, só para o token nunca voltar. Um consentimento novo gera outro token.
4. Uma confirmação com `Id` repetido é ignorada.
5. Uma confirmação com `Quando` depois de `agora` mais `ToleranciaDoRelogio` é recusada. Uma confirmação com mais de 24 horas é recusada.
6. A casa não tem propriedade de nome, endereço, telefone ou texto livre.

| Comando | Efeito | Evento | Chave de idempotência |
|---|---|---|---|
| `AtivarCasa(token, microarea, perfil, agora)` | Cria a casa. Casa ativa com o mesmo token só atualiza o perfil. Casa desativada recusa com `CASA_DESATIVADA`. | `CasaAtivada` | `casaToken` |
| `DefinirPerfil(perfil)` | Troca o perfil. | `PerfilDaCasaDefinido` | `casaToken`, o último valor vale |
| `RegistrarPacote(recebidoEm)` | Avança `UltimoPacoteEm`. | nenhum para outro módulo | não precisa: só anda para frente |
| `Confirmar(confirmacao, agora)` | Guarda a confirmação. | `ConfirmacaoRegistrada` | `Confirmacao.Id` |
| `DesativarCasa(agora)` | Grava `DesativadaEm`. O caso de uso apaga eventos e confirmações da casa. | `CasaDesativada` | `casaToken`. Repetir não muda nada. |

#### Agregado `EventoSensor`

| Item | Conteúdo |
|---|---|
| Raiz | `EventoSensor`, identificado por `(CasaToken, Seq, OcorridoEm)`. Imutável. |
| Objetos de valor | `TipoDePacote {Movimento, BotaoBem, BotaoSair, Telemetria, Heartbeat}`, `TempC`, `TensaoV`. |
| Gravado | Token, `Seq`, tipo, `OcorridoEm`, `RecebidoEm`, `TempC`, `TensaoV`, `Bufferizado`. |
| Calculado | Se é sinal de vida, que depende do perfil de contato. A seção 5.3 diz quando. |

Invariantes:

1. `(CasaToken, Seq, OcorridoEm)` é único. O índice único do banco garante. A gravação usa `INSERT ... ON CONFLICT DO NOTHING`, e a repetição conta como sucesso.
2. O tipo nunca é `Heartbeat`. O heartbeat só avança `UltimoPacoteEm`.
3. `OcorridoEm` não passa de `RecebidoEm` mais `ToleranciaDoRelogio`. `OcorridoEm` não tem mais de 24 horas. Um pacote fora disso é recusado inteiro e não avança `UltimoPacoteEm`.
4. O evento só existe para uma casa ativa. O caso de uso `RegistrarPacote` confere isso, porque a regra cruza dois agregados.
5. A temperatura fica entre -10 °C e 60 °C. A tensão fica entre 0 V e 260 V. O valor fora da faixa é gravado como ausente.

| Comando | Efeito | Evento | Chave de idempotência |
|---|---|---|---|
| `EventoSensor.De(pacote, recebidoEm)` | Cria o evento. | `EventoSensorRegistrado`. No event storming, ele aparece como `MovimentoDetectado`, `BotaoBemPressionado`, `SaidaAvisada`, `TelemetriaRecebida` ou `EventoReenviado`. | `(casaToken, seq, ts)` |

#### Agregado `DiaCritico`

| Item | Conteúdo |
|---|---|
| Raiz | `DiaCritico`, identificado pela `Data` de São Paulo. |
| Objetos de valor | `Probabilidade`, `OrigemDoDiaCritico {Modelo, RegraBase, Manual}`, `TurnoRadar`, `JanelaDeContato`, `ResumoDoTurno`. |
| Gravado | Data, probabilidade, motivo, origem, `AbertoEm`, `ToquePedidoEm`, `EncerradoEm` e o `ResumoDoTurno`. |
| Calculado | `Turno(parametros)` devolve o `TurnoRadar`. O turno não tem tabela. |

Invariantes:

1. Existe no máximo um `DiaCritico` por data.
2. A data é a de hoje, no fuso de São Paulo.
3. A probabilidade fica entre 0 e 1. O motivo tem de 1 a 200 caracteres.
4. Declarar de novo a mesma data atualiza probabilidade, motivo e origem. `AbertoEm` não muda, então a janela da manhã não se desloca.
5. Depois do encerramento, declarar de novo é recusado com `TURNO_ENCERRADO`. O turno encerrado não reabre.
6. O toque é pedido no máximo uma vez por dia.
7. Existe um encerramento por data. O resumo é gravado no encerramento e não muda. Encerrar de novo devolve o mesmo resumo.

| Comando | Efeito | Evento | Chave de idempotência |
|---|---|---|---|
| `DeclararDiaCritico(data, previsao, agora)` | Cria o dia crítico e abre o turno, ou atualiza a previsão. | `DiaCriticoDeclarado` | `data` |
| `PedirToque(agora)` | Grava `ToquePedidoEm`. | `ToquePedido` | `ToquePedidoEm` preenchido |
| `EncerrarTurno(resumo, agora)` | Grava `EncerradoEm` e o resumo. | `TurnoEncerrado` | `data` |

`TurnoRadar` tem `AbertoEm`, `FimPrevisto`, `EncerradoEm`, a janela da manhã e a janela da tarde. `EstaAberto(agora)` é verdadeiro quando o turno não foi encerrado e `agora` vem antes de `FimPrevisto`. A seção 5.2 diz como as janelas são cortadas.

`ResumoDoTurno` guarda cinco números: casas ativas, casas com sinal de vida até as 10h, casas atendidas, casas vermelhas pendentes e casas cinza pendentes. O segundo número é o indicador principal da ficha. Os eventos somem em 24 horas, então o resumo é o único lugar em que esse número sobrevive.

#### Serviços, políticas e processos do Motor

| Nome | Tipo | Faz |
|---|---|---|
| `AvaliadorDeCasas` | Serviço de domínio, puro | Aplica as regras da seção 5 e devolve a `Avaliacao`. Não lê banco nem relógio. |
| `RegraDeContato` | Classe abstrata | `Ordem`, `ExigeTurnoAberto`, `Avaliar(contexto)` e `AceitaConfirmacao(confirmacao)`. O método `Avaliar` confere o turno e só então chama `AvaliarNucleo(contexto)`, que cada regra implementa. As quatro regras da entrega herdam dela. |
| `TradutorDePacote` | Camada anticorrupção | Converte o JSON em `PacoteDoNo`. Recusa tipo desconhecido e campo faltando. Com o `RelogioDeDemo` deslocado, soma o mesmo deslocamento ao `ts`. |
| `RegistrarPacote` | Caso de uso | Confere a casa ativa e o `ts`, avança `UltimoPacoteEm`, cria o `EventoSensor` quando o tipo não é heartbeat e trata a repetição como sucesso. |
| `ConsumidorMqtt` | `BackgroundService`, assíncrono | Assina `corredor/v1/+/evento` e chama `RegistrarPacote`. Pacote de token desconhecido ou inativo é descartado sem gravar o payload no log. |
| `PublicadorDeComandos` | Infraestrutura | Publica em `corredor/v1/{casaToken}/comando`. |
| `AoDeclararDiaCritico` | Política P8 | Publica `DIA_CRITICO` para cada casa ativa. |
| `VigiaDoRadar` | `BackgroundService`, a cada `IntervaloDoVigia` | Avalia as casas ativas. Manda um `PedidoDeContato` pela porta `IReceptorDeContato` para cada vermelho pendente e para cada cinza pendente com turno aberto. Às 14h30, chama `PedirToque`, que publica `PEDIR_TOQUE` para as casas `Sozinho` sem sinal de vida desde as 10h. Às 18h, encerra o turno com o resumo se ninguém encerrou. |
| `Expurgo` | `BackgroundService`, a cada `IntervaloDoExpurgo` | Apaga eventos do sensor e confirmações com mais de 24 horas. Não apaga leitura de tensão abaixo de `TensaoMinima` de casa ativa e eletrodependente enquanto não existe confirmação `Acs` com `PessoaVista` depois dela, porque a trava da regra de energia depende dela. Apaga os eventos antes das confirmações, na mesma transação. |
| `MotorDeCasas` | Implementação de `IMotorDeCasas` | Recebe os quatro comandos do Cadastro. |
| `RelogioDeDemo` | `TimeProvider` | Em desenvolvimento, aplica o deslocamento de `PUT /api/demo/relogio`. Em produção, é o `TimeProvider.System`. |

#### Modelos de leitura do Motor

- **`Radar`.** Tem `TurnoAberto`, `Agora`, as listas `Pendentes`, `Atendidas` e `Aguardando`, e a contagem `Verdes`. A ordem de `Pendentes` e de `Atendidas` sai de uma consulta LINQ: `Vermelho` antes de `Cinza`, depois a `Ordem` da regra, depois o `Desde` mais antigo.
- **`CardDoRadar`.** Tem `CasaToken`, `Situacao`, `Motivo`, `Desde`, `Atendida`, `Perfil` e `UltimoPacoteEm`. O app troca o motivo por um texto, porque nenhuma informação pode depender só da cor.
- **`LinhaDoTempo`.** Tem os eventos do sensor e as confirmações de uma casa nas últimas 24 horas, em ordem de `OcorridoEm`, com a marca de bufferizado.

O `Radar` é calculado a cada `GET /api/radar`. O caso de uso carrega as casas ativas da microárea, os eventos do sensor e as confirmações das últimas 24 horas, as leituras de tensão baixa de qualquer idade, pelo mesmo critério do `Expurgo`, e o dia crítico de hoje. Ele monta um `HistoricoDaCasa` por token com uma consulta LINQ agrupada e avalia cada casa. Com 40 casas e sem heartbeat gravado, isso é uma consulta e um laço.

#### O que o Motor grava e o que calcula

| Gravado | Calculado |
|---|---|
| `casa_monitorada`, com `ultimo_pacote_em` | Situação, alerta, atendimento, motivo e `Desde` |
| `confirmacao`, por 24 horas | Sinal de vida, sequências de calor e de tensão baixa |
| `evento_sensor`, por 24 horas, sem heartbeat | Turno, janelas, janela atual |
| `dia_critico`, com o resumo | Radar, ordem, contagens, linha do tempo |

O Motor não grava alerta, aviso nem pedido de contato.

### 4.3 Cadastro

No desenho, veja que `Casa`, `Consentimento` e `VizinhoDeConfianca` vivem dentro do `Idoso`, e que `Visita` e `AvisoAoVizinho` apontam para ele só por identificador.

![Agregados do Cadastro](diagramas/18-agregados-cadastro.mmd)

#### Agregado `Idoso`

| Item | Conteúdo |
|---|---|
| Raiz | `Idoso`, identificado por `IdosoId`. |
| Entidades internas | `Casa`, com endereço e token. `Consentimento`, uma lista que só cresce. `VizinhoDeConfianca`, no máximo um. |
| Objetos de valor | `Autorizacoes(AvisarVizinho, AvisarFamilia)`, `PerfilDaCasa`, `CasaToken`, `MicroareaId`, `TextoRotulado` nos campos pessoais. |
| Gravado | Microárea, `Nome`, `Condicoes`, `Medicamentos`, perfil, endereço e token da casa, os consentimentos com quem colheu, a data, a versão do termo e a data de revogação, e o vizinho com nome, telefone e `AceitouEm`. |
| Calculado | `ConsentimentoVigente`, `TokenAtivo`, `PodeAvisarVizinho`, `DestinatariosDoAviso()`. |

Invariantes:

1. Sem consentimento vigente não há token. O token nasce dentro de `RegistrarConsentimento`.
2. Existe no máximo um consentimento vigente. O consentimento revogado fica guardado, com a data.
3. O consentimento registra a ACS que o colheu, a data e a versão do termo. A data não está no futuro.
4. O token tem 128 bits de `RandomNumberGenerator`. Ele nunca deriva de nome, endereço ou documento. Um consentimento novo gera um token novo.
5. Registrar o consentimento com um vigente devolve o mesmo token e repete a ativação no Motor.
6. O vizinho só é indicado com a autorização `AvisarVizinho` e com a data em que ele aceitou.
7. `DestinatariosDoAviso()` nunca inclui a família sem a autorização `AvisarFamilia`.
8. Revogar o consentimento apaga o token da `Casa` e remove o vizinho. O caso de uso apaga, na mesma transação, os avisos ao vizinho daquele token. As visitas ficam.
9. `Nome` e `Casa.Endereco` são obrigatórios. `Nome`, `Condicoes`, `Medicamentos`, `Casa.Endereco` e o nome e o telefone do vizinho têm o tipo `TextoRotulado`. O rótulo vem do campo, não do conteúdo.
10. O cadastro não tem CPF, RG, CNS nem data de nascimento. O produto não usa esses dados.

| Comando | Evento | Política | Chave de idempotência |
|---|---|---|---|
| `CadastrarIdoso(microarea, nome, endereco, perfil, condicoes, medicamentos, id?)` | `IdosoCadastrado` | nenhuma | nenhuma. A rota é de demo, no Swagger, e repetir cria outro idoso. Só a carga da demo, `Corredor.Api --semear`, informa o `id`, e repetir a carga pula o idoso que já existe. |
| `RegistrarConsentimento(acs, versaoDoTermo, autorizacoes, agora)` | `ConsentimentoRegistrado` | P1 | `IdosoId`. Com consentimento vigente, devolve o mesmo token. |
| `RevogarConsentimento(agora)` | `ConsentimentoRevogado` | P4 | `IdosoId`. Sem consentimento vigente, não faz nada. |
| `IndicarVizinho(nome, telefone, aceitouEm)` | `VizinhoDeConfiancaIndicado` | nenhuma | `IdosoId`. Indicar de novo substitui o vizinho. |
| `AlterarPerfil(perfil)` | `PerfilDaCasaAlterado` | P21 | `IdosoId`, o último valor vale. |

O Cadastro grava primeiro e chama o Motor depois. Os dois schemas não têm transação comum. Se a chamada falha, repetir o comando conserta, porque os métodos de `IMotorDeCasas` convergem. Ao iniciar, a `ReconciliacaoComMotor` reenvia `AtivarCasaAsync` para todo idoso com token.

#### Agregado `Visita`

| Item | Conteúdo |
|---|---|
| Raiz | `Visita`, identificada por um `Id` gerado no celular. Imutável. |
| Objetos de valor | `Desfecho {EstavaBem, EstavaFora, NaoAbriu, Encaminhada}`, `TextoRotulado` na observação. |
| Gravado | `Id`, `IdosoId`, `AcsId`, `OcorridaEm`, `RegistradaEm`, desfecho e observação. |

Invariantes:

1. Uma visita nunca é alterada nem apagada. Uma correção é outra visita.
2. Repetir o mesmo `Id` devolve a visita que já existe e chama o Motor de novo. A confirmação leva o mesmo `Id`, então o Motor ignora a repetição.
3. `OcorridaEm` não passa de `agora` mais `ToleranciaDoRelogio`. Antes de conferir, o caso de uso soma a `OcorridaEm` o deslocamento do relógio de demo, como o `TradutorDePacote` faz com o `ts`.
4. A visita só existe para um idoso com token ativo na hora do registro.
5. A observação é `TextoRotulado` com o rótulo `Saude`, que vem do campo, mais os rótulos que o `MotorDeParsing` achar. Lida do banco, ela volta com todos os rótulos de texto livre: `Saude`, `MoraSozinho`, `Nome`, `Endereco` e `Telefone`. CPF, RG, CNS e credencial já chegam mascarados pelo interceptador.
6. O expurgo de 24 horas e a revogação do consentimento não apagam visitas.
7. Se o Motor recusa a confirmação porque ela tem mais de 24 horas, o Cadastro mantém a visita e responde 201.

`TradutorDeDesfecho` é um dicionário com a tabela abaixo. Ele é o único lugar que decide se um desfecho vira confirmação no Motor.

| Desfecho | A pessoa foi vista? | Chamada ao Motor | Efeito no radar |
|---|---|---|---|
| `EstavaBem` | Sim | `ConfirmarAsync` com `Acs` e `PessoaVista` | Sinal de vida. Atende o alerta de calor ou de cinza, e o de energia, na janela atual. |
| `Encaminhada` | Sim | `ConfirmarAsync` com `Acs` e `PessoaVista` | Igual a `EstavaBem`. |
| `EstavaFora` | Não | `ConfirmarAsync` com `Acs` e `AusenciaConfirmada` | Atende só o alerta de silêncio, e só na janela atual. Nunca é sinal de vida. |
| `NaoAbriu` | Não | Nenhuma | A casa continua vermelha e pendente. O app mostra "SAMU chamado". |

| Comando | Evento | Política | Chave de idempotência |
|---|---|---|---|
| `RegistrarVisita(id, idoso, acs, desfecho, observacao, ocorridaEm, agora)` | `VisitaRegistrada` | P11 | `Visita.Id` |

#### Agregado `MensagemSaida`

| Item | Conteúdo |
|---|---|
| Raiz | `MensagemSaida`, identificada por `Guid`. |
| Objetos de valor | `Canal`, `Veredito`, a lista de `TipoDeDado` achados. |
| Gravado | `Id`, canal, `AcsId`, texto mascarado, veredito, tipos de dado achados, `VerificadaEm`. |

Invariantes:

1. A mensagem nasce com o veredito. Não existe mensagem pendente.
2. O veredito é `Bloqueada` se, e somente se, a `SaidaSegura` achou um tipo de dado que o canal não aceita.
3. O texto original nunca é gravado. O agregado guarda só o texto que `TextoRotulado.ToString()` devolve mascarado.
4. O veredito não muda. Uma nova tentativa é uma nova `MensagemSaida`.
5. A resposta de uma mensagem bloqueada traz a frase fixa do tipo de dado e a versão segura: "Preciso de ajuda para uma visita agora. Quem puder, me chame no privado."

| Comando | Evento | Chave de idempotência |
|---|---|---|
| `VerificarMensagem(texto, canal, saidaSegura, agora)` | `MensagemLiberada` ou `MensagemBloqueada` | nenhuma. Cada verificação é uma tentativa. |

#### Agregado `AvisoAoVizinho`

| Item | Conteúdo |
|---|---|
| Raiz | `AvisoAoVizinho`, identificado por `Guid`. |
| Objetos de valor | `MotivoDoAlerta`, `RespostaDoVizinho {EstaBem, PrecisaDeAjuda}`. |
| Gravado | `Id`, `CasaToken`, `MicroareaId`, motivo, `Desde`, `RegistradoEm`, resposta e `RespondidoEm`. |

Invariantes:

1. Existe no máximo um aviso por `(CasaToken, Motivo, Desde)`. Esse trio é o episódio.
2. O aviso só existe se `Idoso.PodeAvisarVizinho`: autorização `AvisarVizinho` e vizinho indicado.
3. O aviso recebe uma resposta. A mesma resposta repetida não muda nada. Uma resposta diferente é recusada com `AVISO_JA_RESPONDIDO`.
4. O texto que sai é fixo e neutro, sem nome, sem endereço e sem motivo de saúde: "A UBS pede que você veja agora se está tudo bem com a pessoa que você acompanha. Depois, responda aqui." Ele passa pela `SaidaSegura` com o canal `Vizinho`.

| Comando | Evento | Política | Chave de idempotência |
|---|---|---|---|
| `RegistrarAviso(idoso, pedido, agora)` | `AvisoAoVizinhoRegistrado` | P19 | `(casaToken, motivo, desde)` |
| `RegistrarResposta(resposta, agora)` | `VizinhoRespondeu` | P20 | `AvisoAoVizinho.Id` |

#### Referência, políticas e leitura do Cadastro

`Microarea` tem `Id`, `Nome` e `AcsResponsavel`. Ela entra por carga inicial e não tem comando na entrega.

`AcsId` é um código de texto no formato `^[a-z0-9-]{3,40}$`, como `acs-demo-01`, que vem da autenticação. Não existe tabela de ACS, e o banco não guarda credencial.

| Nome | Tipo | Faz |
|---|---|---|
| P1 | Política | Depois de `ConsentimentoRegistrado`, chama `AtivarCasaAsync` com token, microárea e perfil. |
| P4 | Política | Depois de `ConsentimentoRevogado`, chama `DesativarCasaAsync`. |
| P11 | Política | Depois de `VisitaRegistrada`, consulta o `TradutorDeDesfecho` e chama `ConfirmarAsync` quando a tabela manda. O `Id` da confirmação é o `Id` da visita. `Quando` é `OcorridaEm`. |
| P19 | Política, em `ReceptorDeContato` | Implementa `IReceptorDeContato`. Acha o idoso pelo token. Se ele pode avisar o vizinho e o episódio é novo, cria o `AvisoAoVizinho` e grava o envio simulado. O envio simulado é a própria linha do aviso e uma linha de log sem telefone. Se o episódio já existe, não faz nada. |
| P20 | Política | Depois de `VizinhoRespondeu` com `EstaBem`, chama `ConfirmarAsync` com `Vizinho`, `PessoaVista` e `RespondidoEm`. O `Id` da confirmação é o `Id` do aviso. `PrecisaDeAjuda` não chama o Motor. |
| P21 | Política | Depois de `PerfilDaCasaAlterado` com token ativo, chama `DefinirPerfilAsync`. |
| P22 | `ReconciliacaoComMotor`, `IHostedService` | Ao iniciar, reenvia `AtivarCasaAsync` para todo idoso com token. |
| `ListaDeNomes` | Modelo de leitura | `CasaToken`, nome, endereço e o nome e o telefone do vizinho, só de idoso com token ativo. Passa pela `SaidaSegura` com o canal `AppAutenticado`. Condições e medicamentos não vão. |
| `AvisosDoDia` | Modelo de leitura | Os avisos das últimas 24 horas da microárea, com token, motivo, `Desde`, resposta e hora da resposta. O app junta com o radar pelo token. |

#### O que o Cadastro grava e o que calcula

| Gravado | Calculado |
|---|---|
| `microarea`, `idoso` com as colunas da casa, `consentimento`, `vizinho_de_confianca` | Token ativo, consentimento vigente, destinatários do aviso |
| `visita`, sem prazo | `ListaDeNomes`, `AvisosDoDia` |
| `mensagem_saida`, só com texto mascarado | |
| `aviso_ao_vizinho` | |

### 4.4 Guarda

A Guarda segue a tabela de arquivos de [01-visao-geral.md](01-visao-geral.md#arquivos-da-guarda). Esta seção dá os nomes de domínio e as invariantes.

No desenho, veja que o `TextoRotulado` é o ponto em que o `MotorDeParsing` e a `SaidaSegura` se encontram, e que o `EventoDeSeguranca` é o único agregado.

![Agregado, objetos de valor e serviços da Guarda](diagramas/19-agregados-guarda.mmd)

#### Agregado `EventoDeSeguranca`

| Item | Conteúdo |
|---|---|
| Raiz | `EventoDeSeguranca`, identificado por `Guid`. |
| Objetos de valor | `TipoDeEventoDeSeguranca {MascaramentoNaEntrada, BloqueioDeSaida}`, `TipoDeDado`, `Canal`, `MensagemAoCidadao(Texto, Origem {Llm, FraseFixa})`. |
| Gravado | `Id`, `OcorridoEm`, tipo, tipos de dado, canal, módulo, rota, severidade e a mensagem ao cidadão. A rota é o modelo, como `/api/idosos/{id}/vizinho`, nunca o caminho com valores, e fica nula fora de uma requisição. |
| Calculado | `Pontuacao`, que é severidade vezes a exposição do canal. |

Invariantes:

1. O evento nunca guarda o valor original nem um trecho do texto. Ele guarda o tipo de dado, o canal e o módulo.
2. A lista de tipos de dado não é vazia. A severidade é a maior entre os tipos achados, de 1 a 5.
3. A mensagem ao cidadão é anexada uma vez. Sem resposta do LLM em 5 segundos, entra a frase fixa do tipo de evento.
4. O evento não identifica a ACS.
5. A aplicação não altera nem apaga o evento.

| Comando | Evento | Chave de idempotência |
|---|---|---|
| `Registrar(tipo, canal, achados, modulo, rota, agora)` | `PiiMascaradaNaEntrada` ou `SaidaBloqueada` | nenhuma. Cada evento de segurança é um fato novo. |
| `AnexarMensagem(texto, origem)` | `BloqueioTraduzido` | a mensagem preenchida |

#### Objetos de valor e tabelas da Guarda

| Nome | O que é | Regra |
|---|---|---|
| `TipoDeDado` | Enum: `Cpf`, `Rg`, `Cns`, `Saude`, `MoraSozinho`, `Endereco`, `Telefone`, `Nome`, `Credencial`. | É o rótulo do taint. `Saude` cobre CID-10, laudo e nome de remédio. |
| `RegraDePii` | Tipo, padrão, validador, máscara e severidade de 1 a 5. | Vem de `Regras/pii.json`. A regra de CPF mascara pelo formato. O dígito verificador válido só sobe a severidade. O exemplo `123.456.789-00` tem dígito inválido e é mascarado. |
| `Achado` | Tipo, campo, valor mascarado e severidade. | Nunca tem o valor em claro. |
| `TextoRotulado` | Um texto e o conjunto de `TipoDeDado` que ele carrega. | O valor é `internal`. Ele só sai pela `SaidaSegura` ou pelo conversor do EF Core, que é a `SaidaSegura` com o canal `Banco`. Juntar dois textos une os rótulos. `ToString()` devolve a máscara. |
| `Canal` | Enum: `Log`, `RespostaSemAutenticacao`, `MensagemDeErro`, `Mqtt`, `GrupoDoBairro`, `PromptDoLlm`, `Vizinho`, `AppAutenticado`, `Banco`. | |
| `PoliticaDeCanal` | Para cada canal, os tipos de dado aceitos e a exposição de 1 a 3. | Fica num dicionário, não numa sequência de `if`. |
| `Veredito` | `Liberada` com o texto, ou `Bloqueada` com os achados. | É o retorno da `SaidaSegura`. |
| `CorDoFarol` | Enum: `Verde`, `Amarelo`, `Vermelho`. | Calculado. |

Severidades em `pii.json`: CPF 5 com dígito válido e 4 só no formato, CNS 5, credencial 5, saúde 4, mora sozinho 4, RG 4, endereço 3, telefone 3, nome 3.

`PoliticaDeCanal` na entrega:

| Canal | Aceita | Exposição |
|---|---|---|
| `GrupoDoBairro` | nenhum rótulo | 3 |
| `PromptDoLlm` | nenhum rótulo | 3 |
| `Mqtt` | nenhum rótulo | 3 |
| `MensagemDeErro` | nenhum rótulo | 3 |
| `RespostaSemAutenticacao` | nenhum rótulo | 3 |
| `Vizinho` | nenhum rótulo | 3 |
| `Log` | nenhum rótulo | 2 |
| `AppAutenticado` | `Nome`, `Endereco`, `Telefone` | 1 |
| `Banco` | todos | 1 |

#### Serviços da Guarda

| Nome | Faz | Invariante |
|---|---|---|
| `MotorDeParsing` | Aplica as `RegraDePii` a um texto ou a um JSON e devolve os achados e o `TextoRotulado`. | Puro. Não grava nada. |
| `InterceptadorDeEntrada` | Middleware. Lê o corpo de toda requisição. | Mascara CPF, RG, CNS e credencial em qualquer campo, antes do log e do banco, e registra `PiiMascaradaNaEntrada` com o canal `Banco`. A regra de credencial casa com o nome inteiro da chave, como `senha` ou `apiKey`, e nunca mascara `casaToken`. Os outros tipos de dado não mudam o corpo. Eles viram rótulo no `TextoRotulado`. Nome e endereço do `POST /api/idosos` são gravados inteiros. |
| `SaidaSegura` | Recebe um `TextoRotulado` e um `Canal`, consulta a `PoliticaDeCanal` e devolve o `Veredito`. | Todo bloqueio registra `SaidaBloqueada`. |
| `ProvedorDeLogSeguro` | Decorador de `ILoggerProvider`. | Um argumento `TextoRotulado` com rótulo vai ao log como máscara e registra `SaidaBloqueada` no canal `Log`. |
| `FiltroDeResposta` | Filtro de endpoint. | Bloqueia `TextoRotulado` com rótulo em rota sem autenticação. |
| `TratadorDeErros` | Middleware. | Troca a mensagem de exceção com dado pessoal por texto neutro e converte `RegraDeDominioViolada` em 422 ou 409. |
| `Tradutor` | Monta o prompt com tipo de dado, canal e módulo e chama o LLM. | O prompt passa pela `SaidaSegura` com o canal `PromptDoLlm`. Roda depois da resposta HTTP, numa fila em memória lida por um `BackgroundService`. Com falha, anexa a frase fixa. |
| `Farol` | Calcula a `CorDoFarol` sobre os eventos das últimas 24 horas. | Função pura. |

O desenho mostra os serviços numa saída bloqueada. Veja que a resposta 200 sai no passo 9, antes de o `Tradutor` chamar o LLM.

![Fluxo da Guarda numa saída bloqueada](diagramas/28-fluxo-da-guarda.mmd)

Regra do farol: `Vermelho` se existe uma `SaidaBloqueada` de severidade 4 ou 5 nas últimas 24 horas. `Amarelo` se existe qualquer outro evento. `Verde` se não existe evento. Só uma saída bloqueada deixa o farol vermelho. Um mascaramento na entrada vai até amarelo, porque mostra o sistema funcionando, não uma tentativa de saída.

Na Guarda, os eventos de segurança e o `pii.json` são gravados. Achados, rótulos, veredito, pontuação e cor do farol são calculados.

### 4.5 Chave de idempotência de cada comando

| Contexto | Comando | Chave que torna a repetição inofensiva |
|---|---|---|
| Motor | `RegistrarPacote` | `(casaToken, seq, ts)` |
| Motor | `AtivarCasa` | `casaToken`. Ativa: atualiza o perfil. Desativada: recusa. |
| Motor | `DefinirPerfil` | `casaToken`, o último valor vale |
| Motor | `Confirmar` | `Confirmacao.Id` |
| Motor | `DesativarCasa` | `casaToken` |
| Motor | `DeclararDiaCritico` | `data` |
| Motor | `PedirToque` | `ToquePedidoEm` preenchido |
| Motor | `EncerrarTurno` | `data` |
| Motor | `Expurgo` | apaga por data. Rodar duas vezes não muda nada. |
| Cadastro | `RegistrarConsentimento` | `IdosoId`, com o consentimento vigente |
| Cadastro | `RevogarConsentimento` | `IdosoId` |
| Cadastro | `IndicarVizinho`, `AlterarPerfil` | `IdosoId`, o último valor vale |
| Cadastro | `RegistrarVisita` | `Visita.Id`, gerado no celular |
| Cadastro | Pedido de contato recebido | `(casaToken, motivo, desde)` |
| Cadastro | `RegistrarResposta` | `AvisoAoVizinho.Id` |
| Cadastro | `CadastrarIdoso`, `VerificarMensagem` | nenhuma. Cada chamada é um fato novo. |
| Guarda | `Registrar` evento de segurança | nenhuma. Cada evento de segurança é um fato novo. |

## 5. Regras da situação da casa

A situação da casa não é uma coluna. Ela muda com a passagem do tempo, sem evento, e uma coluna velha poderia mostrar um verde que ninguém provou. O `AvaliadorDeCasas` calcula a situação a cada leitura.

### 5.1 Entradas da avaliação

`AvaliadorDeCasas.Avaliar(casa, historico, turno, agora, parametros)` é uma função pura. Ela recebe:

- A `CasaMonitorada`: perfil, `AtivadaEm`, `UltimoPacoteEm` e confirmações.
- O `HistoricoDaCasa`: os eventos do sensor e as confirmações das últimas 24 horas, mais as leituras de tensão baixa de qualquer idade, pelo mesmo critério do `Expurgo`. Ele responde a `TemSinalDeVidaDesde(instante)`, `UltimaTensao`, `InicioDaSequenciaBaixa`, `PrimeiraLeituraBaixaSemConfirmacao` e `InicioDaSequenciaQuente`.
- O `TurnoRadar` de hoje, que pode não existir.
- O instante `agora`.
- Os `ParametrosDoMotor`.

### 5.2 Janelas de contato

O Motor corta o dia de São Paulo em janelas. Os cortes são 00h, a abertura do turno quando existe turno hoje, 10h e 15h. Cada pedaço entre dois cortes é uma `JanelaDeContato`. A janela atual é a que contém `agora`.

Dentro do turno, duas janelas têm prazo:

- A janela da manhã vai da abertura às 10h. O prazo é 10h. Ela só existe se a abertura vem antes das 10h.
- A janela da tarde vai das 10h, ou da abertura se ela vem depois, às 15h. O prazo é 15h. Ela só existe se a abertura vem antes das 15h.

As outras janelas não têm prazo. Elas só limitam a validade de um sinal de vida e de uma confirmação. Num dia crítico aberto às 05h, as janelas são de 00h a 05h, a manhã de 05h a 10h, a tarde de 10h a 15h e de 15h a 24h. Num dia comum, as janelas são de 00h a 10h, de 10h a 15h e de 15h a 24h.

Uma janela com prazo vence quando `agora` chega ao prazo. Para o perfil `Acamado`, a janela vence no início, porque ele não passa no corredor e entra direto na lista.

### 5.3 Sinal de vida por perfil de contato

| Perfil de contato | Conta como sinal de vida |
|---|---|
| `Sozinho` | Evento `Movimento`, `BotaoBem` ou `BotaoSair`, e confirmação com `PessoaVista` |
| `Acamado` | Só confirmação com `PessoaVista` |
| `Coabitado` | Só confirmação com `PessoaVista` |

O evento do sensor conta pelo `OcorridoEm`. A confirmação conta pelo `Quando`. Só conta o que aconteceu até `agora`. `AusenciaConfirmada` nunca é sinal de vida. Na casa `Acamado` e na casa `Coabitado`, outra pessoa pode passar no corredor ou apertar o botão, então nenhum evento do sensor conta.

### 5.4 As regras em ordem

`RegraDeContato` é uma classe abstrata. A primeira regra que devolve um alerta define a situação, o motivo e o `Desde`. A ordem é a da tabela, e o avaliador não conhece nenhuma classe concreta.

| Ordem | Classe | Exige turno aberto | Devolve alerta quando | Situação | `Desde` | Confirmação que atende |
|---|---|---|---|---|---|---|
| 1 | `EnergiaEletrodependente` | Não | A casa é eletrodependente, já mandou um pacote e vale uma destas condições, nesta ordem. (a) A última leitura de tensão está abaixo de `TensaoMinima`. (b) Existe leitura abaixo de `TensaoMinima` sem confirmação da ACS com `PessoaVista` depois dela. (c) Nenhum pacote chegou dentro de `SilencioMaximoEletrodependente`. | `Vermelho` | (a) a primeira leitura da sequência baixa atual. (b) a primeira leitura baixa sem confirmação depois dela. (c) `UltimoPacoteEm`. | Só `Acs` com `PessoaVista` |
| 2 | `SemHeartbeat` | Não | A casa nunca mandou pacote, ou nenhum pacote chegou dentro de `SilencioMaximo`. | `Cinza` | `UltimoPacoteEm`, ou `AtivadaEm` se nunca chegou pacote | `Acs` ou `Vizinho`, com `PessoaVista` |
| 3 | `CalorInterno` | Sim | As leituras de temperatura estão em `TemperaturaDeCalor` ou mais, sem interrupção, há `DuracaoDoCalor` ou mais. | `Vermelho` | O início da sequência quente mais `DuracaoDoCalor` | `Acs` ou `Vizinho`, com `PessoaVista` |
| 4 | `SemMovimentoNaJanela` | Sim | O perfil é `Sozinho` ou `Acamado`, não houve saída avisada no turno, e não existe sinal de vida desde o início da última janela vencida. | `Vermelho` | O prazo da janela vencida, ou o início dela para o `Acamado` | `Acs` com `AusenciaConfirmada`. A `PessoaVista` é sinal de vida e desfaz o alerta. |
| 5 | `IdaNoturnaSemVolta` | | Fora da entrega. | | | |

O desenho mostra a mesma ordem como uma escada. Leia da esquerda para a direita. A primeira pergunta com resposta sim decide a situação, e cada alerta traz a confirmação que o atende. A regra 4 só vale para o perfil `Sozinho` ou `Acamado`, sem saída avisada.

![Regras da situação da casa, em ordem](diagramas/06-situacao-da-casa.mmd)

Detalhes que evitam erro:

- A regra 2 vem antes das regras 3 e 4. Essa ordem impede o aparelho mudo de virar verde. O `UltimoPacoteEm` é hora do servidor, então um lote do buffer com horas antigas mostra que o aparelho está ligado agora.
- A condição (b) da regra 1 é a trava. A luz pode ter voltado, mas o concentrador ficou parado. A casa fica vermelha até a ACS ver a pessoa depois da última leitura baixa.
- A regra 4 olha a última janela vencida e aceita sinal de vida desde o início dela, mesmo depois do prazo. Às 10h30, um movimento às 10h20 tira o vermelho da manhã.
- A saída avisada vale para o perfil `Sozinho` e suspende a regra 4 até o fim do turno. Ela não suspende as regras 1, 2 e 3.
- As regras 1 e 3 usam a mesma função, "início da sequência atual em que a condição vale". Ela é escrita uma vez, em `HistoricoDaCasa`.
- `RegraDeContato.AceitaConfirmacao(confirmacao)` é virtual. Cada regra sobrescreve com a coluna "Confirmação que atende".

### 5.5 Passos do avaliador

1. Percorrer as regras na ordem. A primeira que devolve um alerta define situação, motivo e `Desde`.
2. Com alerta, a casa fica atendida se existe uma confirmação que a regra aceita, com `Quando` igual ou posterior ao `Desde` e igual ou posterior ao início da janela atual. A situação não muda.
3. Sem alerta, com turno aberto e com sinal de vida desde o início da janela atual, a situação é `Verde`.
4. Sem alerta, em qualquer outro caso, a situação é `Aguardando`. Isso inclui o dia comum, o turno encerrado, a casa `Coabitado` sem confirmação e a casa que ainda está no prazo.

A janela atual limita a confirmação. Uma visita às 09h atende só até as 10h. Na janela seguinte, a casa volta à lista se a condição continua. Isso vale para o silêncio, o cinza, o calor, a energia e o acamado. Uma visita não cobre o turno inteiro.

### 5.6 Tabela verdade da situação

R1 a R4 são as regras 1 a 4 da seção 5.4. "Sim" quer dizer que a regra devolve alerta. "Sinal de vida" quer dizer sinal de vida aceito pelo perfil desde o início da janela atual.

| Linha | Turno aberto | R1 | R2 | R3 | R4 | Sinal de vida na janela atual | Situação | Motivo |
|---|---|---|---|---|---|---|---|---|
| 1 | qualquer | sim | qualquer | qualquer | qualquer | qualquer | `Vermelho` | `EnergiaEletrodependente` |
| 2 | qualquer | não | sim | qualquer | qualquer | qualquer | `Cinza` | `SemHeartbeat` |
| 3 | sim | não | não | sim | qualquer | qualquer | `Vermelho` | `CalorInterno` |
| 4 | sim | não | não | não | sim | não, sempre | `Vermelho` | `SemMovimentoNaJanela` |
| 5 | sim | não | não | não | não | sim | `Verde` | nenhum |
| 6 | sim | não | não | não | não | não | `Aguardando` | nenhum |
| 7 | não | não | não | não se aplica | não se aplica | qualquer | `Aguardando` | nenhum |

Na linha 4, o sinal de vida é sempre "não". A janela atual começa no início da última janela vencida ou depois dele. Se existisse sinal de vida na janela atual, ele estaria também depois do início da janela vencida, e a regra 4 não devolveria alerta.

Na linha 7, as regras 3 e 4 exigem turno aberto, então não devolvem alerta. Fora do turno não existe `Verde`.

`Verde` aparece só na linha 5, que exige turno aberto, nenhuma regra com alerta e sinal de vida na janela atual.

### 5.7 Tabela verdade do atendimento

A confirmação só conta se o `Quando` vem igual ou depois do `Desde` e igual ou depois do início da janela atual. Fora disso, a casa fica pendente.

| Motivo | Origem | Resultado | Atendida |
|---|---|---|---|
| `EnergiaEletrodependente` | `Acs` | `PessoaVista` | sim |
| `EnergiaEletrodependente` | `Vizinho` | `PessoaVista` | não |
| `EnergiaEletrodependente` | `Acs` | `AusenciaConfirmada` | não |
| `SemHeartbeat` | `Acs` ou `Vizinho` | `PessoaVista` | sim |
| `SemHeartbeat` | `Acs` | `AusenciaConfirmada` | não |
| `CalorInterno` | `Acs` ou `Vizinho` | `PessoaVista` | sim |
| `CalorInterno` | `Acs` | `AusenciaConfirmada` | não |
| `SemMovimentoNaJanela` | `Acs` | `AusenciaConfirmada` | sim |
| `SemMovimentoNaJanela` | `Acs` ou `Vizinho` | `PessoaVista` | não se aplica. É sinal de vida, e a regra deixa de devolver alerta. |

O vizinho nunca manda `AusenciaConfirmada`. A resposta `PrecisaDeAjuda` não chama o Motor. Uma casa atendida continua `Vermelho` ou `Cinza`. Ela sai de `Pendentes`, entra em `Atendidas` e nunca conta como verde.

### 5.8 Exemplos

Casa `Sozinho`, turno aberto às 05h:

| Agora | Último sinal de vida | Situação |
|---|---|---|
| 08h00 | nenhum | `Aguardando` |
| 08h00 | 06h12 | `Verde` |
| 10h00 | nenhum | `Vermelho` desde 10h00 |
| 10h01 | 09h50 | `Aguardando` |
| 10h31 | 10h30 | `Verde` |
| 12h00 | 06h12 | `Aguardando` |
| 15h00 | 06h12 | `Vermelho` desde 15h00 |
| 15h01 | 10h30 | `Aguardando` |
| 15h21 | 15h20 | `Verde` |
| 18h30 | 15h20 | `Aguardando`, turno encerrado |

Casa `Acamado`, turno aberto às 05h:

| Agora | Confirmação com `PessoaVista` | Situação |
|---|---|---|
| 05h01 | nenhuma | `Vermelho` desde 05h00 |
| 09h01 | 09h00 | `Verde` |
| 10h00 | 09h00 | `Vermelho` desde 10h00 |
| 11h01 | 11h00 | `Verde` |
| 15h01 | 11h00 | `Aguardando` |

### 5.9 Parâmetros e relógio de demo

`ParametrosDoMotor` vem do `appsettings.json` por `IOptions`. Ele é o único lugar dos números das regras.

| Parâmetro | Produção | Demo |
|---|---|---|
| `IntervaloDoHeartbeat` | 20 s | 20 s |
| `SilencioMaximo` | 30 min | 1 min |
| `SilencioMaximoEletrodependente` | 60 s | 60 s |
| `TensaoMinima` | 100 V | 100 V |
| `TemperaturaDeCalor` | 34 °C | 34 °C |
| `DuracaoDoCalor` | 2 h | 2 min |
| `FimDaManha` | 10h00 | 10h00 |
| `FimDaTarde` | 15h00 | 15h00 |
| `HoraDoPedidoDeToque` | 14h30 | 14h30 |
| `FimDoTurno` | 18h00 | 18h00 |
| `Retencao` | 24 h | 24 h |
| `ToleranciaDoRelogio`, em `ParametrosDoRelogio` | 2 min | 2 min |
| `IntervaloDoVigia` | 15 s | 5 s |
| `IntervaloDoExpurgo` | 1 h | 1 h |

`IntervaloDoHeartbeat` é a cadência do nó. O Motor só usa esse valor para conferir, ao iniciar, que `SilencioMaximoEletrodependente` é pelo menos o dobro dele.

Os horários das janelas não mudam na demo. Quem grava o vídeo move a hora do Motor com o `RelogioDeDemo`. `PUT /api/demo/relogio` recebe a hora desejada e guarda o deslocamento. O `TradutorDePacote` soma o mesmo deslocamento ao `ts` de cada pacote, então o nó do Wokwi e o simulador continuam coerentes com o Motor. O `POST /visitas` soma o mesmo deslocamento a `ocorridaEm`, então a visita registrada com o relógio movido cai na janela certa. O Cadastro lê o deslocamento por `IDeslocamentoDoRelogio`, de `Corredor.Contratos`. Para mover o relógio para trás, rode o reinício da seção 8.4 de [04-modelo-de-dados.md](04-modelo-de-dados.md), porque `UltimoPacoteEm` só anda para frente. A rota só existe no ambiente `Development`.

Com esses valores, o eletrodependente mudo fica vermelho no radar 60 segundos depois do último pacote. O pedido de contato sai em até 60 segundos mais `IntervaloDoVigia`.

## 6. Contratos públicos

### 6.1 `Corredor.Contratos`

```csharp
namespace Corredor.Contratos;

public readonly record struct CasaToken
{
	public string Valor { get; }
	public static bool TryParse(string? texto, out CasaToken token);
	public static CasaToken Novo();
}

public readonly record struct MicroareaId(int Valor);

public enum PerfilDeContato { Sozinho, Acamado, Coabitado }

public sealed record PerfilDaCasa(PerfilDeContato Contato, bool Eletrodependente);

public enum OrigemDaConfirmacao { Acs, Vizinho }

public enum ResultadoDaConfirmacao { PessoaVista, AusenciaConfirmada }

public sealed record Confirmacao(
	Guid Id,
	OrigemDaConfirmacao Origem,
	ResultadoDaConfirmacao Resultado,
	DateTimeOffset Quando);

public enum MotivoDoAlerta { EnergiaEletrodependente, SemHeartbeat, CalorInterno, SemMovimentoNaJanela }

public sealed record PedidoDeContato(CasaToken Token, MotivoDoAlerta Motivo, DateTimeOffset Desde);

public interface IMotorDeCasas
{
	Task AtivarCasaAsync(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, CancellationToken ct);
	Task DefinirPerfilAsync(CasaToken token, PerfilDaCasa perfil, CancellationToken ct);
	Task ConfirmarAsync(CasaToken token, Confirmacao confirmacao, CancellationToken ct);
	Task DesativarCasaAsync(CasaToken token, CancellationToken ct);
}

public interface IReceptorDeContato
{
	Task PedirContatoAsync(PedidoDeContato pedido, CancellationToken ct);
}

public sealed record ParametrosDoRelogio(TimeSpan ToleranciaDoRelogio);

public interface IDeslocamentoDoRelogio
{
	TimeSpan Deslocamento { get; }
}
```

- `CasaToken.TryParse` aceita 32 caracteres hexadecimais minúsculos. `CasaToken.Novo` usa 128 bits de `RandomNumberGenerator`.
- `AtivarCasaAsync` cria a casa. Com a casa ativa, atualiza o perfil. Com a casa desativada, recusa com `CASA_DESATIVADA`.
- `DefinirPerfilAsync` exige casa ativa. Casa desconhecida recusa com `CASA_DESCONHECIDA`.
- `ConfirmarAsync` ignora `Id` repetido. O `Id` da ACS é o `Id` da visita. O `Id` do vizinho é o `Id` do aviso.
- `DesativarCasaAsync` grava a desativação e apaga eventos e confirmações. Chamar duas vezes dá o mesmo resultado.
- `IReceptorDeContato` é declarado pelo Motor e implementado pelo Cadastro. O Motor chama a porta a cada ciclo do `VigiaDoRadar`, para cada alerta pendente. O Cadastro descarta a repetição pela chave `(casaToken, motivo, desde)`.
- `ParametrosDoRelogio` vem da seção `Relogio` do `appsettings.json`. O Motor e o Cadastro leem o mesmo valor.
- `IDeslocamentoDoRelogio` é implementado pelo `RelogioDeDemo` em `Development`. Em produção, o host registra uma implementação que devolve zero. O Cadastro soma o deslocamento a `ocorridaEm` em `POST /visitas`.
- Nenhum tipo carrega nome, endereço ou texto livre.

### 6.2 MQTT

O contrato v1 não muda. Tópico de evento: `corredor/v1/{casaToken}/evento`. Tópico de comando: `corredor/v1/{casaToken}/comando`.

```json
{
	"seq": 1842,
	"ts": "2026-11-18T09:12:40Z",
	"tipo": "MOVIMENTO",
	"tempC": 31.5,
	"tensaoV": 126.8,
	"bufferizado": false
}
```

- `tipo` aceita `MOVIMENTO`, `BOTAO_BEM`, `BOTAO_SAIR`, `TELEMETRIA` e `HEARTBEAT`.
- A chave única é `(casaToken, seq, ts)`. Um reenvio depois da queda do broker não duplica nada. Um `seq` que volta a 1 depois do reinício do ESP32 chega com outro `ts` e é aceito.
- `ts` é a hora do fato, em UTC. O Motor recusa `ts` mais de 2 minutos à frente da chegada ou com mais de 24 horas. `bufferizado: true` marca o evento que ficou guardado no nó.
- O Motor lê `tempC` e `tensaoV` dos eventos `TELEMETRIA`. O `HEARTBEAT` só avança `UltimoPacoteEm`.
- Cadência do nó: `HEARTBEAT` a cada 20 segundos. `TELEMETRIA` a cada 5 minutos e na hora em que a tensão cruza 100 V. As duas usam os tipos e os campos que o contrato já tem.
- Comandos: `{"tipo": "DIA_CRITICO"}` acende o LED âmbar e sai quando o dia crítico é declarado. `{"tipo": "PEDIR_TOQUE"}` toca o buzzer e sai às 14h30. Os comandos não são retidos no broker.

### 6.3 Rotas HTTP

Todas as rotas ficam sob `/api` e aparecem no Swagger. As horas saem em ISO 8601 com o fuso de São Paulo, `-03:00`. O host confere se a microárea da rota é a da ACS autenticada. Erros de domínio saem com `{"codigo": "...", "mensagem": "..."}`, sem dado pessoal.

| Rota | Módulo | Quem chama | Sucesso | Erros |
|---|---|---|---|---|
| `PUT /dias-criticos/{data}` | Motor | job de ML | 200 | 422 `DATA_NAO_E_HOJE`, 422 `PROBABILIDADE_FORA_DA_FAIXA`, 409 `TURNO_ENCERRADO` |
| `POST /dias-criticos/{data}/encerramento` | Motor | app | 200 | 404 `DIA_CRITICO_DESCONHECIDO` |
| `GET /radar?microarea={id}` | Motor | app | 200 | |
| `GET /casas/{token}/linha-do-tempo` | Motor | app | 200 | 404 `CASA_DESCONHECIDA`, também para casa desativada |
| `PUT /demo/relogio` | Motor | equipe, só em `Development` | 200 | |
| `POST /idosos` | Cadastro | Swagger | 201 | 422 `NOME_OBRIGATORIO`, 422 `ENDERECO_OBRIGATORIO` |
| `POST /idosos/{id}/consentimento` | Cadastro | Swagger | 200 | 503 `MOTOR_INDISPONIVEL` |
| `DELETE /idosos/{id}/consentimento` | Cadastro | Swagger | 204 | |
| `PUT /idosos/{id}/vizinho` | Cadastro | Swagger | 204 | 422 `AUTORIZACAO_AUSENTE` |
| `PUT /idosos/{id}/perfil` | Cadastro | Swagger | 204 | |
| `GET /lista-de-nomes?microarea={id}` | Cadastro | app | 200 | |
| `POST /visitas` | Cadastro | app | 201, ou 200 na repetição | 422 `TOKEN_INATIVO`, 422 `VISITA_NO_FUTURO`, 503 `MOTOR_INDISPONIVEL` |
| `POST /mensagens/verificar` | Cadastro | app | 200 | |
| `GET /avisos?microarea={id}` | Cadastro | app | 200 | |
| `POST /avisos/{id}/resposta` | Cadastro | canal simulado do vizinho, pelo Swagger | 200 | 409 `AVISO_JA_RESPONDIDO` |
| `GET /painel` | Guarda | painel Risco Cidadão | 200 | |

A página HTML do painel fica em `/painel`, servida pelo Nginx, e lê `GET /api/painel`.

`PUT /api/dias-criticos/2026-11-18`. Repetir a chamada não abre outro turno nem muda `abertoEm`.

```json
{ "probabilidade": 0.81, "motivo": "mínima de 24 °C pela segunda noite", "origem": "MODELO" }
```

```json
{ "data": "2026-11-18", "abertoEm": "2026-11-18T05:00:03-03:00", "probabilidade": 0.81, "motivo": "mínima de 24 °C pela segunda noite", "origem": "MODELO", "encerradoEm": null }
```

`origem` aceita `MODELO`, `REGRA_BASE` e `MANUAL`.

`POST /api/dias-criticos/2026-11-18/encerramento`, sem corpo. Repetir devolve o mesmo resumo.

```json
{ "data": "2026-11-18", "encerradoEm": "2026-11-18T17:30:00-03:00", "resumo": { "casasAtivas": 40, "comSinalDeVidaAte10h": 34, "atendidas": 4, "vermelhasPendentes": 1, "cinzasPendentes": 1 } }
```

`GET /api/radar?microarea=3`

```json
{
	"turnoAberto": true,
	"agora": "2026-11-18T10:31:00-03:00",
	"pendentes": [
		{ "casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", "situacao": "Vermelho", "motivo": "SemMovimentoNaJanela", "desde": "2026-11-18T10:00:00-03:00", "atendida": false, "perfil": { "contato": "Sozinho", "eletrodependente": false }, "ultimoPacoteEm": "2026-11-18T10:30:52-03:00" }
	],
	"atendidas": [
		{ "casaToken": "a07d5e2c9b1f4c8e6d3a0b7f2e9c4d1a", "situacao": "Cinza", "motivo": "SemHeartbeat", "desde": "2026-11-18T09:10:00-03:00", "atendida": true, "perfil": { "contato": "Sozinho", "eletrodependente": false }, "ultimoPacoteEm": "2026-11-18T09:10:00-03:00" }
	],
	"aguardando": [
		{ "casaToken": "c51e8b3d0f6a4d2c9e7b1a5f3c8d0e6b", "saidaAvisada": false }
	],
	"verdes": 27
}
```

`GET /api/casas/{token}/linha-do-tempo`

```json
{
	"casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b",
	"ultimoPacoteEm": "2026-11-18T10:30:52-03:00",
	"eventos": [ { "seq": 1842, "tipo": "MOVIMENTO", "ocorridoEm": "2026-11-18T06:12:40-03:00", "recebidoEm": "2026-11-18T06:12:41-03:00", "tempC": null, "tensaoV": null, "bufferizado": false } ],
	"confirmacoes": [ { "origem": "Acs", "resultado": "PessoaVista", "quando": "2026-11-18T10:12:00-03:00" } ]
}
```

`PUT /api/demo/relogio`

```json
{ "agora": "2026-11-18T09:58:00-03:00" }
```

```json
{ "agora": "2026-11-18T09:58:00-03:00", "deslocamentoEmSegundos": -36120 }
```

`POST /api/idosos`. O interceptador mascara CPF, RG, CNS e credencial em qualquer campo. Nome e endereço são gravados inteiros.

```json
{ "microareaId": 3, "nome": "Aparecida Souza", "endereco": "Rua das Flores, 12", "perfil": { "contato": "Sozinho", "eletrodependente": true }, "condicoes": "usa concentrador de oxigênio", "medicamentos": "diurético" }
```

```json
{ "id": "6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f" }
```

`POST /api/idosos/{id}/consentimento`. A ACS vem da autenticação. Repetir com consentimento vigente devolve o mesmo token.

```json
{ "versaoDoTermo": "2026-11", "autorizacoes": { "avisarVizinho": true, "avisarFamilia": false } }
```

```json
{ "casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b" }
```

`PUT /api/idosos/{id}/vizinho`

```json
{ "nome": "Rosa Lima", "telefone": "(00) 98888-0000", "aceitouEm": "2026-11-16T14:00:00-03:00" }
```

`PUT /api/idosos/{id}/perfil`

```json
{ "contato": "Acamado", "eletrodependente": true }
```

`GET /api/lista-de-nomes?microarea=3`

```json
[ { "casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", "nome": "Aparecida Souza", "endereco": "Rua das Flores, 12", "vizinho": { "nome": "Rosa Lima", "telefone": "(00) 98888-0000" } } ]
```

`POST /api/visitas`. O `id` nasce no celular. A ACS vem da autenticação. Com 503, a visita já está gravada e o app repete o mesmo `id`. Com o relógio de demo deslocado, o servidor soma o deslocamento a `ocorridaEm`.

```json
{ "id": "0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f", "casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", "ocorridaEm": "2026-11-18T10:40:00-03:00", "desfecho": "EstavaFora", "observacao": "vizinha disse que foi à feira" }
```

```json
{ "id": "0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f", "registradaEm": "2026-11-18T11:30:00-03:00" }
```

`desfecho` aceita `EstavaBem`, `EstavaFora`, `NaoAbriu` e `Encaminhada`.

`POST /api/mensagens/verificar`. A mensagem bloqueada é um resultado normal, então a resposta é 200.

```json
{ "canal": "GrupoDoBairro", "texto": "ele mora sozinho e toma diurético" }
```

```json
{ "id": "9e7c5a3b-1d0f-4b2e-8c6a-4f2d0b8e6c4a", "veredito": "Bloqueada", "textoMascarado": "ele [MORA_SOZINHO] e toma [SAUDE]", "tiposDeDado": ["MoraSozinho", "Saude"], "explicacao": "Essa mensagem diz que a pessoa mora sozinha e fala da saúde dela. No grupo do bairro, qualquer um lê.", "versaoSegura": "Preciso de ajuda para uma visita agora. Quem puder, me chame no privado." }
```

Com `veredito` `Liberada`, `explicacao` e `versaoSegura` saem `null`. A `explicacao` é a frase fixa do tipo de dado. A mensagem do LLM vai para o painel, depois da resposta.

`GET /api/avisos?microarea=3`

```json
[ { "id": "5a3c1e9b-7d2f-4c0a-8e6b-2f4d6a8c0e1b", "casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", "motivo": "SemMovimentoNaJanela", "desde": "2026-11-18T10:00:00-03:00", "registradoEm": "2026-11-18T10:00:12-03:00", "resposta": "EstaBem", "respondidoEm": "2026-11-18T10:05:00-03:00" } ]
```

`POST /api/avisos/{id}/resposta`

```json
{ "resposta": "EstaBem" }
```

```json
{ "id": "5a3c1e9b-7d2f-4c0a-8e6b-2f4d6a8c0e1b", "resposta": "EstaBem", "respondidoEm": "2026-11-18T10:05:00-03:00" }
```

`GET /api/painel`, sem autenticação. Nenhum campo carrega o valor de um dado pessoal.

```json
{ "cor": "Vermelho", "pontos24h": 12, "eventos": [ { "ocorridoEm": "2026-11-18T17:10:00-03:00", "tipo": "BloqueioDeSaida", "canal": "GrupoDoBairro", "modulo": "Cadastro", "tiposDeDado": ["MoraSozinho", "Saude"], "severidade": 4, "mensagemAoCidadao": "Uma mensagem com dados de saúde de um idoso foi barrada antes de ir para o grupo do bairro." } ] }
```

## 7. Casos de teste

Os casos 1 a 20 são do `AvaliadorDeCasas`. Todos são de 18/11, com turno das 05h às 18h, salvo quando a linha diz "dia comum". Cada caso vira um `[Theory]` do xUnit e um Test Case do Azure Boards.

| # | Casa e fatos | Agora | Resultado esperado |
|---|---|---|---|
| 1 | `Sozinho`, sem sinal de vida, aparelho ligado | 09h00 | `Aguardando`. Nunca `Verde`. |
| 2 | `Sozinho`, movimento às 07h00 | 09h00 | `Verde` |
| 3 | `Sozinho`, sem sinal de vida | 10h00 | `Vermelho`, `SemMovimentoNaJanela`, pendente, desde 10h00 |
| 4 | `Sozinho`, só um movimento, às 10h30 | 10h31 | `Verde` |
| 5 | `Sozinho`, só um movimento, às 03h12 | 10h00 | `Vermelho`. O movimento de antes da abertura não vale. |
| 6 | `Sozinho`, movimento às 07h00 | 12h30 | `Aguardando`. A janela da tarde pede sinal de vida novo e ainda não venceu. |
| 7 | `Sozinho`, movimento às 07h00 | 15h30 | `Vermelho`, `SemMovimentoNaJanela`, desde 15h00 |
| 8 | `Sozinho`, sem sinal de vida, visita `EstavaBem` às 10h12 | 10h15 | `Verde` |
| 9 | `Sozinho`, sem sinal de vida, visita `EstavaBem` às 09h00 | 09h30, 10h00 e 15h00 | `Verde` às 09h30. `Aguardando` às 10h00. `Vermelho` desde 15h00 às 15h00. |
| 10 | `Sozinho`, movimento às 09h00, último pacote às 09h10 | 10h00 | `Cinza`, pendente, desde 09h10. Não é `Verde`. |
| 11 | Caso 10 com visita `EstavaBem` às 10h30 | 10h31 e 15h00 | `Cinza`, atendida, às 10h31. `Cinza`, pendente, às 15h00. |
| 12 | Caso 10 com visita `EstavaBem` às 09h05, antes de o aparelho calar | 10h00 | `Cinza`, pendente |
| 13 | Caso 10. O nó volta às 11h00 e reenvia um movimento bufferizado das 10h20. | 11h00 | `Verde`. O `OcorridoEm` conta. Com o movimento bufferizado das 09h50, `Aguardando`. |
| 14 | `Sozinho` sem sinal de vida desde a abertura. Aparelho mudo volta às 11h00 sem movimento. | 11h00 | `Vermelho`, `SemMovimentoNaJanela`, desde 10h00 |
| 15 | Casa ativada que nunca mandou pacote | 09h00 | `Cinza` |
| 16 | `Acamado`, aparelho ligado, movimento às 05h00 | 05h01, 09h01 com visita às 09h00, 10h00 | `Vermelho` desde 05h00 às 05h01. `Verde` às 09h01. `Vermelho` desde 10h00 às 10h00. |
| 17 | `Sozinho`, saída avisada às 08h40 | 09h00, 10h00 e 16h00 | `Verde` às 09h00. `Aguardando` às 10h00 e às 16h00. Nunca `Vermelho` no turno. |
| 18 | `Sozinho`, 35 °C das 11h00 às 15h10, movimento às 13h00, visita `EstavaBem` às 13h30 | 13h05, 13h31 e 15h00 | `Vermelho`, `CalorInterno`, desde 13h00, pendente às 13h05. Atendida às 13h31. Pendente às 15h00. |
| 19 | Eletrodependente, dia comum. Tensão 0 V das 14h20 às 15h05, 127 V às 15h10. Vizinho responde `EstaBem` às 14h30. ACS visita às 14h40 e às 15h30. | 14h21, 14h31, 14h41, 15h00, 15h11 e 15h31 | `Vermelho`, pendente, às 14h21 e às 14h31. Atendida às 14h41. Pendente às 15h00. Pendente às 15h11, pela trava. `Aguardando` às 15h31. |
| 20 | Eletrodependente, dia comum, último pacote às 14h00 em ponto | 61 s e 59 s depois | `Vermelho` com 61 segundos sem pacote. `Aguardando` com 59 segundos. |

Casos que cobrem os desfechos, os perfis e a ingestão:

| # | Casa e fatos | Agora | Resultado esperado |
|---|---|---|---|
| 21 | `Sozinho`, sem sinal de vida, visita `NaoAbriu` às 10h40 | 10h41 | `Vermelho`, pendente. O Cadastro não chama o Motor. |
| 22 | `Sozinho`, sem sinal de vida, visita `EstavaFora` às 10h40 | 10h41 e 15h00 | `Vermelho`, atendida, às 10h41. `Vermelho`, pendente, desde 15h00, às 15h00. Nunca `Verde`. |
| 23 | Caso 10 com vizinho `EstaBem` às 10h30 | 10h31 | `Cinza`, atendida |
| 24 | `Coabitado`, movimento às 07h00 e `BotaoBem` às 08h00 | 09h00 e 10h00 | `Aguardando` nas duas horas. Nenhum evento do sensor conta. |
| 25 | `Coabitado`, visita `EstavaBem` às 10h30 | 10h31 | `Verde` |
| 26 | Mesmo pacote `(casaToken, seq, ts)` recebido duas vezes | | Um `EventoSensor`. |
| 27 | Nó reinicia e manda `seq` 1 com `ts` novo | | Evento aceito. |
| 28 | Pacote com `ts` 3 minutos à frente da chegada | | Pacote recusado. `UltimoPacoteEm` não muda. |
| 29 | `PUT /dias-criticos/{data}` duas vezes, a segunda às 05h01 | | Um turno. `abertoEm` continua 05h00. |
| 30 | `Sozinho`, sem sinal de vida desde 10h00 | 14h30 | Um `PEDIR_TOQUE` para a casa. Nenhum segundo pedido no dia. |
| 31 | Casa eletrodependente ativa. Leitura baixa às 02h40, sem confirmação, com 25 horas | | O `Expurgo` não apaga a leitura. A casa continua `Vermelho`. |

Casos do Cadastro e da Guarda:

| # | Situação | Resultado esperado |
|---|---|---|
| 32 | `Idoso` sem consentimento | Sem token. `GET /lista-de-nomes` não traz a pessoa. |
| 33 | Revogar o consentimento | Token apagado da `Casa`, avisos do token apagados, `DesativarCasaAsync` chamado, visitas ficam. |
| 34 | `DestinatariosDoAviso()` sem `AvisarFamilia` | A família não aparece. |
| 35 | Pedido de contato repetido com o mesmo `(casaToken, motivo, desde)` | Um `AvisoAoVizinho`. |
| 36 | Pedido de contato sem `AvisarVizinho` | Nenhum aviso. |
| 37 | `POST /visitas` repetido com o mesmo `id` | Uma visita. Uma confirmação no Motor. |
| 38 | `POST /idosos` com CPF em `condicoes` | CPF gravado e logado como `***.456.***-00`. Nome e endereço inteiros. Farol amarelo. |
| 39 | Mensagem "ele mora sozinho e toma diurético" para `GrupoDoBairro` | `Bloqueada`. `MensagemSaida` só com o texto mascarado. `SaidaBloqueada` de severidade 4. Farol vermelho. |
| 40 | Gravar `Idoso.Nome` no log | O log recebe `[NOME]` e o painel ganha um evento. |
| 41 | Rota sem autenticação que tentaria devolver `Idoso.Condicoes` | Resposta bloqueada pelo `FiltroDeResposta`. |
| 42 | LLM fora do ar | A mensagem ao cidadão é a frase fixa. |

Os mesmos casos entram no Azure Boards. Os negativos da arquitetura estão cobertos: silêncio nunca vira verde pelos casos 1, 10, 11 e 15. Família sem autorização não recebe aviso pelo caso 34. Mensagem com "mora sozinho" é bloqueada pelo caso 39. Reenvio não duplica evento pelo caso 26.

## 8. O que entra em 19/11

### 8.1 Ordem de construção

| Passo | Quando | Contratos e Motor | Cadastro | Guarda |
|---|---|---|---|---|
| 1 | 16/11, noite | A solução com os cinco projetos e os três de teste, com as referências da seção 2. `Corredor.Contratos` fechado. `ParametrosDoMotor` e `RelogioDeDemo`. | `Idoso` vazio, com a migração. `Microarea` por carga inicial. | `TipoDeDado`, `Canal`, `TextoRotulado`, `Veredito`. |
| 2 | 17/11 | `CasaMonitorada` com `AtivarCasa` e `RegistrarPacote`. `EventoSensor` com a chave única e a recusa de `ts`. `TradutorDePacote` e `ConsumidorMqtt`. `DiaCritico` com `DeclararDiaCritico`, `TurnoRadar` e as janelas. `AvaliadorDeCasas` com `SemHeartbeat`, `SemMovimentoNaJanela`, sinal de vida por perfil, `Verde` e `Aguardando`. `Radar`. | `RegistrarConsentimento` gera o token e chama `AtivarCasaAsync`. `ListaDeNomes`. `ReconciliacaoComMotor`. | `MotorDeParsing`, `PoliticaDeCanal`, `SaidaSegura`, `InterceptadorDeEntrada`. |
| 3 | 18/11 | `EnergiaEletrodependente` com a trava. `Confirmar`, atendimento e a tabela da seção 5.7. `CalorInterno`. `LinhaDoTempo`. | `Visita` com `TradutorDeDesfecho`. `MensagemSaida`. | `EventoDeSeguranca`, `Tradutor`, `Farol`, painel, `ProvedorDeLogSeguro`, `FiltroDeResposta`, `TratadorDeErros`. |
| 4 | 18/11, se sobrar tempo, nesta ordem | `Expurgo`. `EncerrarTurno` com o resumo. Saída avisada e `AusenciaConfirmada`. `VigiaDoRadar` com o pedido de toque e o encerramento das 18h. Pedido de contato pela porta. | Rotas de revogação e de perfil. `AvisoAoVizinho` e `ReceptorDeContato`. Resposta do vizinho e `GET /avisos`. | Schema `guarda`, se a lista em memória entrou primeiro. |

Três pessoas escrevem no mesmo `api/`. Cada uma fica dentro do projeto do próprio módulo. Só uma pessoa mexe em `Corredor.Api/Program.cs`.

### 8.2 Ordem de corte

Se o prazo apertar, a equipe corta nesta ordem, do primeiro ao último. Cada item diz o que sobra quando ele sai.

| # | Corta | O que sobra |
|---|---|---|
| 1 | `IdaNoturnaSemVolta` e o aviso à família | A autorização `AvisarFamilia` e o caso 34. |
| 2 | Agrupamento de apagão por rua | Nada. O Motor não tem a rua. |
| 3 | Envio simulado ao vizinho, resposta do vizinho e `GET /avisos` | O `VigiaDoRadar`, a porta e o registro `AvisoAoVizinho`. A origem `Vizinho` fica coberta pelo caso 23. |
| 4 | Timer das 05h | O script de ML roda à mão. |
| 5 | Pedido de toque | O turno sem o buzzer das 14h30. |
| 6 | Saída avisada e `AusenciaConfirmada` | O `BOTAO_SAIR` vira só sinal de vida, sem suspender a regra 4. `EstavaFora` deixa de chamar o Motor, e a casa fica vermelha e pendente. |
| 7 | `EncerrarTurno` e `ResumoDoTurno` | O turno fecha às 18h. O indicador sai de uma consulta feita na gravação. |
| 8 | Rotas de revogação e de perfil | Os métodos na raiz e os testes deles. |
| 9 | `Expurgo` | O README declara que a retenção de 24 horas não foi construída. |
| 10 | `VigiaDoRadar`, `IReceptorDeContato` e `AvisoAoVizinho` | O card no topo do radar. Ninguém é chamado de madrugada, e o vídeo diz isso. |
| 11 | `CalorInterno` | Três classes concretas de regra, que ainda sustentam a herança. |

### 8.3 Nunca corta

Estas regras são o produto. Sem uma delas, a demo mostra um verde sem sinal de vida ou um vazamento.

1. A chave única `(casaToken, seq, ts)` e a recusa de `ts` adiantado.
2. `SemHeartbeat` antes de qualquer regra que olhe sinal de vida, medido por `UltimoPacoteEm`.
3. `EnergiaEletrodependente` com a trava.
4. `Aguardando`, e `Verde` só com turno aberto e sinal de vida na janela atual.
5. O sinal de vida por perfil de contato. No `Acamado` e no `Coabitado`, só confirmação com `PessoaVista`.
6. O token só depois do consentimento.
7. `NaoAbriu` não confirma a casa, e `AusenciaConfirmada` nunca é sinal de vida.
8. O canal `GrupoDoBairro` não aceita rótulo.
9. `EventoDeSeguranca` sem o valor, e `MensagemSaida` só com o texto mascarado.
10. O `RelogioDeDemo`. Sem ele, não há vídeo das janelas de 10h e de 15h.

### 8.4 Fora da entrega

- Casa com dois idosos.
- Responsável legal no consentimento.
- Troca de nó com o mesmo token.
- Mais de uma microárea e encerramento do turno por microárea.
- Autenticação com mais de uma ACS por microárea.
- Envio real ao vizinho e à família.
- Assinatura do pacote MQTT contra movimento forjado.
- Integração com o e-SUS e com a distribuidora de energia.
- Tratamento da visita depois da revogação.

O token viaja no nome do tópico por um broker público. Quem assina `corredor/v1/#` lê o token e pode publicar um `MOVIMENTO` falso, que o Motor trataria como sinal de vida. Só a casa do Wokwi passa por esse caminho. O modelo reduz o dano: token de 128 bits, pacote de token desconhecido descartado, e nenhum pacote muda perfil ou cadastro. A correção é assinar o pacote com um segredo por casa. O vídeo de Physical Computing e o PDF de Cyber dizem essa limitação.

## 9. Mudanças em relação a `docs/arquitetura.md`

| # | A arquitetura diz | Este modelo diz | Motivo |
|---|---|---|---|
| 1 | Cinco projetos. `Corredor.Cadastro` referencia a interface `IMotorDeCasas`. | Seis projetos. `IMotorDeCasas`, `IReceptorDeContato` e os tipos compartilhados ficam em `Corredor.Contratos`. Motor e Cadastro referenciam esse projeto e nenhum referencia o outro. | A barreira fica no compilador mesmo com tudo `public`. |
| 2 | Três situações. O radar devolve vermelhos, cinzas e a contagem de verdes. | Quatro situações, com `Aguardando`. `Verde` exige sinal de vida na janela atual e só existe com turno aberto. O radar devolve pendentes, atendidas, aguardando e a contagem de verdes. | Antes do prazo, a casa sem sinal de vida seria verde sem ninguém saber dela. |
| 3 | `PerfilDaCasa` é um enum com `Sozinho`, `Acamado`, `Eletrodependente` e `Coabitado`. | `PerfilDaCasa(PerfilDeContato, bool Eletrodependente)`. | A eletrodependente que mora sozinha, ou acamada, não cabe num valor só. |
| 4 | A seta "aviso neutro" sai do Motor para o vizinho. | O aviso mora no Cadastro. O Motor manda um `PedidoDeContato` pela porta `IReceptorDeContato`. O Cadastro descarta a repetição por episódio. O Motor não guarda alerta. | O Motor não tem o telefone do vizinho e não pode ter. |
| 5 | Chave única `(casaToken, seq)`. | Chave única `(casaToken, seq, ts)`. O firmware não muda. | O `seq` volta a 1 quando o ESP32 reinicia. |
| 6 | Todo pacote vira evento. O payload tem só `ts`. | O `HEARTBEAT` só avança `UltimoPacoteEm`. `EventoSensor` grava `OcorridoEm` e `RecebidoEm`. `ts` adiantado ou velho é recusado. | Volume, e o silêncio mede a chegada enquanto a janela mede o fato. Um relógio adiantado criaria um verde falso. |
| 7 | `IMotorDeCasas` com `DefinirPerfilAsync(token, perfil)` e `ConfirmarAsync(token, origem)`. `OrigemDaConfirmacao` com `Acs` e `Vizinho`. | Quatro métodos: `AtivarCasaAsync` com microárea, `DefinirPerfilAsync`, `ConfirmarAsync(token, Confirmacao)` e `DesativarCasaAsync`. `Confirmacao` tem `Id`, `Origem`, `Resultado` e `Quando`. | O radar filtra por microárea. A revogação desliga a casa. Sem `Quando`, a hora seria a do envio. Sem `Id`, o reenvio duplicaria. Sem `Resultado`, "estava fora" viraria sinal de vida. |
| 8 | `POST /visitas` grava o desfecho e confirma a casa no Motor. | Só `EstavaBem` e `Encaminhada` são sinal de vida. `EstavaFora` vira `AusenciaConfirmada`. `NaoAbriu` não chama o Motor. O corpo leva `id` e `ocorridaEm`. | "Não abriu, chamei SAMU" deixaria a casa verde. |
| 9 | A arquitetura não diz quanto tempo vale uma confirmação. | A confirmação vale só na janela atual, para todas as regras e para o acamado. | Uma visita às 09h deixaria a tarde de um dia de 36 °C sem checagem. |
| 10 | Janelas "das 10h ou das 15h". `PEDIR_TOQUE` às 15h. | A manhã vai da abertura às 10h. A tarde vai das 10h às 15h. O pedido de toque sai às 14h30. | Às 15h a janela já fechou. |
| 11 | `SemHeartbeat` com 30 minutos. Regra 1 com "sem heartbeat", sem número. | Heartbeat a cada 20 segundos. Eletrodependente com 60 segundos sem pacote fica vermelho. Todo número fica em `ParametrosDoMotor`, com valor de demo, e existe o `RelogioDeDemo`. | O critério de teste pede alerta rápido para o eletrodependente, e o vídeo não pode esperar as 10h. |
| 12 | Regra 1 com "tensão caindo". | Depois de uma leitura baixa, a casa fica vermelha até a ACS ver a pessoa depois da leitura. O `Expurgo` não apaga leitura baixa sem confirmação depois dela. | A luz voltar não mostra que a pessoa está bem. |
| 13 | `OrigemDaConfirmacao` sem efeito por regra. | Cada regra lista as origens e os resultados que aceita. A regra de energia aceita só a ACS. | O vizinho não pode resolver um alerta de energia. |
| 14 | O interceptador troca o valor pela máscara antes do log e do banco. | Ele mascara CPF, RG, CNS e credencial em qualquer campo. Os outros tipos de dado só ganham rótulo. | Mascarar nome e endereço apagaria o dado da lista de nomes, e a `SaidaSegura` não veria o que bloquear. |
| 15 | `MensagemSaida` é dado do Cadastro, sem regra sobre o texto. | `MensagemSaida` guarda só o texto mascarado. | O texto bloqueado é o próprio vazamento. |
| 16 | "Qualquer bloqueio de severidade 4 ou 5 dá vermelho." | Só uma saída bloqueada deixa o farol vermelho. O mascaramento na entrada vai até amarelo. | O mascaramento mostra o sistema funcionando, não uma tentativa de saída. |
| 17 | Um Postgres, dois schemas. | Três schemas, com `guarda`. | O painel precisa dos eventos de segurança depois de um reinício. |
| 18 | `TurnoRadar` é entidade do Motor ao lado de `DiaCritico`. | `TurnoRadar` é objeto de valor de `DiaCritico`. Um encerramento por data. | O turno não existe sem o dia crítico e tem a mesma chave. |
| 19 | A tabela de rotas não tem encerramento, revogação, perfil, avisos nem relógio de demo. | Entram `POST /dias-criticos/{data}/encerramento`, `DELETE /idosos/{id}/consentimento`, `PUT /idosos/{id}/perfil`, `GET /avisos`, `POST /avisos/{id}/resposta`, `PUT /demo/relogio` e `GET /painel`. | O app tem a tela de encerramento. As outras rotas fecham fluxos que a própria arquitetura descreve. |
| 20 | O script de ML roda na demo com uma data histórica. | `DiaCritico` só aceita a data de hoje. O script usa as variáveis da data histórica e envia a data de hoje. | Um dia crítico com data passada nunca tem turno aberto. |
| 21 | "O módulo Motor não guarda dado pessoal." | "O Motor guarda só dado pseudonimizado." | `Acamado` e `Eletrodependente` são dado de saúde ligado a um token que o Cadastro sabe reverter. A frase exata resiste à pergunta da banca sobre LGPD. |
| 22 | O objetivo do protótipo diz que o aparelho mudo deixa o card cinza. | Vale para toda casa, menos a do eletrodependente, que fica vermelha. | A regra 1 da arquitetura já diz vermelho. O pitch mostra o cinza em outra casa. |
| 23 | "Perfil Acamado entra aqui direto." | O acamado fica vermelho no início de cada janela sem confirmação com `PessoaVista`. A casa `Coabitado` só fica verde com confirmação humana. | Na casa com outra pessoa, o movimento não é sinal de vida da idosa. |
| 24 | Ordem de corte: `IdaNoturnaSemVolta`, agrupamento por rua, aviso ao vizinho com o registro, timer das 05h. | A ordem da seção 8.2. O envio ao vizinho é o terceiro corte, mas o registro do pedido de contato só sai no décimo. | Sem o registro, o fluxo da madrugada do eletrodependente perde a trilha. |

Em três pontos este modelo concorda com a arquitetura por um motivo que vale registrar. A situação calculada por função pura é a decisão que mais protege contra o verde falso. O `DiaCritico` dentro do Motor evita uma chamada entre módulos às 05h. O Motor sem referência ao Cadastro transforma a regra de privacidade em erro de compilação.

### 9.1 Mudanças pedidas pelo modelo de dados

O [04-modelo-de-dados.md](04-modelo-de-dados.md) pediu estas mudanças para gravar o modelo. Cada linha diz onde o texto mudou neste arquivo ou em [02-event-storming.md](02-event-storming.md). O resto dos dois arquivos não mudou.

| # | Mudança | Onde | Motivo |
|---|---|---|---|
| 1 | `AvisoAoVizinho` grava `MicroareaId`. | 4.3, agregado `AvisoAoVizinho`, linha "Gravado" | `GET /avisos` filtra por microárea, e o token some na revogação. |
| 2 | O `Expurgo` guarda a leitura baixa só de casa ativa e eletrodependente, só uma confirmação `Acs` com `PessoaVista` solta a leitura, e os eventos saem antes das confirmações. | 4.2, tabela de serviços, `Expurgo`. Caso 31. Política P7 e hotspot H23 do event storming. | Sem o recorte, um apagão deixa leituras de 0 V para sempre nas casas comuns. Sem a ordem, a trava nunca solta. |
| 3 | A avaliação lê as leituras baixas de qualquer idade, pelo critério do `Expurgo`, e não pelo que sobrou na tabela. | 4.2, modelo de leitura `Radar`. 5.1, `HistoricoDaCasa`. | O radar acerta com o expurgo atrasado ou cortado. |
| 4 | `POST /visitas` soma a `ocorridaEm` o deslocamento do relógio de demo. | 4.3, invariante 3 de `Visita`. 5.9. 6.3, exemplo de `POST /api/visitas`. Hotspot H36 do event storming. | Com o relógio deslocado, a visita caía fora da janela ou recebia `VISITA_NO_FUTURO`. |
| 5 | Depois de mover o relógio para trás, roda o reinício da seção 8.4 de 04-modelo-de-dados.md. | 5.9 | `UltimoPacoteEm` só anda para frente, e a casa nunca ficaria cinza nem vermelha por silêncio. |
| 6 | `ToleranciaDoRelogio` sai de `ParametrosDoMotor` para `ParametrosDoRelogio`, e o deslocamento do relógio de demo ganha a interface `IDeslocamentoDoRelogio`. Os dois ficam em `Corredor.Contratos`. | 4.1, "Parâmetros". 5.9. 6.1, código e lista. | A invariante 3 da `Visita` e a mudança 4 rodam no Cadastro, que não referencia o Motor. A mudança em `Corredor.Contratos` pede o acordo das duas pessoas. |
| 7 | `AvisosDoDia` lê as últimas 24 horas, não o dia de hoje. | 4.3, tabela de leitura do Cadastro | "Hoje" poria o fuso de São Paulo no Cadastro. O fuso fica no corte das janelas. |
| 8 | Revogar o consentimento apaga os avisos ao vizinho daquele token. | 4.3, invariante 8 de `Idoso`. Caso 33. Fluxo 1, passo 8, do event storming. | O aviso guarda o token que a revogação promete apagar. |
| 9 | `CadastrarIdoso` aceita um `id` opcional, que só a carga da demo informa. | 4.3, comandos de `Idoso` | A carga passa pelos casos de uso e precisa ser repetível. O token nasce na carga e não entra no Git. |
| 10 | A regra de CPF mascara pelo formato. O dígito verificador válido só sobe a severidade de 4 para 5. | 4.4, `RegraDePii` e severidades | O exemplo `123.456.789-00`, do professor e do caso 38, tem dígito inválido e precisa ser mascarado. |
| 11 | A regra de credencial casa com o nome inteiro da chave e nunca mascara `casaToken`. O mascaramento na entrada registra o canal `Banco`. | 4.4, `InterceptadorDeEntrada` | Um token de 32 hexadecimais parece credencial, e `POST /visitas` pararia. O canal é obrigatório e entra na pontuação. |
| 12 | A observação da visita leva o rótulo `Saude` pelo campo, mais os rótulos do conteúdo. Lida do banco, volta com todos os rótulos de texto livre. | 4.3, invariante 5 de `Visita` | Uma frase em que as regras não acham nada sairia sem rótulo e poderia ir ao log. O banco não guarda rótulo. |
| 13 | Se o Motor recusa a confirmação por ter mais de 24 horas, o Cadastro mantém a visita e responde 201. | 4.3, invariante 7 de `Visita` | A fila do app trataria um 422 como recusa, com a visita já gravada. |
| 14 | `EventoDeSeguranca` grava o modelo da rota, nulo fora de uma requisição. | 4.4, agregado `EventoDeSeguranca`, linha "Gravado" | O caminho real leva o token e o `IdosoId` ao painel aberto. |
| 15 | `AcsId` é texto no formato `^[a-z0-9-]{3,40}$`. Não existe tabela de ACS. | 4.3, parágrafo depois de `Microarea` | Quatro colunas guardam o valor. |
| 16 | O envio simulado é a própria linha do aviso e uma linha de log sem telefone. | 4.3, política P19 | O modelo não dizia onde o envio simulado fica gravado. |
| 17 | A linha do tempo de casa desativada responde 404 `CASA_DESCONHECIDA`. | 6.3, tabela de rotas | A linha vazia confirmaria que o token existiu. |
| 18 | Com conflito de versão ou chave repetida, o caso de uso recarrega e repete o comando uma vez. | 4.1, "Concorrência" | O modelo dizia como repetir sem efeito, não o que fazer com duas escritas ao mesmo tempo. |
| 19 | Cada módulo conecta com um papel próprio do Postgres. | 2, último parágrafo | O Postgres passa a impedir que um módulo leia o schema de outro. |
| 20 | Os telefones de exemplo usam o DDD 00. | 6.3, exemplos de `PUT /api/idosos/{id}/vizinho` e de `GET /api/lista-de-nomes` | A varredura do repositório falha com DDD que existe. |

As propostas recusadas e as que ficaram para a equipe estão na seção 11 de 04-modelo-de-dados.md.
