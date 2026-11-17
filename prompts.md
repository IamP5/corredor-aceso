# Uso de IA no projeto

O combinado da Global Solution permite IA como copiloto, com os prompts registrados e com a equipe capaz de explicar cada linha. Este arquivo registra onde a IA foi usada e com qual pedido.

A ferramenta foi o Claude Code. O que a IA produziu foi conferido por execução: os scripts de infraestrutura rodaram em contêiner, o modelo de dados rodou em Postgres 16 com EF Core, e as regras da situação da casa passaram por um modelo executável com casos aleatórios.

## Descoberta

- Pesquisa do problema, personas e ranking das ideias. Os prompts estão em `docs/prompt-discovery-poteto.md` e `docs/prompt-ideias-definidas-poteto.md`.
- As entrevistas, os testes de usabilidade e a banca desta fase foram simulados com personas. Cada documento de `docs/discovery/` diz isso no início.

## Arquitetura

| Pedido | Resultado |
|---|---|
| Idealizar a solução técnica e o diagrama de arquitetura a partir dos entregáveis de cada disciplina | `docs/arquitetura.md`, hoje substituído por `docs/arquitetura/01-visao-geral.md` |
| Conferir a aderência aos requisitos de Cybersecurity depois da explicação do professor | seção do módulo Guarda |
| Definir o modelo de domínio com Domain-Driven Design e event storming, com três propostas independentes e um juiz | `docs/arquitetura/02-event-storming.md`, `docs/arquitetura/03-modelo-de-dominio.md` |
| Definir a modelagem de dados, com três propostas independentes executadas em Postgres | `docs/arquitetura/04-modelo-de-dados.md` |
| Pesquisar e definir infraestrutura como código, CI/CD e ferramenta de monorepo | `docs/arquitetura/05-infra-e-entrega.md` |
| Estudar os exemplos e o tema do Pele e escolher o tipo de diagrama de cada visão | `docs/arquitetura/diagramas/GUIA.md` |

## Construção

Cada parte do código registra aqui o pedido que a gerou, na ordem em que foi construída. Cada parte foi feita por uma sessão do Claude Code isolada num worktree, a partir dos documentos de `docs/arquitetura/`, e juntada na `main` depois.

| Parte | Pedido | Resultado |
|---|---|---|
| Esqueleto da API | Criar a solução .NET 10 com os cinco projetos, os contratos de `Corredor.Contratos`, autenticação por token da ACS, saúde, versão e Swagger, sem regra de módulo | `api/`, commit `805e129` |
| Módulo Motor | Implementar o Motor pela seção 4.2 e pelas regras da seção 5 de `03-modelo-de-dominio.md`: avaliador puro, consumidor MQTT, dia crítico, radar, linha do tempo e relógio de demo, com os casos de teste 1 a 20 | `api/src/Corredor.Motor` e `api/tests/Corredor.Motor.Testes` |
| Módulo Guarda | Implementar a Guarda pela seção 4.4: regras de PII em JSON, interceptador de entrada, `SaidaSegura` com a política de canal, log seguro, tradução por LLM com frase fixa de reserva e painel Risco Cidadão | `api/src/Corredor.Guarda`, `docs/evidencias/cyber/` |
| Módulo Cadastro | Implementar o Cadastro pela seção 4.3: idoso, consentimento com token, vizinho, visita, verificação de mensagem, avisos ao vizinho, migrações do EF Core e a carga da demo | `api/src/Corredor.Cadastro`, `api/semente/` |
| App da ACS | Construir o app Expo com as telas do radar, da casa, da visita, da mensagem e do encerramento, o SQLite da seção 6 de `04-modelo-de-dados.md`, a fila de desfechos sem rede e acessibilidade | `app/`, `docs/evidencias/mobile/` |
| Nó e simulador | Escrever o firmware do ESP32 para o Wokwi e o simulador das 39 casas com o contrato MQTT da seção 6.2 e os cenários da demo | `iot/`, `docs/evidencias/iot/` |
| Modelo de dia crítico | Montar o notebook com dados reais do INMET e do SIH, comparar o XGBoost com a regra da noite quente e escrever o job das 05h | `ml/` |
| Qualidade | Escrever as Features e os Test Cases para o Azure Boards a partir dos casos da seção 7, a rastreabilidade e o kit de entrega | `docs/qa/`, `docs/entrega/` |
| Integração | Juntar as sete partes, rodar as três cenas da demo no sistema de verdade, corrigir os defeitos das junções com um teste para cada um, conferir a invariante do falso verde no sistema integrado e escrever o relatório | `tools/demo.sh`, `tools/demo/`, `docs/evidencias/integracao/` |
