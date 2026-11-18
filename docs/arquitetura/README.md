# Arquitetura do Corredor Aceso

Esta pasta guarda todos os documentos e todos os desenhos da arquitetura do Corredor Aceso, a solução da equipe para a Global Solution FIAP 2026.2. Os cinco documentos, lidos na ordem abaixo, descrevem o produto, o domínio, os dados e a entrega.

## Documentos, em ordem de leitura

| # | Documento | O que responde |
|---|---|---|
| 1 | [01-visao-geral.md](01-visao-geral.md) | O que o produto faz, quais são as partes, quem é dono de cada dado, as decisões e os motivos, as três cenas da demo e os critérios de avaliação por disciplina. |
| 2 | [02-event-storming.md](02-event-storming.md) | Quais fatos acontecem no domínio, quem os causa, quais políticas reagem e quais dúvidas foram fechadas. Tem os oito fluxos, passo a passo. |
| 3 | [03-modelo-de-dominio.md](03-modelo-de-dominio.md) | Contextos, projetos .NET, glossário, agregados, regras da situação da casa, contratos MQTT e HTTP, casos de teste e escopo da entrega. |
| 4 | [04-modelo-de-dados.md](04-modelo-de-dados.md) | Tabelas do Postgres, mapeamento do EF Core, consultas, retenção, classificação dos dados, SQLite do app e carga da demo. |
| 5 | [05-infra-e-entrega.md](05-infra-e-entrega.md) | VM, provisionamento por Bash, CI, releases, deploy com rollback, operação, medições e limitações. |

O glossário da seção 3 do [modelo de domínio](03-modelo-de-dominio.md#3-linguagem-ubíqua) vale para todos os documentos, para os desenhos e para o código.

## Desenhos

A arquitetura tem 27 desenhos. Cada desenho é um arquivo `.mmd` em `diagramas/`, com o SVG claro e o SVG escuro ao lado. A tabela liga o SVG de cada desenho, e a coluna "Documento" indica onde ele aparece.

| # | Desenho | Tipo | Documento |
|---|---|---|---|
| 01 | [Visão geral do sistema](diagramas/01-visao-geral.svg) | `flowchart LR` | 01, "O sistema em um desenho" |
| 02 | [Projetos .NET e referências permitidas](diagramas/02-modulos.svg) | `classDiagram` | 03, seção 2 |
| 03 | [Cena 1: o movimento tira o card da tela](diagramas/03-cena-movimento.svg) | `sequenceDiagram` | 01, "As três cenas da demo" |
| 04 | [Cena 2: o aparelho mudo fica cinza](diagramas/04-cena-silencio.svg) | `sequenceDiagram` | 01, "As três cenas da demo" |
| 05 | [Cena 3: a mensagem é bloqueada](diagramas/05-cena-bloqueio.svg) | `sequenceDiagram` | 01, "As três cenas da demo" |
| 06 | [Regras da situação da casa](diagramas/06-situacao-da-casa.svg) | `flowchart LR` | 03, seção 5.4 |
| 07 | [Mapa de contexto](diagramas/07-mapa-de-contextos.svg) | `flowchart LR` | 03, seção 1.2 |
| 08 | [Event storming, fluxo 1: consentimento e instalação](diagramas/08-event-storming-fluxo-1-consentimento.svg) | `eventmodeling` | 02, sessão 2 |
| 09 | [Event storming, fluxo 2: noite comum](diagramas/09-event-storming-fluxo-2-noite-comum.svg) | `eventmodeling` | 02, sessão 2 |
| 10 | [Event storming, fluxo 3: dia crítico, manhã](diagramas/10-event-storming-fluxo-3-dia-critico-manha.svg) | `eventmodeling` | 02, sessão 2 |
| 11 | [Event storming, fluxo 3: dia crítico, tarde](diagramas/11-event-storming-fluxo-3-dia-critico-tarde.svg) | `eventmodeling` | 02, sessão 2 |
| 12 | [Event storming, fluxo 4: apagão com eletrodependente](diagramas/12-event-storming-fluxo-4-apagao.svg) | `eventmodeling` | 02, sessão 2 |
| 13 | [Event storming, fluxo 5: aparelho mudo](diagramas/13-event-storming-fluxo-5-aparelho-mudo.svg) | `eventmodeling` | 02, sessão 2 |
| 14 | [Event storming, fluxo 6: mensagem bloqueada](diagramas/14-event-storming-fluxo-6-mensagem-bloqueada.svg) | `eventmodeling` | 02, sessão 2 |
| 15 | [Event storming, fluxo 7: desfecho da visita](diagramas/15-event-storming-fluxo-7-desfecho-da-visita.svg) | `eventmodeling` | 02, sessão 2 |
| 16 | [Event storming, fluxo 8: pedido de contato](diagramas/16-event-storming-fluxo-8-pedido-de-contato.svg) | `eventmodeling` | 02, sessão 2 |
| 17 | [Agregados do Motor](diagramas/17-agregados-motor.svg) | `classDiagram` | 03, seção 4.2 |
| 18 | [Agregados do Cadastro](diagramas/18-agregados-cadastro.svg) | `classDiagram` | 03, seção 4.3 |
| 19 | [Agregado e serviços da Guarda](diagramas/19-agregados-guarda.svg) | `classDiagram` | 03, seção 4.4 |
| 20 | [Modelo de dados: os três schemas](diagramas/20-modelo-de-dados.svg) | `erDiagram` | 04, seção 9 |
| 21 | [VM e seus serviços](diagramas/21-vm-e-servicos.svg) | `flowchart LR` | 05, "Visão geral" |
| 22 | [Ordem de provisionamento](diagramas/22-provisionamento.svg) | `flowchart LR` | 05, seção 6.1 |
| 23 | [Pipeline de CI](diagramas/23-ci.svg) | `flowchart LR` | 05, seção 1.12 |
| 24 | [Release e rollback](diagramas/24-release-e-rollback.svg) | `swimlane-beta LR` | 05, seção 1.6 |
| 25 | [Árvore do repositório](diagramas/25-repositorio.svg) | `treeView-beta` | 05, seção 2 |
| 27 | [Telas do app da ACS](diagramas/27-telas-do-app.svg) | `flowchart LR` | 01, "App da ACS" |
| 28 | [Fluxo da Guarda numa saída bloqueada](diagramas/28-fluxo-da-guarda.svg) | `sequenceDiagram` | 03, seção 4.4 |

O SVG escuro de cada desenho tem o mesmo nome, com `.escuro.svg` no fim. O fluxo 3 do event storming tem dois desenhos, porque o fluxo completo passa de 30 quadros.
