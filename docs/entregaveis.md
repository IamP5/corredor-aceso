# Entregáveis por disciplina

Fonte: PDF "GS CitizenTech - 3ES Fev - FIAP 2026" e o documento "Explicação: Global Solutions" do professor de Cybersecurity. Prazo de todas as entregas: 19/11/2026 às 23h55, na tarefa de cada disciplina no Teams ou no Portal do Aluno. Um integrante envia cada disciplina.

A coluna "Quem faz" separa o que sai do repositório do que só uma pessoa da equipe consegue fazer, como gravar um vídeo ou enviar no Teams.

## Vale para todas

| Item | Onde | Quem faz |
|---|---|---|
| Ficha da CitizenTech no início do README | `README.md` | repositório |
| ODS identificados e explicados | `README.md` e `docs/discovery/01-ideia-vencedora.md` | repositório |
| Registro dos prompts de IA | `prompts.md` | repositório |
| Nenhum dado pessoal real, fonte dos dados públicos declarada | `ml/dados/README.md`, verificador em `tools/` | repositório |
| Uma pasta por parte, README claro | raiz | repositório |
| Todo integrante sabe explicar o projeto inteiro | | equipe |

## SOA e Web Services

Nota: domínio 3,0, conexão com banco 3,0, migrações 1,5, documentação automática 1,5, testes automatizados 1,0. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Código completo do backend | `api/` | repositório |
| Estruturação do domínio | `api/src/Corredor.Cadastro`, `docs/arquitetura/03-modelo-de-dominio.md` | repositório |
| Conexão com banco de dados | Postgres, `CadastroDbContext` | repositório |
| Migrações | `api/src/Corredor.Cadastro/Migrations` | repositório |
| Documentação dos endpoints | Swagger em `/swagger` | repositório |
| Testes automatizados | `api/tests/Corredor.Cadastro.Testes` | repositório |
| README com instruções, descrição e nomes | `api/README.md` | repositório, nomes da equipe |
| Evidências da aplicação | `docs/evidencias/soa/` | repositório e prints da equipe |

## Cybersecurity

Nota: parsing de PII 3,0, taint analysis 3,0, IA na remediação 2,0, dashboard Risco Cidadão 2,0. Entrega: um PDF com a documentação e, separado, um ZIP ou o link do GitHub com o código.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Interceptador que detecta e mascara PII na entrada | `api/src/Corredor.Guarda` | repositório |
| Bloqueio de PII em log e em resposta sem autenticação | `api/src/Corredor.Guarda` | repositório |
| Integração com LLM que traduz o evento técnico | `api/src/Corredor.Guarda` | repositório, chave de API da equipe |
| Painel com farol e mensagem traduzida | `/painel` | repositório |
| PDF com Source, Auditoria e Vídeo do Dashboard, com prints | `docs/evidencias/cyber/` | repositório gera o rascunho, equipe grava o vídeo |

## C# Software Development

Nota: POO 3,0, Web API 2,5, EF Core e LINQ 1,5, async e await 1,0, qualidade do código 2,0. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Projeto C# com POO, interfaces e injeção de dependência | `api/src/Corredor.Motor`, `api/src/Corredor.Contratos` | repositório |
| Web API | `api/src/Corredor.Api` | repositório |
| Entity Framework Core e LINQ | `MotorDbContext`, consulta do radar | repositório |
| Programação assíncrona | consumidor MQTT e casos de uso | repositório |
| Testes unitários | `api/tests/Corredor.Motor.Testes` | repositório |
| Documentação dos endpoints | Swagger em `/swagger` | repositório |
| README e evidências | `api/README.md`, `docs/evidencias/csharp/` | repositório e prints da equipe |

## IA e Machine Learning

Nota: problema e dados reais 2,0, escolha justificada do modelo 1,0, preparação e divisão 2,0, treino com parâmetros diferentes 2,0, resultados em treino e teste 2,0, conclusão para o cidadão 1,0. Dados simulados não são aceitos. Entrega: links do repositório e do vídeo.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Notebook executado | `ml/corredor_aceso.ipynb` | repositório |
| Fonte, link e período dos dados | `ml/dados/README.md` | repositório |
| Dependências | `ml/requirements.txt` | repositório |
| Relatório de 2 a 4 páginas | `ml/RELATORIO_ML.md` | repositório |
| Ao menos 10 combinações de 3 hiperparâmetros, validação cruzada só no treino | notebook | repositório |
| Vídeo de até 5 minutos no YouTube, não listado | | equipe |

## Mobile Development e IoT

Nota em 6 critérios: telas e navegação, consumo de dados, banco de dados, acessibilidade, aderência ao problema e documentação. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz |
|---|---|---|
| App Expo com React Native, TypeScript e Expo Router | `app/` | repositório |
| Consumo de API ou mock | `app/src/api` | repositório |
| Banco de dados local | `app/src/banco`, expo-sqlite | repositório |
| Acessibilidade | rótulos, contraste e texto junto da cor | repositório |
| README com a Ficha, instruções e prints das telas | `app/README.md` | repositório e prints da equipe |
| Vídeo de até 3 minutos com o app rodando | | equipe |

## Operating Systems

Nota: script de infra (rede, DNS, IP) 2, script da aplicação 2, script de monitoramento com alertas de CPU, disco e memória 2, monitoramento como serviço tolerante a falhas 1, logs de auditoria e execução 1, funcionamento completo 2. Entrega: link do GitHub.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Provisionamento de rede, DNS, IP e hostname | `infra/01-rede.sh` | repositório |
| Provisionamento da aplicação | `infra/02-app.sh` | repositório |
| Monitor de CPU, disco e memória com alertas | `infra/monitor.sh`, `infra/03-monitor.sh` | repositório |
| Monitor como serviço tolerante a falhas | `infra/systemd/corredor-monitor.service` | repositório |
| Logs de auditoria e de execução | `infra/lib/auditoria.sh` | repositório |
| Teste de falha | `infra/teste-de-falha.sh` | repositório |
| README e evidências técnicas | `infra/README.md`, `docs/evidencias/os/` | repositório |
| Vídeo de até 7 minutos com o provisionamento automático na VM | roteiro em `docs/arquitetura/05-infra-e-entrega.md` | equipe |

## Physical Computing: IoT e IoB

Entrega: link do projeto no Wokwi, link do vídeo e um arquivo `.txt` com os nomes.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Circuito ESP32 com sensores, integrado à solução | `iot/no/` | repositório |
| Link do projeto no Wokwi | `iot/README.md` | equipe publica no Wokwi |
| Vídeo de até 4 minutos com a integração | | equipe |
| Arquivo com o nome dos integrantes | `integrantes.txt` | equipe |

## Testing, Compliance e Quality Assurance

Nota: Features 50%, Test Cases 50%. Entrega: PDF com o link do projeto no Azure Boards e os prints. O professor entra no projeto como Admin. O link do vídeo de pitch também vai nesta disciplina.

| Item pedido | Onde | Quem faz |
|---|---|---|
| Features com descrição e critérios de aceite | `docs/qa/features.md`, depois Azure Boards | repositório escreve, equipe cadastra |
| Test Cases funcionais, positivos e negativos, com objetivo, preparação, entradas, saídas esperadas e passos | `docs/qa/casos-de-teste.md`, depois Azure Boards | repositório escreve, equipe cadastra |
| Professor como Admin no projeto do Azure | | equipe |
| PDF com o link e os prints | | equipe |
| Vídeo de pitch de até 4 minutos no YouTube | | equipe |
