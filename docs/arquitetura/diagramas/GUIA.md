# Guia de diagramas do Corredor Aceso

Este guia diz qual tipo de diagrama Pele usar em cada vista do projeto, como escrever o `.mmd`, qual tema aplicar, como renderizar e como gerar o site da arquitetura. Ele vale para todo diagrama em `docs/arquitetura/diagramas/`.

Os documentos da arquitetura agora são lidos como site HTML, em `docs/arquitetura/site/`. Por isso não há mais limite de largura: cada diagrama sai na largura natural, e o site dá zoom ao que passa da janela. O Markdown continua sendo a fonte que a equipe edita.

Tudo o que este guia diz sobre aparência veio de uma imagem aberta e olhada: SVG renderizado com o `pele` 1.13.0 desta pasta e fotografado em tamanho natural no Chrome headless.

## O que foi visto

- **Os 601 exemplos de pele.run/examples**, nos 32 tipos, na primeira rodada (folhas de contato e alguns em tamanho real).
- **63 candidatos do projeto na largura natural, sem teto**, para as 16 vistas, em 2 a 4 tipos cada, com conteúdo de `docs/arquitetura.md`, `02-event-storming.md` e `03-modelo-de-dominio.md`. Entraram os tipos que o limite de 820 px tinha tirado: `eventmodeling`, `C4Context`, `C4Container`, `C4Component`, `C4Deployment`, `architecture-beta`, `swimlane-beta`, `erDiagram` com os três schemas, `stateDiagram-v2`, `gantt` e `journey`. Os que entraram na galeria estão em `galeria/rascunhos/`.
- **Os 8 modelos de `modelos/`**, cada um no tema claro ou no escuro.
- **O site gerado**, nas páginas com o diagrama de 2983 px, no claro e no escuro, com zoom.

## Tipo escolhido por vista

A galeria `galeria/rascunhos.html` mostra cada candidato ao lado do código.

| Vista | Tipo escolhido | Por quê | Segunda opção | Defeitos vistos |
|---|---|---|---|---|
| Visão geral do sistema | `flowchart LR` | O diagrama de `arquitetura.md`, como está, sai com 2983×607 e legível: grupos aninhados (casa, VM, processo .NET), cada aresta com o protocolo. | `C4Container` | C4: arestas passam por cima das caixas (Simulador → Mosquitto cruza o App), rótulos de relação se sobrepõem, estereótipos em inglês («container», «external system», [SYSTEM]). `architecture-beta`: aresta atravessa nó (Nó ESP32 → broker passa pelo Simulador) e não tem rótulo. |
| Módulos e referências permitidas | `classDiagram` | Separa "depende de" (`..>`) de "implementa" (`..\|>`) e mostra os métodos de `IMotorDeCasas`. | `C4Component` | O `C4Component` ficou limpo na largura natural, mas não tem seta de realização e os estereótipos saem em inglês («component», [CONTAINER]). |
| Fluxos MQTT e HTTP das 3 cenas | `sequenceDiagram`, um por cena | Diz quem chama quem, em que ordem, com a rota HTTP e o tópico MQTT escritos na mensagem. É o que quem programa precisa. | `eventmodeling` | O `eventmodeling` mostra comando, evento e leitura por contexto, mas não diz protocolo nem rota. O `swimlane-beta LR` ficou legível (1646 px) e diz menos que a sequência. |
| Regras da situação da casa | `flowchart LR` em escada | A ordem das regras é a semântica: a primeira que devolve alerta vence. A escada desce da esquerda para a direita (1969×458) e termina em `Verde` ou `Aguardando`. | `stateDiagram-v2` | Na largura natural o state ficou legível, com rótulo junto da linha. Ele perde porque sugere uma situação gravada que muda por transição, e a situação é calculada a cada leitura (seção 5 do modelo de domínio). |
| Mapa de contextos | `flowchart LR` | O bloco de `03-modelo-de-dominio.md` sai com 1560×281, e o rótulo de cada aresta diz a relação (conformista, camada anticorrupção). | `block` | O `block` não tem rótulo de aresta. |
| Event storming | `eventmodeling`, um por fluxo | Uma raia por contexto (`Eventos: Motor`, `Eventos: Cadastro`), o tempo da esquerda para a direita, comando e leitura na raia do meio, dados do comando em mono. O fluxo 3 inteiro sai com 2709×346. | `flowchart LR` com `:::evento`, `:::comando`, `:::politica`, `:::leitura` | Nomes sem espaço (são os nomes do código). O Pele liga cada quadro ao anterior; sem `rf` no começo de cada fatia aparecem setas falsas (Radar → Relógio). Raias em inglês: o render traduz (ver abaixo). Política e automação usam a mesma cor. |
| Agregados | `classDiagram` com `namespace` por módulo | Raiz, entidade, composição `*--` e referência entre módulos só por `CasaToken`, com tipos e métodos. | `block` | Duas arestas tracejadas chegam perto uma da outra em `CasaMonitorada`. O `block` só lista nomes. |
| Modelo de dados | `erDiagram`, os três schemas num diagrama | Um `subgraph` por schema. Fica à vista que nenhuma relação cruza schema. Tipos, PK, FK, UK e cardinalidade claros (1048×1746). | um `erDiagram` por schema | Com `direction LR`, o Pele empilha os schemas na vertical e só deita as tabelas dentro de cada um. |
| VM e seus serviços | `flowchart LR` | Unidades systemd, portas, log e o que vem de fora, com a rota em cada aresta (1846×427). | `C4Deployment` | `architecture-beta`: a aresta `api → LLM` atravessa o timer e o nó do log, e nenhuma aresta tem rótulo. `C4Deployment`: as arestas correm juntas no topo e se cruzam, estereótipos em inglês. |
| Ordem de provisionamento | `flowchart LR` | Os quatro scripts em cadeia e `lib/auditoria.sh` tracejado para todos (1171×211). | `timeline TD` | No timeline cada seção ganha uma cor de série, e a 4ª é vermelha: parece alerta sem ser. |
| Pipeline de CI | `flowchart LR` | Gatilho, os 5 jobs em paralelo num grupo e o portão; cada aresta separada (1043×566). | `swimlane-beta LR` | O swimlane junta as 5 arestas que convergem num tronco só, e não dá para seguir uma. |
| Release e rollback | `swimlane-beta LR` | A raia diz onde cada passo roda (GitHub ou VM), e `:::verde` e `:::alerta` funcionam (1464×394). | `flowchart LR` | O nome da raia sai na vertical. `gitGraph` serve só para a estratégia de branches. |
| Árvore do repositório | `treeView-beta` | Nomes em negrito e descrição em itálico depois de `##` (464 px). | `mindmap` | O mindmap perde a profundidade e as descrições. |
| Plano dos 3 dias | `gantt` com `todayMarker off` | Mostra o que corre em paralelo e o caminho crítico (`crit`, vermelho) por frente de trabalho. | `timeline TD` | Largura fixa de 800 px. Sem `todayMarker off`, o SVG muda a cada render. O Pele pinta toda tarefa com o acento âmbar; `tema/classes.css` repinta a tarefa comum na série 1. |
| Telas do app da ACS | `flowchart LR` | As telas como nós e a navegação de volta (detalhe → radar), que vira rota do Expo Router. | `journey` | O `journey` com 7 passos ficou inteiro na largura natural (1120 px). Ele perde porque as notas de humor seriam inventadas: a entrevista com uma ACS real ainda não aconteceu. Ele não mostra a volta entre telas. |
| Fluxo da Guarda | `sequenceDiagram` | Os participantes são as classes (`InterceptadorDeEntrada`, `SaidaSegura`, `Tradutor`), e a sequência mostra a resposta 200 antes da tradução assíncrona. | `flowchart TB` | `agentflow-beta`: o conector do LLM aparece solto, sem aresta até o Tradutor. |

### O que mudou em relação à escolha de 820 px

- **Visão geral, mapa de contextos, VM, provisionamento:** continuam flowchart, agora `LR` e na largura natural. O provisionamento saiu do `timeline`.
- **Estado da casa:** a escada vira `LR`.
- **Event storming:** sai o flowchart, entra `eventmodeling`.
- **Modelo de dados:** sai "um diagrama por schema", entra um diagrama com os três schemas.
- **Release:** sai o flowchart TB, entra `swimlane-beta LR`.
- **Plano:** sai o `timeline`, entra `gantt`.
- **Fluxo da Guarda:** sai o flowchart, entra `sequenceDiagram`.
- **Ficam:** módulos e agregados (`classDiagram`), cenas (`sequenceDiagram`), CI e telas do app (`flowchart`), árvore (`treeView-beta`).

### Defeitos que ainda tiram um tipo do projeto

- **`architecture-beta`:** a aresta atravessa nós e não tem rótulo. Visto na visão geral e na VM.
- **C4 (`C4Context`, `C4Container`, `C4Deployment`):** arestas por cima das caixas e rótulos de relação sobrepostos, mesmo sem limite de largura. Estereótipos e tipos de fronteira em inglês. O `C4Component` com 5 componentes ficou limpo; use-o só se a vista não precisar de seta de realização.
- **`journey`:** só com notas de humor que vieram de uma pessoa real.
- **`stateDiagram-v2`** para a situação da casa: não é defeito de desenho, é de semântica.
- **`agentflow-beta`:** conector solto.
- **`swimlane-beta`** com arestas que convergem: o tronco único esconde de onde vem cada uma.

### Contornos que o render já aplica

- **Raias do `eventmodeling` em português.** O Pele escreve "UI/Automation", "Command/Read Model", "Events" e os prefixos "UI/A:", "C/RM:", "Stream:". O `tema/pele-tema.mjs` troca por "Tela e automação", "Comando e leitura", "Eventos", "Tela:", "Leitura:" e "Eventos:", e recentra o texto com a tabela de larguras da Atkinson. A galeria desenha no navegador sem essa troca, então lá as raias aparecem em inglês.
- **Tarefa comum do `gantt` na série 1**, em `tema/classes.css`, para o âmbar ficar com o que importa.

## Regras de escrita

1. **Direção.** Comece com `LR`. Use `TB` quando a cadeia principal for curta e o resto pendurar dos lados. A página do site cresce para os lados e dá zoom; rolar para baixo num diagrama alto cansa mais.
2. **Largura.** Não há teto. Um diagrama largo demais para ler mesmo com zoom (o fan-in de 30 dos exemplos do Pele, com 3857 px) deve ser dividido pelo assunto, não pela largura.
3. **Tamanho.**
   - Flowchart: até 25 nós e até 6 nós no mesmo nível.
   - Sequence: até 8 participantes, com o nome do código.
   - Class: até 10 classes, até 6 membros por classe.
   - ER: os três schemas num diagrama, um `subgraph` por schema.
   - Eventmodeling: um fluxo da sessão 2 por diagrama, até 30 quadros.
   - Swimlane: até 4 raias.
   - Gantt: uma seção por frente de trabalho.
4. **Rótulo.**
   - Nó: até 3 linhas, quebradas com `<br/>`.
   - Losango: uma pergunta curta, com no máximo um `<br/>`.
   - Mensagem de sequence: a rota ou o tópico inteiro, como no código. Sem `maxWidth` o Pele não quebra mais no meio da palavra.
   - Rótulo de aresta: até 4 palavras.
5. **Nomes.**
   - Arquivo: `NN-vista.mmd`, com número de dois dígitos e nome em minúsculas com hífen. Exemplo: `03-cena-movimento.mmd`.
   - Id de nó: curto e em minúsculas (`motor`, `cad`, `pg`).
   - Texto: em português, com os nomes do código como estão no código (`IMotorDeCasas`, `corredor-api.service`).
6. **Eventmodeling.**
   - Nome do quadro é o nome no código, sem espaço: `MovimentoDetectado`.
   - Evento leva o prefixo do contexto (`Motor.`, `Cadastro.`, `Guarda.`), que vira a raia `Eventos: Motor`. Comando, leitura, tela e automação vão sem prefixo.
   - Comece cada fatia com `rf` em vez de `tf`, para o Pele não ligar o último quadro da fatia anterior ao primeiro desta.
   - Política do event storming (P8, P11) entra como `pcr`, com o nome da classe que a executa.
   - Leitura liga os eventos que ela lê com `->> 03 ->> 09`.
7. **Gantt.** Sempre `todayMarker off`. `crit` só no caminho crítico.
8. **Cor.** Não use `style` nem `classDef` com cor fixa, porque cor fixa não muda com o tema. Use as classes do projeto, cada uma com uma linha `classDef nome stroke-width:2px`, porque o Pele exige a linha:
   - `:::luz`: a única coisa que importa no diagrama, no máximo uma por diagrama. É a lâmpada do corredor.
   - `:::alerta`, `:::cinza`, `:::verde`: os estados do radar.
   - `:::externo`: fora do sistema (broker público, LLM, dados públicos).
   - `:::evento`, `:::comando`, `:::politica`, `:::agregado`, `:::leitura`: só no event storming em flowchart.
   As classes funcionam em `flowchart`, `swimlane-beta` e `stateDiagram-v2` (visto).
9. **Seta.** Os caracteres → e ← não estão no recorte latino das fontes. Eles aparecem na fonte de reserva do sistema. Prefira texto.

## Tema

Há dois temas, ambos com as cores e as fontes do vídeo do pitch (`pitch/corredor-aceso/video/src/theme.ts`):

- **Claro (`tema/claro.css`):** papel quente, tinta da noite. Para documentos e para o GitHub no modo claro.
- **Escuro (`tema/escuro.css`):** a casa às 3 da manhã. Fundo `#0B1220`, texto papel `#F3ECDF`, a lâmpada `#FFB13B` como único acento e um brilho âmbar fraco no topo do fundo. Para o pitch e para o GitHub no modo escuro.

As fontes são Atkinson Hyperlegible no texto, Atkinson Hyperlegible Mono nas partes mono e Fraunces 600 no título. Os arquivos ficam em `tema/fontes/`, todos sob a licença SIL OFL.

| Variável | Claro | Escuro |
|---|---|---|
| `--pele-bg` | `#fbf7ef` | `#0b1220` |
| `--pele-fg` | `#0b1220` | `#f3ecdf` |
| `--pele-muted` | `#545d6e` | `#a3acbd` |
| `--pele-line` | `#3a4255` | `#b8b0a2` |
| `--pele-surface` | `#f3ecdf` | `#17223a` |
| `--pele-surface-alt` | `#e9dfcc` | `#111a2d` |
| `--pele-border` | `#968767` | `#56688c` |
| `--pele-accent` | `#9a5800` | `#ffb13b` |
| `--pele-critical` | `#b3262d` | `#e5484d` |
| `--pele-radius` | `6px` | `6px` |
| `--pele-series-1` a `-8` | `#4a566e #9a5800 #1d7350 #b3262d #2d5f8b #6a4c93 #2b7176 #7a5c2e` | `#8b95a7 #ffb13b #3dbe8b #e5484d #7fa7d9 #c4a6e6 #5cc8c8 #d9c3a0` |

A série 1 é cinza-azulada de propósito. Ela é a cor padrão de vários tipos, e o âmbar fica guardado para o que importa. As classes do projeto ficam em `tema/classes.css`.

**Contraste medido** com `node tema/contraste.mjs`: os 80 pares passam. Texto tem mínimo de 4,5:1, e linha e borda, de 3:1. Pares que o script mede:

- texto sobre o fundo, sobre o nó e sobre o grupo;
- texto secundário;
- acento;
- texto do fundo sobre cada barra de série sólida;
- texto sobre cada série tingida a 22%;
- as classes do projeto;
- o pico do brilho do tema escuro.

Os menores valores:

| Tema | Par | Contraste |
|---|---|---|
| escuro | `--pele-critical` sobre o fundo | 4,78:1 |
| escuro | fundo sobre a barra da série 4 | 4,78:1 |
| claro | texto secundário sobre o grupo | 5,02:1 |
| claro | acento sobre o fundo | 5,21:1 |
| claro e escuro | texto da noite sobre a lâmpada (`:::luz`) | 10,35:1 |
| claro | borda sobre o fundo (não é texto) | 3,30:1 |
| escuro | borda sobre o fundo (não é texto) | 3,35:1 |
| claro | texto sobre o fundo | 17,52:1 |
| escuro | texto sobre o fundo | 15,94:1 |

## Como renderizar

```bash
cd docs/arquitetura/diagramas
npm install
npm run render            # cada .mmd desta pasta vira nome.svg (claro) e nome.escuro.svg, na largura natural
node render.mjs modelos   # outra pasta
node tema/contraste.mjs   # mede o contraste dos temas
```

- O `render.mjs` usa `renderizar()` de `tema/pele-tema.mjs`. Ele mede o texto com as larguras reais da Atkinson (`tema/metricas.json`, gerado por `tema/metricas.py`), então as caixas saem do tamanho do texto.
- Nenhum `maxWidth` é passado ao Pele: o desenho sai na largura natural e o Pele não troca `LR` por `TB` sozinho.
- Cada SVG sai autossuficiente: traz as fontes usadas em base64, as variáveis do tema num `<style>` interno e um retângulo de fundo pintado. Serve para abrir sozinho, para o pitch e para o README.
- O render sai com código 1 se algum diagrama falhar (testado com um `.mmd` quebrado).

## Como gerar o site

```bash
cd docs/arquitetura/diagramas
npm install
npm run site              # apaga e gera docs/arquitetura/site/
```

Abra `docs/arquitetura/site/index.html` com dois cliques. O site não precisa de servidor nem de rede: fontes, CSS e o script do Pele ficam copiados dentro de `site/`.

- **Páginas.** Uma por documento: `docs/arquitetura.md`, cada `docs/arquitetura/*.md` e este guia. O índice lista todas com o primeiro parágrafo. Links relativos entre os `.md` viram links entre as páginas.
- **Diagrama no Markdown.** Duas formas, e as duas viram SVG dentro da página:
  - um bloco cercado ` ```mermaid `;
  - uma referência a um `.mmd`, sozinha no parágrafo, com o caminho relativo ao `.md`. O texto entre colchetes vira a legenda:

    ```markdown
    ![Visão geral do Corredor Aceso](diagramas/01-visao-geral.mmd)
    ```

  No GitHub, a referência aparece como link para o `.mmd`. Prefira a referência: o mesmo `.mmd` gera o SVG do `npm run render` e o diagrama do site.
- **Largura.** O diagrama sai na largura natural. Mais largo que a coluna de texto, ele sai da coluna até a largura da janela. Mais largo que a janela, encolhe para caber e ganha zoom: pinça ou Ctrl/Cmd + roda, arrastar para mover, duplo clique, teclas `+ - 0` e setas, e os botões no canto. Cada diagrama tem um link para o SVG sozinho, no tema da página.
- **Tema.** Botão claro e escuro, guardado no navegador. Na primeira visita vale o tema do sistema. O SVG da página não traz `<style>`: as variáveis do tema ficam na página, e trocar o tema não redesenha nada.
- **Impressão.** Sem cabeçalho nem sumário, cores claras, diagrama na largura da página e nunca cortado entre páginas.
- **Erro.** Diagrama que não renderiza aparece na página com o erro e o código, e o `npm run site` sai com código 1.
- O `site.mjs` usa uma dependência além do Pele: `marked` 18.2.1, fixa no `package.json`.

## Galeria e modelos

- **`galeria/index.html`** liga uma página por tipo, com os 601 exemplos, e `galeria/rascunhos.html`, com os 50 candidatos do projeto na largura natural. As páginas guardam só o código Mermaid e desenham no navegador com `node_modules/pele`, com um seletor de tema. Precisam de `npm install`.
- **Regenerar a galeria:** `node galeria/gerar.mjs <pasta dos exemplos> [resultados.json]`. Sem a pasta, o comando refaz só a página de rascunhos e o índice.
- **`modelos/`** tem um modelo por tipo escolhido, com conteúdo real do projeto. As primeiras linhas `%%` de cada modelo dizem as vistas e as regras daquele tipo.

![Modelo flowchart: VM e seus serviços](modelos/flowchart.mmd)

![Modelo classDiagram: módulos e referências permitidas](modelos/classDiagram.mmd)

![Modelo sequenceDiagram: cena do movimento](modelos/sequenceDiagram.mmd)

![Modelo eventmodeling: fluxo 3 do event storming, dia crítico](modelos/eventmodeling.mmd)

![Modelo erDiagram: os três schemas](modelos/erDiagram.mmd)

![Modelo swimlane-beta: release e rollback](modelos/swimlane.mmd)

![Modelo gantt: plano até a entrega](modelos/gantt.mmd)

![Modelo treeView-beta: árvore do repositório](modelos/treeView.mmd)

## O que não foi verificado

- Safari e Firefox não foram abertos. Tudo foi visto no Chrome.
- A pinça de verdade num trackpad ou numa tela de toque. O zoom foi testado com roda + Ctrl, duplo clique, teclas e arrasto simulados.
- O GitHub de verdade não foi aberto. No README, o SVG do `npm run render` aparece como imagem e encolhe para a coluna quando passa de cerca de 820 px.
- As classes de event storming usam `color-mix()` dentro do SVG. Isso funcionou no Chrome.
- A galeria mede o texto no canvas do navegador e não traduz as raias do eventmodeling. O site e o `npm run render` medem pela tabela de larguras e traduzem.
