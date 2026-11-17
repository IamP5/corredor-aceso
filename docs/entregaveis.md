# Entregáveis por disciplina

Fonte: PDF "GS CitizenTech - 3ES Fev - FIAP 2026" e o documento "Explicação: Global Solutions" do professor de Cybersecurity. Prazo de todas as entregas: 19/11/2026 às 23h55, na tarefa de cada disciplina no Teams ou no Portal do Aluno. Um integrante envia cada disciplina.

A coluna "Quem faz" separa o que sai do repositório do que só uma pessoa da equipe consegue fazer, como gravar um vídeo ou enviar no Teams. A coluna "Situação" diz o que existe hoje no repositório, conferido em 17/11/2026 com as peças rodando juntas ([relatório da integração](evidencias/integracao/RELATORIO.md)):

- **pronto**: o arquivo existe e foi visto funcionando.
- **parcial**: existe, mas falta uma parte, dita na linha.
- **falta**: ainda não existe no repositório.
- **equipe**: só uma pessoa da equipe consegue fazer.

## Vale para todas

| Item | Onde | Quem faz | Situação |
|---|---|---|---|
| Ficha da CitizenTech no início do README | `README.md` | repositório | pronto |
| ODS identificados e explicados | `README.md` e `docs/discovery/01-ideia-vencedora.md` | repositório | pronto |
| Registro dos prompts de IA | `prompts.md` | repositório | pronto |
| Nenhum dado pessoal real, fonte dos dados públicos declarada | `ml/dados/README.md`, verificador em `docs/arquitetura/referencia/dados/semente/verificar_dados_pessoais.py` | repositório | pronto |
| Uma pasta por parte, README claro | raiz | repositório | parcial: `infra/` ainda não tem `README.md` |
| Nomes dos integrantes | `integrantes.txt` | equipe | equipe: o arquivo existe com uma linha a preencher |
| Todo integrante sabe explicar o projeto inteiro | | equipe | equipe |

## SOA e Web Services

Nota: domínio 3,0, conexão com banco 3,0, migrações 1,5, documentação automática 1,5, testes automatizados 1,0. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Código completo do backend | `api/` | repositório | pronto |
| Estruturação do domínio | `api/src/Corredor.Cadastro`, `docs/arquitetura/03-modelo-de-dominio.md` | repositório | pronto |
| Conexão com banco de dados | Postgres, `CadastroDbContext`, papel `corredor_cadastro` | repositório | pronto: a API integrada rodou com um papel por módulo |
| Migrações | `api/src/Corredor.Cadastro/Infra/Migrations` | repositório | pronto: aplicadas duas vezes como `corredor_cadastro_migra` |
| Documentação dos endpoints | Swagger em `/swagger` | repositório | pronto |
| Testes automatizados | `api/tests/Corredor.Cadastro.Testes` | repositório | pronto |
| README com instruções, descrição e nomes | `api/README.md` | repositório, nomes da equipe | parcial: faltam os nomes, que vêm de `integrantes.txt` |
| Evidências da aplicação | `docs/evidencias/integracao/`, `docs/evidencias/cyber/evidencia-integrada-cadastro.txt` | repositório e prints da equipe | parcial: há o log das rotas e o relatório; faltam prints do Swagger |

## Cybersecurity

Nota: parsing de PII 3,0, taint analysis 3,0, IA na remediação 2,0, dashboard Risco Cidadão 2,0. Entrega: um PDF com a documentação e, separado, um ZIP ou o link do GitHub com o código.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Interceptador que detecta e mascara PII na entrada | `api/src/Corredor.Guarda/Rotas/InterceptadorDeEntrada.cs` | repositório | pronto |
| Bloqueio de PII em log e em resposta sem autenticação | `api/src/Corredor.Guarda` | repositório | pronto |
| Integração com LLM que traduz o evento técnico | `api/src/Corredor.Guarda/Aplicacao/Tradutor.cs` | repositório, chave de API da equipe | pronto: tradução do OpenRouter vista na cena 3 |
| Painel com farol e mensagem traduzida | `/painel` na VM; no notebook, `/api/guarda/demo/painel` | repositório | pronto |
| PDF com Source, Auditoria e Vídeo do Dashboard, com prints | `docs/evidencias/cyber/`, `docs/entrega/cyber-documentacao.md` | repositório gera o rascunho, equipe grava o vídeo | parcial: rascunho e prints prontos; vídeo e PDF são da equipe |

## C# Software Development

Nota: POO 3,0, Web API 2,5, EF Core e LINQ 1,5, async e await 1,0, qualidade do código 2,0. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Projeto C# com POO, interfaces e injeção de dependência | `api/src/Corredor.Motor`, `api/src/Corredor.Contratos` | repositório | pronto |
| Web API | `api/src/Corredor.Api` | repositório | pronto |
| Entity Framework Core e LINQ | `MotorDbContext`, consulta do radar | repositório | pronto |
| Programação assíncrona | consumidor MQTT e casos de uso | repositório | pronto: o consumidor MQTT recebeu os pacotes do simulador |
| Testes unitários | `api/tests/Corredor.Motor.Testes` | repositório | pronto |
| Documentação dos endpoints | Swagger em `/swagger` | repositório | pronto |
| README e evidências | `api/README.md`, `docs/evidencias/integracao/` | repositório e prints da equipe | parcial: faltam prints do Swagger |

## IA e Machine Learning

Nota: problema e dados reais 2,0, escolha justificada do modelo 1,0, preparação e divisão 2,0, treino com parâmetros diferentes 2,0, resultados em treino e teste 2,0, conclusão para o cidadão 1,0. Dados simulados não são aceitos. Entrega: links do repositório e do vídeo.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Notebook executado | `ml/corredor_aceso.ipynb` | repositório | pronto |
| Fonte, link e período dos dados | `ml/dados/README.md` | repositório | pronto |
| Dependências | `ml/requirements.txt` | repositório | pronto |
| Relatório de 2 a 4 páginas | `ml/RELATORIO_ML.md` | repositório | pronto |
| Ao menos 10 combinações de 3 hiperparâmetros, validação cruzada só no treino | notebook | repositório | pronto: 24 combinações |
| Job que abre o turno | `ml/prever_hoje.py` | repositório | pronto: abriu o turno da API integrada na cena 1 |
| Vídeo de até 5 minutos no YouTube, não listado | roteiro em `ml/README.md` | equipe | equipe |

## Mobile Development e IoT

Nota em 6 critérios: telas e navegação, consumo de dados, banco de dados, acessibilidade, aderência ao problema e documentação. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| App Expo com React Native, TypeScript e Expo Router | `app/` | repositório | pronto |
| Consumo de API ou mock | `app/src/api` | repositório | pronto: o alvo web falou com a API real |
| Banco de dados local | `app/src/banco`, expo-sqlite | repositório | pronto |
| Acessibilidade | rótulos, contraste e texto junto da cor | repositório | pronto |
| README com a Ficha, instruções e prints das telas | `app/README.md`, `docs/evidencias/mobile/`, `docs/evidencias/integracao/` | repositório e prints da equipe | pronto |
| Vídeo de até 3 minutos com o app rodando | roteiro em `app/README.md` | equipe | equipe |

## Operating Systems

Nota: script de infra (rede, DNS, IP) 2, script da aplicação 2, script de monitoramento com alertas de CPU, disco e memória 2, monitoramento como serviço tolerante a falhas 1, logs de auditoria e execução 1, funcionamento completo 2. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Provisionamento de rede, DNS, IP e hostname | `infra/01-rede.sh` | repositório | pronto, não rodado na integração |
| Provisionamento da aplicação | `infra/02-app.sh` | repositório | pronto, não rodado na integração |
| Monitor de CPU, disco e memória com alertas | `infra/monitor.sh`, `infra/03-monitor.sh` | repositório | pronto, não rodado na integração |
| Monitor como serviço tolerante a falhas | `infra/systemd/corredor-monitor.service` | repositório | pronto, não rodado na integração |
| Logs de auditoria e de execução | `infra/lib/auditoria.sh` | repositório | pronto, não rodado na integração |
| Teste de falha | `infra/teste-de-falha.sh` | repositório | pronto, não rodado na integração |
| README e evidências técnicas | `infra/README.md`, `docs/evidencias/os/` | repositório | falta: nenhum dos dois existe |
| Vídeo de até 7 minutos com o provisionamento automático na VM | roteiro em `docs/arquitetura/05-infra-e-entrega.md` | equipe | equipe |

## Physical Computing: IoT e IoB

Entrega: link do projeto no Wokwi, link do vídeo e um arquivo `.txt` com os nomes.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Circuito ESP32 com sensores, integrado à solução | `iot/no/`, simulador em `iot/simulador/` | repositório | pronto: o simulador falou com o Motor real; o nó rodou no wokwi.com contra o broker público, não contra a API |
| Link do projeto no Wokwi | `iot/README.md` | equipe publica no Wokwi | equipe: "ainda não publicado" |
| Vídeo de até 4 minutos com a integração | roteiro em `iot/README.md` | equipe | equipe |
| Arquivo com o nome dos integrantes | `integrantes.txt` | equipe | equipe: preencher a linha marcada |

## Testing, Compliance e Quality Assurance

Nota: Features 50%, Test Cases 50%. Entrega: PDF com o link do projeto no Azure Boards e os prints. O professor entra no projeto como Admin. O link do vídeo de pitch também vai nesta disciplina.

| Item pedido | Onde | Quem faz | Situação |
|---|---|---|---|
| Features com descrição e critérios de aceite | `docs/qa/features.md`, depois Azure Boards | repositório escreve, equipe cadastra | parcial: escrito; cadastro no Azure é da equipe |
| Test Cases funcionais, positivos e negativos, com objetivo, preparação, entradas, saídas esperadas e passos | `docs/qa/casos-de-teste.md`, depois Azure Boards | repositório escreve, equipe cadastra | parcial: escrito; cadastro no Azure é da equipe |
| Professor como Admin no projeto do Azure | | equipe | equipe |
| PDF com o link e os prints | | equipe | equipe |
| Vídeo de pitch de até 4 minutos no YouTube | | equipe | equipe |
