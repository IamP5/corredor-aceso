# Checklist de entrega, 18/11 e 19/11

Prazo de todas as disciplinas: **19/11/2026 às 23h55**, na tarefa de cada disciplina no Teams ou no Portal do Aluno. Um integrante envia cada disciplina ([entregaveis.md](../entregaveis.md)). Esta lista tem só o que uma pessoa precisa fazer: gravar, publicar, cadastrar, montar PDF e enviar. O que sai do repositório já está nas pastas de cada parte.

Os papéis vêm do plano da equipe em [01-ideia-vencedora.md](../discovery/01-ideia-vencedora.md), seção 7: **P1** ESP32 e Linux, **P2** SOA e Cyber, **P3** C#, **P4** ML, **P5** Expo, Azure Boards e pitch. Escreva o nome de cada pessoa ao lado do papel antes de começar:

| Papel | Nome |
|---|---|
| P1 | |
| P2 | |
| P3 | |
| P4 | |
| P5 | |

Os roteiros dos vídeos estão em [roteiros.md](roteiros.md). O esqueleto do PDF de Cyber está em [cyber-documentacao.md](cyber-documentacao.md). A importação do Azure está em [../qa/README.md](../qa/README.md).

## Antes de tudo, 18/11 de manhã

- [x] **P1.** Criar `integrantes.txt` na raiz com o nome completo de cada integrante, um por linha. O `README.md` e a entrega de Physical Computing apontam para ele.
- [ ] **P1.** Conferir que o repositório `IamP5/corredor-aceso` está público e abre numa janela anônima.
- [ ] **Todos.** Rodar `npm run check` na `main` depois da junção das branches. Nenhum vídeo é gravado com o check vermelho.
- [ ] **P1.** Publicar a release que os vídeos vão usar (`v1.0.0-rc.1`, [roteiro-os.md](roteiro-os.md)).

## Ordem de trabalho

A ordem segue as dependências. O token da casa 1 só vai para o firmware do Wokwi depois da carga final na VM (seção 8.2 de [04-modelo-de-dados.md](../arquitetura/04-modelo-de-dados.md)), então o vídeo de Physical Computing vem depois do de Operating Systems. O relógio de demo só existe com a API em `Development`, que é o caso da VM local (seção 1.11 de 05-infra-e-entrega.md).

| Quando | O quê | Quem |
|---|---|---|
| 18/11, manhã | Importar Features e Test Cases no Azure. Pôr o professor de Testing como Admin. | P5 |
| 18/11, manhã | Gravar o vídeo de Operating Systems na VM nova | P1 |
| 18/11, início da tarde | Publicar o Wokwi (casa de teste) e conferir o painel no Pages | P1 |
| 18/11, tarde | Gravar o vídeo de Physical Computing | P1 |
| 18/11, tarde | Prints de SOA e de C# (Swagger, testes, migrações) | P2 e P3 |
| 18/11, noite | Gravar o vídeo de Mobile | P5 |
| 19/11, manhã | Gravar o vídeo de ML | P4 |
| 19/11, manhã | Gravar o vídeo do dashboard de Cyber | P2 |
| 19/11, manhã | Gravar o pitch | P5, com todos |
| 19/11, tarde | Montar o PDF de Cyber e o PDF de Testing. Executar os Test Cases no Azure e tirar os prints. | P2 e P5 |
| 19/11, até 22h | Enviar as oito disciplinas | cada dono |
| 19/11, 22h a 23h | Conferir cada envio aberto no Teams. Reenviar o que faltar. | P5 |

Envie até as 22h. A hora que sobra é para um link que não abre.

## Testing, Compliance e Quality Assurance

Nota: Features 50%, Test Cases 50%. Entrega: PDF com o link do projeto no Azure Boards e os prints. O professor entra no projeto como Admin. O link do vídeo de pitch também vai nesta disciplina.

- [ ] **P5, 18/11.** Criar o projeto no Azure DevOps com o processo **Scrum**, porque nele a Feature tem o campo Acceptance Criteria. Com o processo Agile, use `features-agile.csv`.
- [ ] **P5, 18/11.** Importar `docs/qa/azure/features.csv` em Boards, Queries, Import work items. Conferir 1 épico e 29 Features e salvar.
- [ ] **P5, 18/11.** Criar um Test Plan e, para cada Feature, uma suíte baseada em requisito. Importar em cada suíte o arquivo da Feature, gerado com a área e o responsável reais (passo a passo em [../qa/README.md](../qa/README.md)). A suíte baseada em requisito liga cada Test Case à Feature.
- [ ] **P5, 18/11.** Pôr o professor como Admin do projeto: Project settings, Permissions, Project Administrators, adicionar o e-mail institucional dele.
- [ ] **P5, 19/11.** Executar no Azure Test Plans os casos que já rodam na demo e marcar Passed ou Failed. Um caso que falha fica Failed, com o motivo.
- [ ] **P5, 19/11.** Montar o PDF de Testing: link do projeto, prints do backlog de Features, de uma Feature aberta com os critérios de aceite, da árvore de suítes, de um Test Case aberto com os passos, de um caso positivo e de um negativo, e do gráfico de resultados. Colocar o link do pitch na primeira página.
- [ ] **P5, 19/11.** Enviar no Teams: o PDF. No texto do envio: o link do projeto no Azure e o link do pitch no YouTube.

| Envia | O quê |
|---|---|
| P5 | PDF com o link do Azure Boards, os prints e o link do vídeo de pitch de até 4 minutos |

## Pitch (vai em Testing)

- [ ] **P5 com todos, 19/11.** Gravar o pitch de **até 4 minutos** pelo roteiro de [roteiros.md](roteiros.md).
- [ ] **P5.** Publicar no YouTube e conferir que o link abre sem login.
- [ ] **P5.** Pôr o link no PDF de Testing e no `README.md`.

## Cybersecurity

Nota: parsing de PII 3,0, taint analysis 3,0, IA na remediação 2,0, dashboard Risco Cidadão 2,0. Entrega: um PDF com a documentação e, separado, um ZIP ou o link do GitHub com o código.

- [ ] **P2, 18/11.** Configurar a chave do LLM na VM em `/etc/corredor/` (variável `Llm__ApiKey`, seção 5 de 05-infra-e-entrega.md). A chave nunca entra no Git.
- [ ] **P2, 19/11.** Rodar as quatro evidências da auditoria e tirar os prints na ordem de [cyber-documentacao.md](cyber-documentacao.md).
- [ ] **P2, 19/11.** Gravar o vídeo do dashboard pelo roteiro. Os documentos não dão limite de tempo para este vídeo. Mantenha perto de 3 minutos.
- [ ] **P2, 19/11.** Preencher o esqueleto, salvar em `docs/evidencias/cyber/` e gerar o PDF.
- [ ] **P2, 19/11.** Enviar no Teams dois arquivos separados: o PDF e o link do GitHub (ou o ZIP do repositório).

| Envia | O quê |
|---|---|
| P2 | PDF com Source, Auditoria e Vídeo do Dashboard. Separado: link do GitHub ou ZIP. |

## SOA e Web Services

Nota: domínio 3,0, conexão com banco 3,0, migrações 1,5, documentação automática 1,5, testes automatizados 1,0. Entrega: link do GitHub.

- [ ] **P2, 18/11.** Escrever os nomes da equipe em `api/README.md`.
- [ ] **P2, 18/11.** Prints em `docs/evidencias/soa/`: Swagger com as rotas do Cadastro, `POST /api/idosos` e `POST /api/idosos/{id}/consentimento` respondendo, as tabelas do schema `cadastro`, a pasta `Migrations` e o resultado do `dotnet test` de `Corredor.Cadastro.Testes`.
- [ ] **P2, 19/11.** Enviar no Teams o link do GitHub.

| Envia | O quê |
|---|---|
| P2 | Link do GitHub |

## C# Software Development

Nota: POO 3,0, Web API 2,5, EF Core e LINQ 1,5, async e await 1,0, qualidade do código 2,0. Entrega: link do GitHub.

- [ ] **P3, 18/11.** Prints em `docs/evidencias/csharp/`: Swagger com as rotas do Motor, `GET /api/radar` com pendentes e atendidas, a classe `RegraDeContato` e as quatro regras, a consulta LINQ do radar, o `ConsumidorMqtt` assíncrono e o resultado do `dotnet test` de `Corredor.Motor.Testes`.
- [ ] **P3, 19/11.** Enviar no Teams o link do GitHub.

| Envia | O quê |
|---|---|
| P3 | Link do GitHub |

## IA e Machine Learning

Nota: problema e dados reais 2,0, escolha justificada do modelo 1,0, preparação e divisão 2,0, treino com parâmetros diferentes 2,0, resultados em treino e teste 2,0, conclusão para o cidadão 1,0. Dados simulados não são aceitos. Entrega: links do repositório e do vídeo.

- [ ] **P4, 18/11.** Conferir que `ml/corredor_aceso.ipynb` está executado, com a saída de cada célula salva no arquivo.
- [ ] **P4, 18/11.** Conferir que `ml/dados/README.md` tem fonte, link e período do INMET e do SIH.
- [ ] **P4, 19/11.** Gravar o vídeo de **até 5 minutos** pelo roteiro.
- [ ] **P4, 19/11.** Publicar no YouTube como **não listado** e conferir que abre sem login.
- [ ] **P4, 19/11.** Enviar no Teams o link do repositório e o link do vídeo.

| Envia | O quê |
|---|---|
| P4 | Link do repositório e link do vídeo no YouTube, não listado |

## Mobile Development e IoT

Nota em 6 critérios: telas e navegação, consumo de dados, banco de dados, acessibilidade, aderência ao problema e documentação. Entrega: link do GitHub.

- [ ] **P5, 18/11.** Prints das telas em `app/README.md`: entrada, dia crítico, radar, detalhe da casa, mensagem bloqueada e encerramento do turno, e o radar sem conexão.
- [ ] **P5, 18/11.** Gravar o vídeo de **até 3 minutos** com o app rodando, pelo roteiro.
- [ ] **P5, 18/11.** Publicar o vídeo e pôr o link em `app/README.md`.
- [ ] **P5, 19/11.** Enviar no Teams o link do GitHub. No texto do envio, o link do vídeo.

| Envia | O quê |
|---|---|
| P5 | Link do GitHub, com o link do vídeo no texto e no `app/README.md` |

## Operating Systems

Nota: script de infra (rede, DNS, IP) 2, script da aplicação 2, monitoramento com alertas de CPU, disco e memória 2, monitoramento como serviço tolerante a falhas 1, logs de auditoria e execução 1, funcionamento completo 2. Entrega: link do GitHub.

- [ ] **P1, 18/11.** Ensaiar a sequência de roteiro-os.md uma vez e anotar o tempo do provisionamento.
- [ ] **P1, 18/11.** Rodar `multipass delete --purge corredor-ubs` e gravar o vídeo de **até 7 minutos** numa VM nova, pelo roteiro.
- [ ] **P1, 18/11.** Prints em `docs/evidencias/os/`: saída do provisionamento, segunda execução com `0 mudança(s)`, `sudo corredor status`, `alertas.log` com `ALERTA` e `NORMALIZADO`, resumo do teste de falha com os PASSOU e `sudo corredor logs auditoria`.
- [ ] **P1, 18/11.** Publicar o vídeo e pôr o link em `infra/README.md`.
- [ ] **P1, 19/11.** Enviar no Teams o link do GitHub. No texto do envio, o link do vídeo.

| Envia | O quê |
|---|---|
| P1 | Link do GitHub, com o link do vídeo no texto e no `infra/README.md` |

## Physical Computing: IoT e IoB

Entrega: link do projeto no Wokwi, link do vídeo e um arquivo `.txt` com os nomes.

- [ ] **P1, 18/11.** Salvar o projeto no Wokwi como público, com a casa de teste e `SILENCIO_DEMO_S 90`, e pôr o link em `iot/README.md`. Esta entrega não usa a VM nem o token da casa 1.
- [ ] **P1, 18/11.** Conferir o painel publicado em <https://iamp5.github.io/corredor-aceso/painel/> depois do push.
- [x] **P1, 19/11.** Gravar o vídeo de **até 4 minutos** com a integração, pelo roteiro: [youtu.be/XH_M3QyMZsQ](https://youtu.be/XH_M3QyMZsQ).
- [ ] **P1, 19/11.** Enviar no Teams o link do Wokwi, o link do vídeo e o arquivo `integrantes.txt`.

| Envia | O quê |
|---|---|
| P1 | Link do Wokwi, link do vídeo de até 4 minutos e `integrantes.txt` |

## Conferência final, 19/11 às 22h

- [ ] Cada link abre numa janela anônima: repositório, Wokwi, os seis vídeos e o projeto do Azure com o professor como Admin.
- [ ] O `README.md` tem a ficha, os links dos vídeos e o link do Azure.
- [ ] `prompts.md` registra os prompts usados até o fim.
- [ ] Nenhum arquivo do repositório tem chave, token de casa, e-mail ou telefone real. A varredura da seção 8.3 de 04-modelo-de-dados.md e `python3 docs/qa/ferramentas/conferir.py` passam.
- [ ] As oito tarefas do Teams mostram "entregue".
