# Arquitetura do Corredor Aceso

Esta pasta guarda todos os documentos e todos os desenhos da arquitetura do Corredor Aceso, a solução da equipe para a Global Solution FIAP 2026.2. Quem lê os cinco documentos na ordem abaixo entende o produto, o domínio, os dados e a entrega.

Para ler com os desenhos em tamanho natural, abra `site/index.html` com dois cliques. O site não precisa de servidor nem de rede. O GitHub não desenha a referência a um arquivo `.mmd`. Lá, use a tabela de desenhos abaixo, que liga o SVG já renderizado.

## Documentos, em ordem de leitura

| # | Documento | O que responde |
|---|---|---|
| 1 | [01-visao-geral.md](01-visao-geral.md) | O que o produto faz, quais são as partes, quem é dono de cada dado, as decisões e os motivos, as três cenas da demo e onde cada disciplina pontua. |
| 2 | [02-event-storming.md](02-event-storming.md) | Quais fatos acontecem no domínio, quem os causa, quais políticas reagem e quais dúvidas foram fechadas. Tem os oito fluxos, passo a passo. |
| 3 | [03-modelo-de-dominio.md](03-modelo-de-dominio.md) | Contextos, projetos .NET, glossário, agregados, regras da situação da casa, contratos MQTT e HTTP, casos de teste e ordem de corte. |
| 4 | [04-modelo-de-dados.md](04-modelo-de-dados.md) | Tabelas do Postgres, mapeamento do EF Core, consultas, retenção, classificação dos dados, SQLite do app e carga da demo. |
| 5 | [05-infra-e-entrega.md](05-infra-e-entrega.md) | VM, provisionamento por Bash, CI, releases, deploy com rollback, operação, roteiro do vídeo de Operating Systems e plano até 19/11. |
| 6 | [diagramas/GUIA.md](diagramas/GUIA.md) | Qual tipo de diagrama cada vista usa, como escrever o `.mmd`, o tema e como renderizar. |

O glossário da seção 3 do [modelo de domínio](03-modelo-de-dominio.md#3-linguagem-ubíqua) vale para todos os documentos, para os desenhos e para o código.

## Desenhos

Cada desenho é um arquivo `.mmd` em `diagramas/`, com o SVG claro e o SVG escuro ao lado. A coluna "Documento" diz onde ele aparece.

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
| 26 | [Plano até 19/11](diagramas/26-plano.svg) | `gantt` | 05, seção 8.1 |
| 27 | [Telas do app da ACS](diagramas/27-telas-do-app.svg) | `flowchart LR` | 01, "App da ACS" |
| 28 | [Fluxo da Guarda numa saída bloqueada](diagramas/28-fluxo-da-guarda.svg) | `sequenceDiagram` | 03, seção 4.4 |

O SVG escuro de cada desenho tem o mesmo nome, com `.escuro.svg` no fim. O fluxo 3 do event storming tem dois desenhos, porque inteiro ele passa de 30 quadros.

## Outras pastas

| Pasta | O que tem |
|---|---|
| `diagramas/` | Os 28 arquivos `.mmd`, os SVG, o `render.mjs`, o `site.mjs`, o tema em `tema/`, um modelo por tipo de diagrama em `modelos/` e a galeria de exemplos em `galeria/`. |
| `referencia/repo/` | Os arquivos de infra, de CI e de configuração, nos caminhos que têm no repositório. O documento 05 aponta para eles. |
| `referencia/dados/` | O SQL, o projeto do EF Core e os scripts que conferem o documento 04. O `montar.py` de lá gera o documento 04, então edite `modelo-de-dados.fonte.md`, não o documento. |
| `site/` | A versão em HTML dos documentos. É só saída. Apague e gere de novo. |

## Como gerar os desenhos e o site

```bash
cd docs/arquitetura/diagramas
npm install
npm run render    # cada .mmd vira nome.svg e nome.escuro.svg
npm run site      # apaga e gera docs/arquitetura/site/
```

Os dois comandos saem com código 1 se algum desenho não renderizar. Depois de mudar um `.mmd`, rode os dois e abra o desenho no tema claro e no escuro.
