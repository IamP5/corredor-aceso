# Visão geral do Corredor Aceso

Este documento mostra a solução inteira em uma leitura. Ele diz o que o produto faz, quais são as partes, quem é dono de cada dado, quais decisões dão forma ao desenho e onde cada disciplina da Global Solution pontua. Os detalhes ficam nos documentos 02 a 05, e cada seção aponta para eles.

Ele substitui a primeira versão da arquitetura, que ficava em `docs/arquitetura.md`. Onde os dois textos discordam, vale este, porque ele segue os documentos finais. A seção [O que mudou desde a primeira versão](#o-que-mudou-desde-a-primeira-versão) lista as diferenças.

## O que o produto faz

O Corredor Aceso é uma luz de corredor que protege a idosa do tropeço à noite. Em dia crítico de calor ou de apagão, a agente comunitária de saúde (ACS) abre o celular e vê só as casas que ficaram em silêncio, não as 40 da microárea.

O produto funciona em cinco passos:

1. Às 05h, um job decide se hoje é dia crítico para pessoas de 60 anos ou mais. Quem decide é a regra da noite quente, com o clima do INMET. Um XGBoost treinado com INMET e SIH/DATASUS empatou com essa regra no teste, e por isso o job usa a regra (`origem: REGRA_BASE`). Se for dia crítico, o job abre o turno do radar.
2. Na casa, um nó ESP32 acende a luz quando a idosa passa no corredor. O mesmo movimento conta como sinal de vida. O nó também mede temperatura e tensão, tem um botão e guarda os eventos quando a rede cai.
3. No dia crítico, o app da ACS mostra o radar. Cada casa está em uma de quatro situações: `Verde`, `Aguardando`, `Vermelho` ou `Cinza`. A casa verde não aparece como card. Ela só entra na contagem.
4. Quando uma casa tem alerta pendente, o vizinho de confiança que a idosa escolheu recebe um aviso neutro, sem nome e sem motivo de saúde.
5. Todo texto que sai do sistema passa por uma verificação de dado pessoal. Um bloqueio vira um evento no painel Risco Cidadão, explicado em duas frases por um LLM.

Uma regra o produto nunca quebra. O silêncio do aparelho nunca vira verde. Uma casa só fica `Verde` com o turno aberto e com sinal de vida na janela atual.

A ficha da CitizenTech, as seis perguntas do enunciado e a pergunta de ML estão em [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md). O produto se liga a quatro Objetivos de Desenvolvimento Sustentável:

| ODS | Meta | Ligação com o produto |
|---|---|---|
| 3, saúde e bem-estar | 3.4 e 3.d | Previne desidratação, queda e descompensação cardiovascular em idosos. Transforma alerta de clima em ação da atenção básica. |
| 10, redução das desigualdades | 10.2 | A idosa não precisa de smartphone, Wi-Fi, crédito nem leitura. |
| 11, cidades sustentáveis | 11.5 | Reduz o dano do calor extremo e do apagão a quem é mais vulnerável no território. |
| 13, ação climática | 13.1 | Adapta o cuidado ao calor extremo com um gatilho que vem de dados de clima. |

As entrevistas e os testes da descoberta foram simulados com personas. Nenhuma ACS real participou até aqui, e cada documento da descoberta diz isso no início.

## O sistema em um desenho

No desenho, siga o caminho de um movimento. Ele sai do nó, passa pelo broker público e pela ponte do Mosquitto, chega ao módulo Motor e aparece no app pelo Nginx. O Motor está em âmbar porque é ele que responde quem ficou em silêncio.

![Visão geral do sistema: casa, broker público, VM, módulos da API, app, dados públicos, vizinho e LLM](diagramas/01-visao-geral.mmd)

## Partes e donos

Cada parte tem uma pasta, uma tecnologia e um dado que só ela grava.

| Parte | Pasta | Tecnologia | Dona de | Disciplina |
|---|---|---|---|---|
| Nó do corredor | `iot/` | ESP32 no Wokwi, PubSubClient | leitura dos sensores, luz de caminho, buffer, heartbeat | Physical Computing |
| Simulador | `iot/simulador/` | Python | pacotes das 39 casas simuladas | Physical Computing |
| Contratos | `api/src/Corredor.Contratos` | .NET 10 | `CasaToken`, `PerfilDaCasa`, `Confirmacao`, `PedidoDeContato`, `IMotorDeCasas`, `IReceptorDeContato` | C# e SOA |
| Módulo Motor | `api/src/Corredor.Motor` | .NET 10, EF Core, MQTTnet | `CasaMonitorada`, `EventoSensor`, `DiaCritico`, a situação da casa e o radar | C# |
| Módulo Cadastro | `api/src/Corredor.Cadastro` | .NET 10, EF Core | `Idoso` com `Casa`, `Consentimento` e `VizinhoDeConfianca`, `Visita`, `MensagemSaida`, `AvisoAoVizinho`, `Microarea` | SOA |
| Módulo Guarda | `api/src/Corredor.Guarda` | .NET 10, regras em JSON, LLM por HTTP | `RegraDePii`, `TextoRotulado`, `PoliticaDeCanal`, `EventoDeSeguranca`, o painel | Cybersecurity |
| Host | `api/src/Corredor.Api` | ASP.NET Core, Swagger | `Program.cs`, autenticação, injeção de dependência, `/healthz` e `/api/versao` | C# e SOA |
| Testes | `api/tests/` | xUnit | um projeto de teste por módulo | C# e SOA |
| Modelo de dia crítico | `ml/` | pandas, XGBoost, scikit-learn | notebook, relatório, `prever_hoje.py` | IA e ML |
| App da ACS | `app/` | Expo, React Native, TypeScript, Expo Router, expo-sqlite | cache da lista de nomes, do radar e dos avisos, fila de desfechos | Mobile |
| Infra | `infra/` e `tools/` | Bash, systemd, Nginx, Mosquitto, Postgres, Terraform | VM, serviços, logs, monitor, deploy | Operating Systems |
| Qualidade | Azure Boards | Features e Test Cases | critérios de aceite | Testing |

Três regras decidem quem enxerga o quê:

- **O Motor só guarda dado pseudonimizado.** Ele conhece o token da casa, a microárea e o perfil. `Corredor.Motor` não referencia `Corredor.Cadastro`, então o compilador impede o Motor de ler um nome.
- **O Cadastro é o único dono de dado pessoal.** Nome, endereço, condições, medicamentos e o telefone do vizinho ficam no schema `cadastro`, em campos do tipo `TextoRotulado`.
- **O token e o nome só se encontram no celular.** O app baixa a lista de nomes para o SQLite e junta com o radar pelo `casaToken`.

O banco repete a separação. Há um Postgres com três schemas, `motor`, `cadastro` e `guarda`. Cada módulo conecta com um papel próprio, que não enxerga o schema dos outros. As referências permitidas entre os projetos estão na seção 2 do [modelo de domínio](03-modelo-de-dominio.md#2-projetos-net-e-referências-permitidas), e as tabelas estão no [modelo de dados](04-modelo-de-dados.md).

## Decisões que dão forma ao desenho

Cada decisão tem o motivo e o documento que a detalha.

| # | Decisão | Motivo | Onde está |
|---|---|---|---|
| 1 | Um monólito modular em C#, com três módulos, em vez de duas APIs. | O enunciado de SOA aceita qualquer linguagem. Um processo cobre as rubricas de SOA e de C# com um deploy só. A separação por processo que se perde volta como separação por projeto. | [03, seção 2](03-modelo-de-dominio.md#2-projetos-net-e-referências-permitidas) |
| 2 | .NET 10, em pacote self-contained para x64 e arm64. | O .NET 8 saiu de suporte em 10/11/2026. Com o pacote self-contained, a VM não instala runtime, e o binário que roda é o que foi testado. | [05, seção 1.3](05-infra-e-entrega.md#13-net-10-e-pacote-self-contained-para-x64-e-arm64) |
| 3 | `Corredor.Contratos` guarda os tipos e as duas interfaces. Motor e Cadastro referenciam esse projeto e nenhum referencia o outro. | A regra de privacidade vira erro de compilação. | [03, seção 1.2](03-modelo-de-dominio.md#12-mapa-de-contexto) |
| 4 | A situação da casa é calculada por uma função pura, o `AvaliadorDeCasas`. Ela não tem coluna no banco. | A situação muda com a passagem do tempo, sem evento. Uma coluna velha poderia mostrar um verde que ninguém provou. | [03, seção 5](03-modelo-de-dominio.md#5-regras-da-situação-da-casa) |
| 5 | São quatro situações. `Aguardando` é a casa sem alerta e sem sinal de vida na janela atual. | Com três situações, a casa que ainda não deu sinal de vida às 06h seria verde sem ninguém saber dela. | [02, hotspot H1](02-event-storming.md#situação-da-casa-e-radar) |
| 6 | A regra `SemHeartbeat` vem antes de toda regra que olha sinal de vida, e mede a hora de chegada do último pacote. | O aparelho mudo nunca vira verde, mesmo com um movimento antigo guardado. | [03, seção 5.4](03-modelo-de-dominio.md#54-as-regras-em-ordem) |
| 7 | A chave única do evento é `(casaToken, seq, ts)`. O `HEARTBEAT` não vira linha. | O reenvio depois de uma queda não duplica nada. O `seq` volta a 1 quando o ESP32 reinicia. O heartbeat daria cerca de 170 mil linhas por dia. | [03, seção 4.2](03-modelo-de-dominio.md#42-motor) |
| 8 | O sinal de vida depende do perfil de contato, e uma confirmação humana vale só na janela atual. | Na casa com outra pessoa, o movimento não é sinal de vida da idosa. Uma visita às 09h não cobre a tarde de um dia de 36 °C. | [03, seções 5.3 e 5.5](03-modelo-de-dominio.md#53-sinal-de-vida-por-perfil-de-contato) |
| 9 | O aviso ao vizinho mora no Cadastro. O Motor manda um `PedidoDeContato` pela porta `IReceptorDeContato`. | O Motor não tem o telefone do vizinho e não pode ter. | [02, fluxo 8](02-event-storming.md#fluxo-8-pedido-de-contato-e-aviso-ao-vizinho) |
| 10 | O `DiaCritico` fica dentro do Motor, e o turno é um objeto de valor dele. | Evita uma chamada entre módulos às 05h. O turno existe porque o dia crítico existe. | [03, seção 4.2](03-modelo-de-dominio.md#42-motor) |
| 11 | A Guarda mascara CPF, RG, CNS e credencial na entrada. Os outros tipos de dado ganham rótulo, e a `SaidaSegura` decide por canal. | Mascarar nome e endereço apagaria o dado da lista de nomes, e a saída não teria o que bloquear. | [03, seção 4.4](03-modelo-de-dominio.md#44-guarda) |
| 12 | O LLM recebe o tipo de dado, o canal e o módulo. Ele nunca recebe o texto. | O prompt é uma saída como as outras e passa pela `SaidaSegura` com o canal `PromptDoLlm`. | [03, seção 4.4](03-modelo-de-dominio.md#44-guarda) |
| 13 | O nó do Wokwi publica num broker público, e o Mosquitto da VM abre uma ponte de saída. | O Wokwi do navegador não alcança a rede local sem o gateway pago. A VM não abre porta MQTT. O tópico leva só o token. | [05, seção 1.11](05-infra-e-entrega.md#111-app-nó-do-wokwi-simulador-e-relógio-de-demo) |
| 14 | A VM oficial é a do Multipass, no notebook de quem grava. A VM do Azure é um acréscimo com prazo. | Nenhum item avaliado depende da nuvem, e um problema de cota na véspera derrubaria a demo. | [05, seção 1.1](05-infra-e-entrega.md#11-local-primeiro-azure-como-acréscimo) |
| 15 | Dentro da VM, quem trabalha são scripts Bash. O Terraform só cria a VM, o cloud-init só clona e chama um script, e o GitHub Actions só chama um comando por SSH. | A rubrica de Operating Systems avalia o provisionamento por script. | [05, seção 1.2](05-infra-e-entrega.md#12-terraform-cloud-init-e-bash-cada-um-no-seu-papel) |
| 16 | A release é uma tag `vX.Y.Z`. O deploy guarda um `pg_dump`, aplica o SQL gerado pelo EF Core e volta sozinho se a versão nova não fica saudável. | A tag diz o que foi testado, empacotado e posto no ar. A migração aditiva deixa o rollback sem mexer no banco. | [05, seções 1.5 a 1.7](05-infra-e-entrega.md#15-branches-tags-e-releases) |
| 17 | Todo número das regras fica em `ParametrosDoMotor`, com valor de produção e de demo, e existe o `RelogioDeDemo`. | A janela das 10h, a das 15h e os 30 minutos de silêncio não cabem num vídeo. | [03, seção 5.9](03-modelo-de-dominio.md#59-parâmetros-e-relógio-de-demo) |

## As três cenas da demo

O pitch mostra três cenas. Cada uma prova uma regra do produto e tem caso de teste no [modelo de domínio](03-modelo-de-dominio.md#7-casos-de-teste). As cenas são gravadas na VM local, com a API em `Development`. Quem grava move a hora do Motor com `PUT /api/demo/relogio`, e os valores de demo de `ParametrosDoMotor` encurtam a espera.

### Cena 1: o movimento tira o card da tela

Às 10h31 de um dia crítico, a casa do nó do Wokwi está em `Vermelho` por `SemMovimentoNaJanela`. A idosa passa no corredor. O nó acende a luz e publica um `MOVIMENTO`. No próximo `GET /api/radar`, a casa está `Verde` e o card sai da lista de pendentes. A cena é o caso de teste 4.

No desenho, veja que o Motor grava duas coisas: a hora de chegada em `ultimo_pacote_em` e o evento com a chave `(casa_token, seq, ocorrido_em)`. A situação não é gravada. Ela é calculada no passo 9.

![Cena 1: o movimento no corredor faz o card sair do radar](diagramas/03-cena-movimento.mmd)

### Cena 2: o aparelho mudo fica cinza

Uma casa `Sozinho`, que não é eletrodependente, deu sinal de vida às 09h00. Às 09h10 o nó para de publicar. Passado o `SilencioMaximo`, de 30 minutos em produção e de 1 minuto na demo, a casa fica `Cinza` por `SemHeartbeat` e entra nas pendentes. O movimento das 09h00 não a deixa verde. A cena cobre os casos de teste 10 e 13.

Na casa do eletrodependente o resultado é outro. Com 60 segundos sem pacote, ela fica `Vermelho` por `EnergiaEletrodependente`, em qualquer dia. Por isso a cena usa outra casa.

No desenho, veja o intervalo entre os passos 4 e 5. Nenhuma mensagem chega, e é a falta dela que muda o radar.

![Cena 2: o nó para de publicar e a casa fica cinza, nunca verde](diagramas/04-cena-silencio.mmd)

### Cena 3: a mensagem com dado pessoal é bloqueada

A ACS escreve "ele mora sozinho e toma diurético" para o grupo do bairro e toca em verificar. A `SaidaSegura` acha os rótulos `MoraSozinho` e `Saude`, que o canal `GrupoDoBairro` não aceita. O app mostra a frase fixa e a versão segura, o painel Risco Cidadão fica vermelho e o LLM explica o bloqueio em duas frases. A cena é o caso de teste 39.

No desenho, veja que a resposta 200 sai no passo 7, antes de o LLM ser chamado, e que o Postgres recebe só o texto mascarado.

![Cena 3: a mensagem para o grupo do bairro é bloqueada e explicada](diagramas/05-cena-bloqueio.mmd)

## App da ACS

O app tem seis telas no Expo Router: entrada, dia crítico, radar, detalhe da casa, mensagem bloqueada e encerramento do turno. O SQLite guarda a lista de nomes, o último radar, os avisos e a fila de desfechos que ainda não subiram. A sessão da ACS fica no `expo-secure-store`. A letra é grande, o contraste é alto, cada controle tem rótulo para leitor de tela, e a situação aparece em texto além da cor.

No desenho, veja a volta do detalhe da casa para o radar. Registrar o desfecho de uma visita é o gesto que a ACS repete o dia inteiro. Os documentos finais não dizem de qual tela a ACS abre a mensagem para o grupo. O desenho parte do detalhe da casa.

![Telas do app da ACS e a navegação entre elas](diagramas/27-telas-do-app.mmd)

As tabelas do SQLite e a fila de desfechos estão na seção 6 do [modelo de dados](04-modelo-de-dados.md#6-banco-sqlite-do-app).

## Modelo de dia crítico

A pergunta de ML é se hoje será um dia de excesso de internações de urgência por causa cardiovascular em pessoas de 60 anos ou mais, com o que se sabe às 05h. A pergunta, o alvo, a regra de base e a grade de 24 combinações estão na seção 5 de [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md). A pasta `ml/` entrega:

- `ml/corredor_aceso.ipynb` executado, com validação cruzada só no treino e métricas de treino e de teste lado a lado.
- `ml/dados/README.md`, com fonte, link e período do INMET e do SIH.
- `ml/requirements.txt` e `ml/RELATORIO_ML.md`, de 2 a 4 páginas.
- `ml/prever_hoje.py`, que lê `ml/modelo/metadados.json`, aplica a regra ou o modelo salvo conforme a `origem` e chama `PUT /api/dias-criticos/{data}`. Na VM, o `corredor-ml.timer` roda o script às 05h.

O `DiaCritico` só aceita a data de hoje. Na demo, o script usa as variáveis de uma data histórica e envia a data de hoje. O modelo empatou com a regra da noite quente: o [relatório de ML](../../ml/RELATORIO_ML.md) diz isso, e o job envia `origem: REGRA_BASE`.

## Arquivos da Guarda

A equipe escreve a Guarda do zero, em C#, dentro da mesma solução. O desenho segue os três pontos do professor de Cybersecurity: achar o dado pessoal no que entra, impedir que o dado marcado saia por canal inseguro e traduzir cada bloqueio para o cidadão. Os nomes de domínio e as invariantes estão na seção 4.4 do [modelo de domínio](03-modelo-de-dominio.md#44-guarda).

| Ponto do professor | Arquivo ou classe | Faz | Rubrica |
|---|---|---|---|
| 1. Parsing de PII | `Regras/pii.json` | Uma `RegraDePii` por tipo de dado: padrão, validador, máscara e severidade de 1 a 5. | parsing de PII, 3 pontos |
| 1. Parsing de PII | `MotorDeParsing.cs` | Aplica as regras a um texto ou a um JSON e devolve os achados e o `TextoRotulado`. | parsing de PII, 3 pontos |
| 1. Parsing de PII | `InterceptadorDeEntrada.cs` | Middleware. Mascara CPF, RG, CNS e credencial em qualquer campo, antes do log e do banco. Os outros tipos de dado viram rótulo. | parsing de PII, 3 pontos |
| 2. Taint | `TextoRotulado.cs` | Um texto com os tipos de dado que ele carrega. Juntar dois textos une os rótulos. `ToString()` devolve a máscara. | taint, 3 pontos |
| 2. Taint | `SaidaSegura.cs` | A única porta por onde o valor sai. Consulta a `PoliticaDeCanal` e devolve o `Veredito`. Todo bloqueio registra um evento de segurança. | taint, 3 pontos |
| 2. Taint | `ProvedorDeLogSeguro`, `FiltroDeResposta.cs`, `TratadorDeErros.cs` | Bloqueiam dado rotulado no log, em resposta de rota sem autenticação e em mensagem de erro. | taint, 3 pontos |
| 3. IA | `Tradutor.cs` | Monta o prompt com tipo de dado, canal e módulo e pede duas frases ao LLM. Sem resposta em 5 segundos, usa a frase fixa. | IA, 2 pontos |
| 3. IA | `Painel.cs` e `painel/index.html` | Mostram o farol verde, amarelo ou vermelho, que a função `Farol` calcula, e a mensagem ao cidadão de cada evento. | painel, 2 pontos |

O farol fica vermelho só com uma saída bloqueada de severidade 4 ou 5 nas últimas 24 horas. Um mascaramento na entrada vai até amarelo. O Corredor Aceso não pede CPF. Se a ACS digitar um CPF numa observação, o interceptador mascara o valor antes de gravar.

Quatro evidências vão para o PDF de Cybersecurity, e cada uma é um caso de teste:

1. Um `POST /api/idosos` com um CPF em `condicoes`. O log e o banco guardam `***.456.***-00`. É o caso 38.
2. Uma tentativa de gravar `Idoso.Nome` no log. O log recebe `[NOME]` e o painel ganha um evento. É o caso 40.
3. Uma rota sem autenticação que tentaria devolver `Idoso.Condicoes`. A resposta sai bloqueada. É o caso 41.
4. A mensagem da cena 3. É o caso 39.

## Onde cada disciplina pontua

A lista do que cada disciplina pede, com o caminho de cada item, está em [entregaveis.md](../entregaveis.md). Esta tabela resume onde olhar.

| Disciplina | Critério do enunciado | Onde está |
|---|---|---|
| SOA e Web Services | domínio 3, banco 3, migrações 1,5, documentação 1,5, testes 1 | `Corredor.Cadastro`: regras de consentimento, Postgres, migrações do EF Core, Swagger, xUnit. |
| Cybersecurity | parsing de PII 3, taint 3, IA 2, painel 2 | `Corredor.Guarda`, tabela da seção anterior. Entrega em PDF, mais o código. |
| C# Software Development | POO 3, Web API 2,5, EF Core e LINQ 1,5, async 1, qualidade 2 | `Corredor.Motor`, `Corredor.Contratos` e `Corredor.Api`: `RegraDeContato` e as quatro regras, interfaces e injeção de dependência, a ordem do radar em LINQ, o `ConsumidorMqtt` assíncrono, xUnit. |
| IA e Machine Learning | 6 critérios, com dados reais | `ml/`. Vídeo de até 5 minutos. |
| Mobile Development | telas, consumo de dados, banco, acessibilidade, aderência, documentação | `app/` e o README com a ficha e os prints. Vídeo de até 3 minutos. |
| Operating Systems | 6 itens da rubrica | `infra/`. A seção 7 do [documento de infra](05-infra-e-entrega.md#7-como-cada-item-da-rubrica-de-operating-systems-é-atendido) liga cada item a um arquivo. Vídeo de até 7 minutos. |
| Physical Computing | circuito integrado à solução | `iot/`: o nó é o sensor do radar. Link do Wokwi, vídeo de até 4 minutos e `integrantes.txt`. |
| Testing | Features 50%, Test Cases 50% | Azure Boards. Os 42 casos da seção 7 do [modelo de domínio](03-modelo-de-dominio.md#7-casos-de-teste) viram Test Cases. |

SOA e C# recebem o mesmo repositório. O README da raiz abre com a ficha e aponta, para cada disciplina, o módulo que ela avalia. Vale para todas: um repositório, uma pasta por parte, `prompts.md` na raiz e só dado simulado ou público.

## Plano até 19/11

A entrega é em 19/11/2026, às 23h55. O plano tem duas partes que andam juntas. Os portões da infra e das releases estão na seção 8 do [documento de infra](05-infra-e-entrega.md#8-plano-por-etapas-e-ordem-de-corte), com o desenho do plano. Os passos de cada módulo e a ordem de corte estão na seção 8 do [modelo de domínio](03-modelo-de-dominio.md#8-o-que-entra-em-1911).

Dez regras nunca são cortadas, porque sem uma delas a demo mostra um verde sem sinal de vida ou um vazamento. Elas estão na seção 8.3 do modelo de domínio.

## O que mudou desde a primeira versão

A primeira versão deste documento foi escrita antes do event storming. Os documentos 02 a 05 mudaram estes pontos:

- A plataforma é .NET 10, não .NET 8.
- São cinco projetos em `api/src/` e quatro de teste. `Corredor.Contratos` entrou para guardar os tipos e as interfaces que Motor e Cadastro dividem.
- São quatro situações, com `Aguardando`. O radar devolve pendentes, atendidas, aguardando e a contagem de verdes.
- São três schemas, com `guarda`.
- A chave única do evento é `(casaToken, seq, ts)`.
- O aviso ao vizinho sai do Cadastro, não do Motor.
- Os nomes da VM terminam em `.internal`, não em `.local`.
- O `02-app.sh` não instala runtime nem roda migração. As migrações rodam no deploy.
- No teste de falha, um publicador local faz o papel do nó durante a parada do Mosquitto. O buffer do nó aparece no vídeo de Physical Computing.

A tabela da seção 9 do [modelo de domínio](03-modelo-de-dominio.md#9-mudanças-em-relação-a-docsarquiteturamd) e a da seção 10 do [documento de infra](05-infra-e-entrega.md#10-mudanças-em-relação-à-arquitetura-e-ao-modelo-de-domínio) dão o motivo de cada mudança. Nas duas tabelas, "a arquitetura" e `arquitetura.md` querem dizer a primeira versão, que está no histórico do Git.

## Onde os documentos ainda divergem

Em 17/11, com as sete partes juntas na `main`, cada ponto desta tabela foi conferido contra o código. Os que o código já resolveu saíram da lista e estão no fim da seção.

| Ponto | Um documento diz | O código faz | Vale |
|---|---|---|---|
| Nome das tabelas no ensaio | O ensaio da infra cita `motor."Eventos"` e `cadastro."Idosos"`, da API mínima do ensaio. | As migrações criam `motor.evento_sensor` e `cadastro.idoso`. | O modelo de dados. O texto do ensaio fica como registro do que foi medido. |
| Conferência de migração aditiva | O documento de infra usa `tools/conferir-migracoes.sh`, que lê o `Up()` em C#. | O CI roda o `tools/conferir-migracoes.sh`. O `conferir_migracao_aditiva.sh` do modelo de dados fica em `docs/arquitetura/referencia/dados/` e não está no CI. | O de `tools/`. A seção 11.4 do modelo de dados explica a diferença. |
| Autenticação | O modelo de domínio não define. | O host lê um token por ACS da configuração e o app manda esse token em `Authorization: Bearer` (`api/README.md`, "Autenticação"). `PUT /api/dias-criticos/{data}` aceita chamada sem token, para o job de ML. | `api/README.md`. |
| Campo `diaCritico` em `GET /radar` | A seção 11.3 do modelo de dados deixa em aberto. | O radar não traz o campo. O app usa só `turnoAberto`. | Sem o campo. |
| Painel Risco Cidadão | A seção 6.3 põe a página em `/painel`, servida pelo Nginx. | Na VM é assim. No notebook, sem Nginx, a página sai de `/api/guarda/demo/painel`, que só existe em `Development`. `GET /api/painel` traz o campo `canais`, a mais que o exemplo da seção 6.3. | Os dois caminhos. |
| Casa do teste de falha | O documento de infra só diz que o seed ativa `fa11...`. | A carga ativa essa casa na microárea 999, sem ACS, para ela não aparecer no radar da microárea 3. | O código. |

Resolvidos pelo código: o .NET é o 10 em todos os documentos; são cinco projetos em `api/src/` e quatro de teste; o esqueleto existe; as migrações ficam em `Infra/Migrations` de cada módulo, como em `entregaveis.md`; a semente fica em `api/semente/`, como no modelo de dados.

## O que a equipe precisa confirmar

- Os professores de SOA e de C# aceitam o mesmo repositório, com um módulo para cada disciplina? O texto do enunciado permite. Uma mensagem aos dois antes de 19/11 tira a dúvida.
- A entrevista com uma ACS real aconteceu? O pitch e a ficha ainda citam personas simuladas. As perguntas que só uma pessoa real responde estão no fim da sessão 1 do [event storming](02-event-storming.md#perguntas-que-só-uma-pessoa-real-responde).

## Outros documentos

Leia nesta ordem:

1. [02-event-storming.md](02-event-storming.md): os fatos do domínio, as políticas, os hotspots e os oito fluxos.
2. [03-modelo-de-dominio.md](03-modelo-de-dominio.md): contextos, projetos, glossário, agregados, regras da situação da casa, contratos e casos de teste.
3. [04-modelo-de-dados.md](04-modelo-de-dados.md): tabelas, mapeamento do EF Core, retenção, classificação dos dados, SQLite do app e carga da demo.
4. [05-infra-e-entrega.md](05-infra-e-entrega.md): VM, provisionamento, CI, releases, deploy, operação e plano.
5. [diagramas/GUIA.md](diagramas/GUIA.md): o tipo de diagrama de cada vista, o tema e como renderizar.

O [README desta pasta](README.md) lista todos os documentos e todos os diagramas.
