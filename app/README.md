# App da ACS do Corredor Aceso

App Expo da agente comunitária de saúde (ACS). Em dia de calor extremo ou de apagão, a ACS abre o celular e vê só as casas de idosos que ficaram em silêncio. O app é a entrega de Mobile Development e demonstra o fluxo do cidadão.

Os dados de pessoas são simulados. Nenhum nome, endereço ou telefone do app é de uma pessoa real.

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| Nome da CitizenTech | Corredor Aceso |
| Cidade e bairro | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da Estratégia Saúde da Família, com o Jardim Helena como referência. |
| Cidadão beneficiado | Idosos de 60 anos ou mais que moram sozinhos, e casas com pessoa que depende de aparelho na tomada. Quem opera é a agente comunitária de saúde (ACS). |
| Problema, em uma frase | Em dia de calor extremo ou de apagão, a agente comunitária de saúde (ACS) não tem tempo de visitar todos os idosos que moram sozinhos na microárea e não tem como saber quais deles estão sem dar sinal de vida. |
| Como funciona | Um aparelho instalado no corredor da casa acende a luz quando a pessoa idosa passa à noite. Cada passagem fica registrada como sinal de vida daquela casa. Às 05h, o sistema verifica, com dados públicos do INMET, se a noite foi quente. Quando foi, o dia é declarado crítico. No dia crítico, o aplicativo da ACS lista apenas as casas que ficaram sem sinal de vida, e são essas que ela visita. A casa cujo aparelho parou de transmitir aparece em cinza e nunca é mostrada como verde. Um modelo de ML treinado com dados do INMET e do SIH/DATASUS foi comparado à regra da noite quente e não a superou. Por isso, a regra decide o dia crítico. A luz também atende a um ponto levantado na descoberta, feita com personas simuladas: com o corredor escuro e o interruptor longe, a pessoa idosa bebe menos água para não precisar levantar à noite. Com o caminho iluminado, ela pode se hidratar sem medo de cair. |
| O que cada parte faz | Ver a tabela "Partes do repositório" no `README.md` da raiz. |
| Link do repositório | https://github.com/IamP5/corredor-aceso |
| Indicador de impacto | Percentual de idosos sozinhos da microárea com contato confirmado até as 10h em dia crítico. Meta: 100%. Indicador de segurança: nenhuma casa em verde sem prova de vida. |
| ODS conectados | ODS 3 (metas 3.4 e 3.d), ODS 10 (meta 10.2), ODS 11 (meta 11.5) e ODS 13 (meta 13.1). |

## O que a disciplina avalia e onde está

| Critério | Onde |
|---|---|
| Telas e navegação | Sete telas com Expo Router em `src/app/`. Ver "Telas". |
| Consumo de dados | Cliente HTTP tipado em `src/api/cliente.ts`, com as rotas da seção 6.3 do [modelo de domínio](../docs/arquitetura/03-modelo-de-dominio.md). Mock com os mesmos contratos em `src/api/mock/`. |
| Banco de dados | expo-sqlite em `src/banco/`, com as tabelas da seção 6 do [modelo de dados](../docs/arquitetura/04-modelo-de-dados.md). |
| Acessibilidade | Ver "Acessibilidade". |
| Aderência ao problema | O radar mostra só quem precisa de visita, funciona sem internet e nunca mostra verde sem prova. |
| Documentação | Este arquivo e as evidências em [docs/evidencias/mobile/](../docs/evidencias/mobile/). |

## Como rodar

O app exige Node 20.19 ou mais novo. Os testes usam o SQLite embutido no Node e por isso exigem Node 22.13 ou mais novo. No iPhone, é preciso instalar o Expo Go. O app usa o Expo SDK 54.

```bash
npm --prefix app ci
```

```bash
cd app && npx expo start
```

Leia o QR code com a câmera do iPhone. O Expo Go abre o app.

Sem `EXPO_PUBLIC_API_URL`, o app usa o mock, com 40 casas simuladas. Para conectar o app à API .NET, informe o endereço dela. O celular precisa alcançar esse endereço pela rede, então o endereço deve usar o IP do computador, não `localhost`:

```bash
cd app && EXPO_PUBLIC_API_URL=http://<ip-do-computador>:<porta> npx expo start
```

Na entrada, o código de acesso é o token da ACS que a API lê da configuração. No notebook, com a API em `Development`, é `dev-acs-demo-01-so-no-notebook`, na microárea 3 (`api/README.md`, "Autenticação"). Para subir a API, o banco, a carga e o app de uma vez, use `bash tools/demo.sh subir` e depois `bash tools/demo.sh app`, que abre o alvo web ligado à API em `http://localhost:5080`.

Para ver no navegador:

```bash
cd app && npx expo start --web
```

Sem a API, o servidor mock permite verificar o cliente HTTP e o caminho sem rede, porque serve o mesmo mock por HTTP:

```bash
cd app && npm run api:mock -- 3001
```

```bash
cd app && EXPO_PUBLIC_API_URL=http://localhost:3001 npx expo start --web
```

### Verificações

```bash
npm run check:app
```

```bash
npm --prefix app test
```

```bash
npm --prefix app run contraste
```

`check:app` roda o TypeScript e o ESLint. `test` roda os testes do Jest. `contraste` mede cada par de cores contra 4,5:1. O CI roda `check:app` e `test:app`, que chama os testes do Jest.

## Telas

As telas abaixo foram capturadas no alvo web, numa janela de 390 por 844 pontos, e do Expo Go no simulador do iPhone 16. A pasta [docs/evidencias/mobile/](../docs/evidencias/mobile/) tem todas.

| Tela | O que a ACS faz | Imagem |
|---|---|---|
| Entrada | Digita o código de acesso que a UBS entregou e a microárea. A sessão fica no `expo-secure-store`. | [01-entrada.png](../docs/evidencias/mobile/01-entrada.png), [no iPhone](../docs/evidencias/mobile/ios/01-entrada.png) |
| Dia crítico | Vê se hoje é dia crítico e quantas casas estão urgentes, cinza, atendidas e verdes. | [02-dia-critico.png](../docs/evidencias/mobile/02-dia-critico.png), [no iPhone](../docs/evidencias/mobile/ios/02-dia-critico.png) |
| Radar | Vê as casas em três grupos: precisam de visita, já atendidas e aguardando. Cada card tem selo com símbolo e nome, o motivo em frase e a hora. | [03-radar.png](../docs/evidencias/mobile/03-radar.png), [04-radar-cinza-e-atendida.png](../docs/evidencias/mobile/04-radar-cinza-e-atendida.png), [no iPhone](../docs/evidencias/mobile/ios/03-radar.png) |
| Casa | Vê o nome, o endereço, o perfil, o vizinho de confiança com botão de ligar, as visitas dela e a linha do tempo das últimas 24 horas. | [05-casa.png](../docs/evidencias/mobile/05-casa.png), [06-casa-linha-do-tempo.png](../docs/evidencias/mobile/06-casa-linha-do-tempo.png), [no iPhone](../docs/evidencias/mobile/ios/04-casa.png) |
| Visita | Escolhe o desfecho entre quatro opções grandes e escreve uma observação, se quiser. | [07-visita.png](../docs/evidencias/mobile/07-visita.png), [08-casa-depois-da-visita.png](../docs/evidencias/mobile/08-casa-depois-da-visita.png) |
| Mensagem para o grupo | Escreve a mensagem e confere antes de mandar. Se a mensagem expõe a pessoa, o app explica por quê em duas frases e oferece a versão segura. | [09-mensagem-bloqueada.png](../docs/evidencias/mobile/09-mensagem-bloqueada.png), [10-mensagem-liberada.png](../docs/evidencias/mobile/10-mensagem-liberada.png) |
| Resumo do turno | A ACS encerra o turno na tela do dia, depois de confirmar num diálogo do sistema. Esta tela mostra o indicador: quantas casas deram sinal de vida até as 10h. | [17-encerrar.png](../docs/evidencias/mobile/17-encerrar.png), [18-turno-encerrado.png](../docs/evidencias/mobile/18-turno-encerrado.png), [19-depois-do-turno.png](../docs/evidencias/mobile/19-depois-do-turno.png) |

## Como o app funciona

```text
src/
  app/        telas do Expo Router
  api/        contratos (zod), cliente HTTP, configuração e mock
  banco/      esquema do SQLite, cache e fila de desfechos
  dominio/    hora de São Paulo, textos de situação e regra de envio
  estado/     sessão, provedor que liga API, banco e rede
  ui/         tema, componentes e card da casa
  __testes__/ testes do Jest
ferramentas/  servidor mock HTTP, medidor de contraste, gerador das casas do mock
```

### Dados da API

`src/api/contratos.ts` descreve cada resposta da seção 6.3 com zod. O tipo TypeScript sai do mesmo esquema, e toda resposta passa pelo esquema antes de entrar no app. Uma resposta fora do contrato não chega ao banco.

`src/api/config.ts` lê `EXPO_PUBLIC_API_URL`. Com o endereço, o app usa o cliente HTTP. Sem ele, usa o mock. O mock guarda estado em memória: uma visita registrada muda o radar seguinte, pelas regras da seção 5.7 do modelo de domínio. O cenário é o fluxo 3 do event storming às 10h31: uma casa sem energia, uma casa sem movimento, um aparelho mudo, uma casa cinza já atendida, nove aguardando e 27 verdes.

O cliente HTTP manda `Authorization: Bearer <código de acesso>`, com o código como a ACS digitou. É o token que a API lê da configuração, e ela descobre por ele quem é a ACS e a microárea. O código não aparece na tela. O teste `cliente.test.ts` confere o cabeçalho.

### Banco do celular

O banco se chama `corredor.db` e segue a seção 6 do modelo de dados. O teste confere que o esquema do app é igual ao arquivo `docs/arquitetura/referencia/dados/sqlite/app.sql`.

| Tabela | Para quê |
|---|---|
| `lista_de_nomes` | Nome, endereço e vizinho de cada casa. É o único lugar em que o token encontra o nome. |
| `radar_cabecalho`, `radar_card` | O último radar, na ordem do servidor. O radar continua na tela sem internet. |
| `aviso` | Os avisos ao vizinho do dia, para o card dizer se o vizinho respondeu. |
| `desfecho` | A fila de visitas. O `id` nasce no celular e é a chave de idempotência de `POST /api/visitas`. |
| `sincronizacao` | A hora em que cada recurso foi baixado. Radar com mais de 2 minutos se declara velho. |

Cada recurso é trocado inteiro numa transação. A visita é gravada antes de qualquer chamada de rede. O app envia a mais antiga, uma por vez, ao abrir, ao voltar a rede, depois de cada registro e a cada 15 segundos enquanto houver fila. Erro de rede e 5xx deixam a visita na fila. 4xx marca como "Não enviado", com o motivo em texto. 401 para a fila e pede a entrada de novo.

O SQLite do celular não é cifrado nesta entrega. A proteção é a área privada do app e o bloqueio do aparelho. Na saída da conta, o app apaga as seis tabelas e avisa antes se ainda há visita na fila.

### Caminho sem rede

O caminho foi rodado duas vezes, com o celular sem internet e com a API fora do ar. Os passos, os prints e o log do servidor estão em [caminho-sem-rede.md](../docs/evidencias/mobile/caminho-sem-rede.md).

## Acessibilidade

| Requisito | Como o app atende | Como foi conferido |
|---|---|---|
| Cor nunca sozinha | Cada situação tem selo com símbolo e nome (! Urgente, ? Aparelho sem sinal, … Aguardando, ✓ Deu sinal hoje), barra lateral e frase do motivo. | Teste `textos.test.ts` confere que nome, símbolo e ação são diferentes para cada situação. |
| Contraste de 4,5:1 | Todo par de texto e fundo está em `PARES_DE_CONTRASTE`, em `src/ui/tema.ts`. O menor é 5,41:1. | `npm run contraste` e o teste `contraste.test.ts`. Saída em [contraste.txt](../docs/evidencias/mobile/contraste.txt). |
| Toque de 44 pontos ou mais | Botões, cards e opções têm altura mínima de 48. Na web, o botão de voltar do cabeçalho foi trocado por um de 48, porque o padrão media 30. No iPhone e no Android fica o botão nativo. | Medido na página: o menor alvo das telas de radar, casa e visita tem 48 pontos. |
| Letra grande | Corpo de 18, títulos de 26, título grande do iOS no início e no radar, nada abaixo de 17. | Prints. |
| Liquid Glass | No iOS 26, o botão da barra de ação ("Registrar visita", "Salvar visita", "Verificar mensagem") é uma cápsula de vidro tingida com a cor principal, e a barra de cima usa o vidro do sistema. Só a camada de controles usa vidro; cartões e listas continuam sólidos. Fora do iOS 26 as mesmas peças usam cor sólida. Os cantos usam a curva contínua da Apple e o raio de dentro é o de fora menos o respiro (`src/ui/plataforma.ts`). | Simulador do iPhone 17 com iOS 26.5 e do iPhone 16 com iOS 18.4. |
| Movimento reduzido | As animações (entrada em cascata, toque dos botões, avisos que surgem, barra do encerramento) ficam em `src/ui/movimento.tsx` e usam o Reanimated, que segue a opção "Reduzir movimento" do sistema. A contagem do percentual mostra o número final direto quando a opção está ligada. As entradas duram até 300 ms. Linhas de lista não têm entrada e não encolhem ao toque: escurecem o fundo, ou fazem o ripple no Android. A barra e a contagem do encerramento duram 900 ms, e nenhuma animação segura um toque. | Código em `movimento.tsx`. |
| Leitor de tela | Cada card lê situação, nome, endereço, motivo e visita numa frase. Títulos têm papel de cabeçalho. As opções da visita são `radio`. Faixas de aviso anunciam mudança. | Árvore de acessibilidade do Chrome, nos prints do alvo web. |
| Linguagem simples | Frases curtas, sem sigla técnica: "Ninguém passou no corredor", "Aparelho sem sinal desde 09h10". Código de erro nunca aparece cru. | Teste `textos.test.ts` confere que nenhum código de erro aparece na frase. |

## Testes

```bash
npm --prefix app test
```

Os testes cobrem falhas que não apareceriam na interface. O SQL executa de fato no SQLite do Node, com os `CHECK` do esquema.

| Arquivo | O que prova |
|---|---|
| `radarNaTela.test.ts` | A junção do radar com a lista de nomes: ordem do servidor, card sem nome continua visível, hora em UTC casa com o aviso, só o último desfecho aparece, a lista é trocada inteira, desfecho enviado com mais de 24 horas sai. |
| `fila.test.ts` | A fila: grava sem rede e manda o mesmo `id` quando volta, envia da mais antiga, para no 503, recusa no 422 e segue, para no 401, não envia duas vezes ao mesmo tempo. Também o mock: repetição, efeito da visita no radar, rede caída e mensagem bloqueada. |
| `cliente.test.ts` | O cliente HTTP manda o código de acesso como está em `Authorization: Bearer` e na rota `/api`, e a entrada aceita o token da API. |
| `textos.test.ts` | Situação e motivo viram texto com a hora de São Paulo, radar velho se declara velho, código de erro vira frase. |
| `contraste.test.ts` | Cada par de cores passa de 4,5:1. |

## Limites conhecidos

- O caminho sem rede com o modo avião ligado não foi exercitado em um iPhone. O comportamento está coberto pelo teste do mock com a rede caída.
- Em 17/11/2026, o alvo web rodou contra a API real, com a carga da demo. Os prints e o caminho sem rede com a API parada estão em [docs/evidencias/integracao/](../docs/evidencias/integracao/RELATORIO.md). No celular, o app ainda não se conectou à API real.
- O radar não traz a probabilidade nem o motivo do dia crítico, porque `GET /radar` não expõe esses dados. A tela do dia crítico usa só `turnoAberto`.
- No alvo web, recarregar a página logo depois de fechar outra aba do app pode falhar com `NoModificationAllowedError`, porque o SQLite web ainda segura o arquivo da aba anterior. Fechar a aba e abri-la de novo resolve o problema. No celular, a falha não ocorre.
