# Requisitos da Global Solution, por disciplina

Global Solution FIAP 2026.2, CitizenTech. Engenharia de Software, 3º ano, turma de fevereiro. Projeto: **Corredor Aceso**.

Aqui estão a Ficha da CitizenTech e os requisitos de cada disciplina: critérios, pontos, formato de entrega e onde cada item está no repositório. As fontes são o PDF "GS CitizenTech - 3ES Fev - FIAP 2026" e o documento "Explicação: Global Solutions", do Prof. Vitor Lasse, de Cybersecurity.

O passo a passo para conferir cada disciplina está no [Guia de avaliação do Corredor Aceso](https://iamp5.github.io/corredor-aceso/). Para rodar a solução, basta o Docker: `docker compose up --build` na raiz sobe o banco, a API, o simulador das casas e o app. O detalhe está em [Como rodar](README.md#como-rodar).

O prazo de todas as entregas é 19/11/2026, às 23h55, na tarefa de cada disciplina no Teams ou no Portal do Aluno. Um integrante envia a entrega de cada disciplina.

## Ficha da CitizenTech

A Ficha é a mesma em todas as disciplinas e está no início do [README da raiz](README.md) e do [README do app](app/README.md).

| Campo | Conteúdo |
|---|---|
| Nome da CitizenTech | Corredor Aceso |
| Cidade e bairro | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da Estratégia Saúde da Família, com o Jardim Helena como referência. |
| Cidadão beneficiado | Idosos de 60 anos ou mais que moram sozinhos, e casas com pessoa que depende de aparelho na tomada. Quem opera é a agente comunitária de saúde (ACS). |
| Problema, em uma frase | Em dia de calor extremo ou de apagão, a agente comunitária de saúde (ACS) não tem tempo de visitar todos os idosos que moram sozinhos na microárea e não tem como saber quais deles estão sem dar sinal de vida. |
| Como funciona | Um aparelho instalado no corredor da casa acende a luz quando a pessoa idosa passa à noite. Cada passagem fica registrada como sinal de vida daquela casa. Às 05h, o sistema verifica, com dados públicos do INMET, se a noite foi quente. Quando foi, o dia é declarado crítico. No dia crítico, o aplicativo da ACS lista apenas as casas que ficaram sem sinal de vida, e são essas que ela visita. A casa cujo aparelho parou de transmitir aparece em cinza e nunca é mostrada como verde. Um modelo de ML treinado com dados do INMET e do SIH/DATASUS foi comparado à regra da noite quente e não a superou. Por isso, a regra decide o dia crítico. A luz também atende a um ponto levantado na descoberta, feita com personas simuladas: com o corredor escuro e o interruptor longe, a pessoa idosa bebe menos água para não precisar levantar à noite. Com o caminho iluminado, ela pode se hidratar sem medo de cair. |
| O que cada parte faz | Ver "Partes do repositório", no [README da raiz](README.md#partes-do-repositório). |
| Link do repositório | https://github.com/IamP5/corredor-aceso |
| Guia de avaliação | https://iamp5.github.io/corredor-aceso/ |
| Indicador de impacto | Percentual de idosos sozinhos da microárea com contato confirmado até as 10h em dia crítico. Meta: 100%. Indicador de segurança: nenhuma casa em verde sem sinal de vida. |
| ODS conectados | ODS 3 (metas 3.4 e 3.d), ODS 10 (meta 10.2), ODS 11 (meta 11.5) e ODS 13 (meta 13.1). |

Os dados de pessoas neste repositório são simulados. Os dados de clima e de internações são públicos, do INMET e do SIH/DATASUS.

## Vale para todas as disciplinas

| Requisito | Onde está |
|---|---|
| Ficha da CitizenTech no início do README | [README.md](README.md#ficha-da-citizentech) e acima |
| ODS identificados e explicados | [README.md](README.md) e [docs/discovery/01-ideia-vencedora.md](docs/discovery/01-ideia-vencedora.md) |
| Registro dos prompts de IA usados | [prompts.md](prompts.md) |
| Nenhum dado pessoal real e fonte dos dados públicos declarada | [ml/dados/README.md](ml/dados/README.md) |
| Uma pasta por parte, cada uma com README | `api/`, `app/`, `iot/`, `ml/`, `infra/`, `docs/qa/` |
| Nomes e RMs dos integrantes | [integrantes.txt](integrantes.txt) |
| Todo integrante sabe explicar o projeto inteiro | apresentação da equipe |

## Resumo das entregas

| Disciplina | O que vai na entrega | Vídeo |
|---|---|---|
| SOA e Web Services | link do GitHub | não |
| C# Software Development | link do GitHub | não |
| Cybersecurity | um PDF com a documentação e, separado, o ZIP ou o link do GitHub com o código | dentro do PDF |
| IA e Machine Learning | link do GitHub e link do vídeo | até 5 min, YouTube não listado |
| Mobile Development | link do GitHub e link do vídeo | até 3 min |
| Operating Systems | link do GitHub e link do vídeo | até 7 min |
| Physical Computing | link do Wokwi, link do vídeo e `integrantes.txt` | até 4 min |
| Testing, Compliance e QA | PDF com o link do Azure Boards, os prints e o link do pitch | pitch de até 4 min |

## SOA e Web Services

Entrega: link do GitHub. Total: 10 pontos.

| Critério | Pontos | Onde está |
|---|---|---|
| Estruturação do domínio | 3,0 | `api/src/Corredor.Cadastro/Dominio/`: `Idoso`, `Visita`, `AvisoAoVizinho`, `MensagemSaida` |
| Conexão com banco de dados | 3,0 | Postgres, `CadastroDbContext`, schema `cadastro`, papel `corredor_cadastro` |
| Migrações | 1,5 | `api/src/Corredor.Cadastro/Infra/Migrations/`: `Esqueleto`, `Inicial`, `Gatilhos` |
| Documentação automática dos endpoints | 1,5 | Swagger em `/swagger`, OpenAPI em `/openapi/v1.json`, prints em [docs/evidencias/api/](docs/evidencias/api/README.md) |
| Testes automatizados | 1,0 | `api/tests/Corredor.Cadastro.Testes`, com Postgres real pelo Testcontainers |

O enunciado também exige o código completo do backend, um README com instruções, descrição e nomes dos integrantes, e evidências da aplicação em execução. Detalhe em [api/README.md](api/README.md#soa-e-web-services).

## C# Software Development

Entrega: link do GitHub. Total: 10 pontos.

| Critério | Pontos | Onde está |
|---|---|---|
| Programação orientada a objetos, interfaces e injeção de dependência | 3,0 | `api/src/Corredor.Motor/Dominio/Regras.cs`, `api/src/Corredor.Contratos/Portas.cs`, `*Modulo.cs` |
| Web API | 2,5 | Minimal APIs em `api/src/*/Rotas/`, host em `api/src/Corredor.Api/Program.cs` |
| Entity Framework Core e LINQ | 1,5 | `MotorDbContext`, consultas do radar em `Aplicacao/Consultas.cs` |
| Programação assíncrona (`async` e `await`) | 1,0 | consumidor MQTT em `Infra/Mqtt.cs`, `ServicosPeriodicos.cs`, casos de uso |
| Qualidade do código | 2,0 | `Directory.Build.props`, `.editorconfig`, testes xUnit por módulo, CI em `.github/workflows/ci.yml` |

O enunciado também exige testes unitários, documentação dos endpoints, README e evidências. Detalhe em [api/README.md](api/README.md#c-software-development).

## Cybersecurity

Entrega: **um arquivo PDF** com a documentação e, **separado**, um ZIP ou o link do GitHub com o código-fonte. O enunciado pede os dois em arquivos separados. Total: 10 pontos.

| Critério | Pontos | Onde está |
|---|---|---|
| Mapeamento de PII (motor de parsing) | 3,0 | `api/src/Corredor.Guarda/Rotas/InterceptadorDeEntrada.cs`, `Dominio/MotorDeParsing.cs`, regras em `Regras/pii.json` |
| Taint analysis | 3,0 | `Aplicacao/SaidaSegura.cs`, `Infra/ProvedorDeLogSeguro.cs`, `Rotas/FiltroDeResposta.cs`, `Dominio/PoliticaDeCanal.cs` |
| IA na remediação (tradução do risco para o cidadão) | 2,0 | `Aplicacao/Tradutor.cs`, LLM pelo OpenRouter |
| Dashboard Risco Cidadão | 2,0 | `api/src/Corredor.Guarda/painel/index.html`, em `/api/guarda/demo/painel` no notebook e em `/painel/` na VM |

O que o documento "Explicação: Global Solutions" pede em cada ponto:

1. **Mapeamento de PII.** Um middleware ou função interceptadora no backend olha todo dado que entra (formulário do app ou JSON da API) e acha informação que identifica o cidadão de forma sensível: CPF, RG, laudo médico, senha. Usa regex ou regras de validação. Um JSON com `documento: "123.456.789-00"` tem o campo marcado como PII e mascarado antes de chegar ao log ou ao banco, como `***.456.***-00`.
2. **Taint analysis.** Rastreia o dado da entrada (source) até a saída ou o armazenamento (sink), para provar que a PII do ponto 1 não vaza por acidente. Uma camada de validação antes das saídas bloqueia a impressão de variável marcada como sensível em log (por exemplo `logger.info(dados_de_saude)`) e em resposta HTTP sem autenticação.
3. **Tradução via IA (Risco Cidadão).** O backend captura o evento técnico de segurança e manda um prompt dinâmico para um LLM, por API, pedindo a tradução para linguagem simples, como para um cidadão leigo de 60 anos, em 2 frases.
4. **Dashboard.** O retorno da IA alimenta um painel no frontend com um farol de risco verde, amarelo e vermelho, junto da mensagem traduzida.

O PDF tem três partes, com prints claros do código e das evidências de Parsing PII, Taint Analysis, IA e Dashboard:

| Parte do PDF | O que mostra | Onde está o material |
|---|---|---|
| 1. Source | o código de cada ponto | `api/src/Corredor.Guarda` |
| 2. Auditoria | as evidências de que cada ponto funciona | [docs/evidencias/cyber/](docs/evidencias/cyber/LEIA-ME.md) |
| 3. Vídeo do Dashboard | o painel mudando de verde para amarelo e vermelho | [README da raiz](README.md#vídeos) |

O texto-base do PDF está em [docs/entrega/cyber-documentacao.md](docs/entrega/cyber-documentacao.md).

## IA e Machine Learning

Entrega: link do repositório e link do vídeo. Dados simulados não são aceitos. Total: 10 pontos.

| Critério | Pontos | Onde está |
|---|---|---|
| Problema e dados reais | 2,0 | [ml/dados/README.md](ml/dados/README.md): INMET e SIH/DATASUS, com link, período e licença |
| Escolha justificada do modelo | 1,0 | notebook e [ml/RELATORIO_ML.md](ml/RELATORIO_ML.md) |
| Preparação e divisão dos dados | 2,0 | notebook `ml/corredor_aceso.ipynb` |
| Treino com parâmetros diferentes | 2,0 | 24 combinações de 3 hiperparâmetros, validação cruzada só no treino |
| Resultados em treino e teste | 2,0 | notebook e relatório |
| Conclusão para o cidadão | 1,0 | relatório |

O enunciado também exige o notebook executado, o `requirements.txt`, um relatório de 2 a 4 páginas e um vídeo de até 5 minutos no YouTube, não listado. Detalhe em [ml/README.md](ml/README.md).

## Mobile Development

Entrega: link do GitHub e vídeo de até 3 minutos com o app em execução. O enunciado define seis critérios.

| Critério | Onde está |
|---|---|
| Telas e navegação | sete telas com Expo Router em `app/src/app/` |
| Consumo de dados (API ou mock) | `app/src/api/cliente.ts` e `app/src/api/mock/` |
| Banco de dados local | expo-sqlite em `app/src/banco/` |
| Acessibilidade | rótulos, contraste de 4,5:1 medido por script, texto junto da cor |
| Aderência ao problema | o radar mostra só quem precisa de visita, funciona sem internet e nunca mostra verde sem prova |
| Documentação | [app/README.md](app/README.md), com a Ficha, e prints em [docs/evidencias/mobile/](docs/evidencias/mobile/) |

O app é Expo com React Native, TypeScript e Expo Router.

## Operating Systems

Entrega: link do GitHub, vídeo de até 7 minutos e capturas de tela organizadas no repositório. O enunciado aceita WSL com todas as configurações ou VM local/Azure. O projeto validou uma VM local Ubuntu 24.04 no Lima/VZ; Multipass é uma alternativa documentada e Azure não é necessário. Total: 10 pontos.

| Critério | Pontos | Onde está |
|---|---|---|
| Script de infraestrutura: rede, DNS, IP | 2 | `infra/01-rede.sh` |
| Script da aplicação | 2 | `infra/02-app.sh`, `provisionar.sh`, `deploy.sh` |
| Script de monitoramento com alertas de CPU, disco e memória | 2 | `infra/monitor.sh` |
| Monitoramento como serviço tolerante a falhas | 1 | `infra/03-monitor.sh`, `infra/systemd/corredor-monitor.service` |
| Logs de auditoria e de execução | 1 | `infra/lib/auditoria.sh` |
| Funcionamento completo | 2 | `infra/teste-de-falha.sh`, `infra/verificar.sh` |

Evidências em [docs/evidencias/os/](docs/evidencias/os/README.md). Detalhe em [infra/README.md](infra/README.md).

Além da rubrica acima, a demonstração deve cobrir hostname, rede e resolução de nomes; dependências, usuários, diretórios, variáveis e permissões; aplicação Web funcionando; monitoramento periódico com limites e alertas; controle dos serviços, início automático e recuperação; logs com data, operação e resultado; e testes de falha. O vídeo deve mostrar o provisionamento, a aplicação, o monitor, os serviços, os logs e a recuperação.

O [índice das evidências](docs/evidencias/os/README.md#requisitos-e-comprovação) relaciona esses itens aos arquivos e explica por que o Pages sozinho não conclui a entrega. A [validação em VM Ubuntu 24.04 no Lima/VZ](docs/evidencias/os/vm/README.md) comprovou provisionamento, API, limites padrão, falhas, auditoria e início após reinicialização. A captura da interface atual é do painel de Cyber, não do app da ACS consumindo essa API: essa prova visual e o vídeo ainda faltam. Os nove logs anteriores em contêiner foram preservados. A publicação de todas as provas no [Pages](https://iamp5.github.io/corredor-aceso/#os) também deve ser conferida. Multipass é alternativa documentada, não o hipervisor executado.

## Physical Computing: IoT e IoB

Entrega: link do projeto no Wokwi, link do vídeo de até 4 minutos e um arquivo `.txt` com os nomes dos integrantes.

| Requisito | Onde está |
|---|---|
| Circuito ESP32 com sensores | `iot/no/diagram.json` e `iot/no/no.ino`: presença, DHT22, potenciômetro como sensor de tensão, botão, dois LEDs e buzzer |
| Integrado à solução | o nó segue o contrato MQTT que o Motor da API consome, e o simulador das 39 casas usa o mesmo contrato |
| Link do projeto no Wokwi | [iot/README.md](iot/README.md#abrir-e-rodar-no-wokwicom) |
| Vídeo com a integração | [README da raiz](README.md#vídeos) |
| Arquivo com os nomes | [integrantes.txt](integrantes.txt) |

Evidências em [docs/evidencias/iot/](docs/evidencias/iot/README.md).

## Testing, Compliance e Quality Assurance

Entrega: PDF com o link do projeto no Azure Boards e os prints. O professor recebe acesso de Admin ao projeto. O link do vídeo de pitch, de até 4 minutos, também vai nesta entrega.

| Critério | Peso | Onde está |
|---|---|---|
| Features com descrição e critérios de aceite | 50% | [docs/qa/features.md](docs/qa/features.md), importação em `docs/qa/azure/features.csv` |
| Test Cases funcionais, positivos e negativos, com objetivo, preparação, entradas, saídas esperadas e passos | 50% | [docs/qa/casos-de-teste.md](docs/qa/casos-de-teste.md), importação em `docs/qa/azure/casos-de-teste.csv` |

A rastreabilidade entre regra, Feature, Test Case e teste automatizado está em [docs/qa/rastreabilidade.md](docs/qa/rastreabilidade.md). Detalhe em [docs/qa/README.md](docs/qa/README.md).

## Integrantes

| Nome | RM |
|---|---|
| Bruno Dominicheli | 554981 |
| Miguel Kapicius | 556198 |
| Thiago Ferreira | 555608 |
