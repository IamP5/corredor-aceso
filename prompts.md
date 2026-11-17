# Uso de IA no projeto

O combinado da Global Solution permite IA como copiloto, com os prompts registrados e com a equipe capaz de explicar cada linha. Este arquivo registra onde a IA foi usada e com qual pedido.

A ferramenta foi o Claude Code. O que a IA produziu foi conferido por execução: os scripts de infraestrutura rodaram em contêiner, o modelo de dados rodou em Postgres 16 com EF Core, e as regras da situação da casa passaram por um modelo executável com casos aleatórios.

## Descoberta

- Pesquisa do problema, personas e ranking das ideias. Os prompts estão em `docs/prompt-discovery-poteto.md` e `docs/prompt-ideias-definidas-poteto.md`.
- As entrevistas, os testes de usabilidade e a banca desta fase foram simulados com personas. Cada documento de `docs/discovery/` diz isso no início.

## Arquitetura

| Pedido | Resultado |
|---|---|
| Idealizar a solução técnica e o diagrama de arquitetura a partir dos entregáveis de cada disciplina | `docs/arquitetura.md` |
| Conferir a aderência aos requisitos de Cybersecurity depois da explicação do professor | seção do módulo Guarda |
| Definir o modelo de domínio com Domain-Driven Design e event storming, com três propostas independentes e um juiz | `docs/arquitetura/02-event-storming.md`, `docs/arquitetura/03-modelo-de-dominio.md` |
| Definir a modelagem de dados, com três propostas independentes executadas em Postgres | `docs/arquitetura/04-modelo-de-dados.md` |
| Pesquisar e definir infraestrutura como código, CI/CD e ferramenta de monorepo | `docs/arquitetura/05-infra-e-entrega.md` |
| Estudar os exemplos e o tema do Pele e escolher o tipo de diagrama de cada visão | `docs/arquitetura/diagramas/GUIA.md` |

## Construção

Cada parte do código registra aqui o pedido que a gerou, na ordem em que for construída.
