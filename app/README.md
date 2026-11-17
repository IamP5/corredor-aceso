# App da ACS do Corredor Aceso

App Expo da agente comunitária de saúde (ACS). Em dia de calor extremo ou de apagão, ela abre o celular e vê só as casas de idosos que ficaram em silêncio. É a entrega de Mobile Development e o jeito de mostrar o fluxo do cidadão.

Os dados de pessoas são simulados. Nenhum nome, endereço ou telefone do app é de uma pessoa real.

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| Nome da CitizenTech | Corredor Aceso |
| Cidade e bairro | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da Estratégia Saúde da Família, com o Jardim Helena como referência. |
| Cidadão beneficiado | Idosos de 60 anos ou mais que moram sozinhos, e casas com pessoa que depende de aparelho na tomada. Quem opera é a agente comunitária de saúde (ACS). |
| Problema, em uma frase | Em dia de calor extremo ou de apagão, a ACS não consegue checar todos os idosos sozinhos da microárea e não sabe quem está em risco. |
| Como funciona | Um aparelho no corredor acende a luz quando a idosa passa, e esse movimento vale como "estou bem". Um modelo treinado com dados públicos decide se o dia é crítico. No dia crítico, o celular da ACS mostra só as casas que ficaram em silêncio. Aparelho mudo aparece em cinza e nunca vira verde. |
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

Precisa de Node 20.19 ou mais novo. Os testes usam o SQLite que vem no Node, então pedem Node 22.13 ou mais novo. No iPhone, instale o Expo Go. O app usa o Expo SDK 54.

```bash
npm --prefix app ci
```

```bash
cd app && npx expo start
```

Leia o QR code com a câmera do iPhone. O Expo Go abre o app.

Sem `EXPO_PUBLIC_API_URL`, o app usa o mock, com 40 casas simuladas. Para falar com a API .NET, passe o endereço dela. O celular precisa alcançar esse endereço pela rede, então use o IP do computador, não `localhost`:

```bash
cd app && EXPO_PUBLIC_API_URL=http://<ip-do-computador>:<porta> npx expo start
```

Para ver no navegador:

```bash
cd app && npx expo start --web
```

Antes de a API existir, dá para provar o cliente HTTP e o caminho sem rede com o servidor mock, que serve o mesmo mock por HTTP:

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

`check:app` roda o TypeScript e o ESLint. `test` roda os testes do Jest. `contraste` mede cada par de cores contra 4,5:1.

## Telas

As telas abaixo vieram do alvo web, numa janela de 390 por 844 pontos, e do Expo Go no simulador do iPhone 16. A pasta [docs/evidencias/mobile/](../docs/evidencias/mobile/) tem todas.

| Tela | O que a ACS faz | Imagem |
|---|---|---|
| Entrada | Digita o código de ACS e a microárea. A sessão fica no `expo-secure-store`. | [01-entrada.png](../docs/evidencias/mobile/01-entrada.png), [no iPhone](../docs/evidencias/mobile/ios/01-entrada.png) |
| Dia crítico | Vê se hoje é dia crítico e quantas casas estão urgentes, cinza, atendidas e verdes. | [02-dia-critico.png](../docs/evidencias/mobile/02-dia-critico.png), [no iPhone](../docs/evidencias/mobile/ios/02-dia-critico.png) |
| Radar | Vê as casas em três grupos: precisam de visita, já atendidas e aguardando. Cada card tem selo com símbolo e nome, o motivo em frase e a hora. | [03-radar.png](../docs/evidencias/mobile/03-radar.png), [04-radar-cinza-e-atendida.png](../docs/evidencias/mobile/04-radar-cinza-e-atendida.png), [no iPhone](../docs/evidencias/mobile/ios/03-radar.png) |
| Casa | Vê o nome, o endereço, o perfil, o vizinho de confiança com botão de ligar, as visitas dela e a linha do tempo das últimas 24 horas. | [05-casa.png](../docs/evidencias/mobile/05-casa.png), [06-casa-linha-do-tempo.png](../docs/evidencias/mobile/06-casa-linha-do-tempo.png), [no iPhone](../docs/evidencias/mobile/ios/04-casa.png) |
| Visita | Escolhe o desfecho entre quatro opções grandes e escreve uma observação, se quiser. | [07-visita.png](../docs/evidencias/mobile/07-visita.png), [08-casa-depois-da-visita.png](../docs/evidencias/mobile/08-casa-depois-da-visita.png) |
| Mensagem para o grupo | Escreve a mensagem e confere antes de mandar. Se a mensagem expõe a pessoa, o app explica por quê em duas frases e oferece a versão segura. | [09-mensagem-bloqueada.png](../docs/evidencias/mobile/09-mensagem-bloqueada.png), [10-mensagem-liberada.png](../docs/evidencias/mobile/10-mensagem-liberada.png) |
| Encerrar o turno | Encerra o dia crítico e vê o indicador: quantas casas deram sinal de vida até as 10h. | [17-encerrar.png](../docs/evidencias/mobile/17-encerrar.png), [18-turno-encerrado.png](../docs/evidencias/mobile/18-turno-encerrado.png), [19-depois-do-turno.png](../docs/evidencias/mobile/19-depois-do-turno.png) |

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

O cliente HTTP manda `Authorization: Bearer <código da ACS>`. O modelo de domínio não define a autenticação, então a equipe da API precisa confirmar esse formato.

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
| Contraste de 4,5:1 | Todo par de texto e fundo está em `PARES_DE_CONTRASTE`, em `src/ui/tema.ts`. O menor é 5,72:1. | `npm run contraste` e o teste `contraste.test.ts`. Saída em [contraste.txt](../docs/evidencias/mobile/contraste.txt). |
| Toque de 44 pontos ou mais | Botões, cards e opções têm altura mínima de 48. O botão de voltar do cabeçalho foi trocado por um de 48, porque o padrão media 30 na web. | Medido na página: o menor alvo das telas de radar, casa e visita tem 48 pontos. |
| Letra grande | Corpo de 20, títulos de 28, nada abaixo de 17. | Prints. |
| Leitor de tela | Cada card lê situação, nome, endereço, motivo e visita numa frase. Títulos têm papel de cabeçalho. As opções da visita são `radio`. Faixas de aviso anunciam mudança. | Árvore de acessibilidade do Chrome, nos prints do alvo web. |
| Linguagem simples | Frases curtas, sem sigla técnica: "Ninguém passou no corredor", "Aparelho sem sinal desde 09h10". Código de erro nunca aparece cru. | Teste `textos.test.ts` confere que nenhum código de erro aparece na frase. |

## Testes

```bash
npm --prefix app test
```

Os testes cobrem o que pode quebrar sem ninguém ver. O SQL roda de verdade, no SQLite do Node, com os `CHECK` do esquema.

| Arquivo | O que prova |
|---|---|
| `radarNaTela.test.ts` | A junção do radar com a lista de nomes: ordem do servidor, card sem nome continua visível, hora em UTC casa com o aviso, só o último desfecho aparece, a lista é trocada inteira, desfecho enviado com mais de 24 horas sai. |
| `fila.test.ts` | A fila: grava sem rede e manda o mesmo `id` quando volta, envia da mais antiga, para no 503, recusa no 422 e segue, para no 401, não envia duas vezes ao mesmo tempo. Também o mock: repetição, efeito da visita no radar, rede caída e mensagem bloqueada. |
| `textos.test.ts` | Situação e motivo viram texto com a hora de São Paulo, radar velho se declara velho, código de erro vira frase. |
| `contraste.test.ts` | Cada par de cores passa de 4,5:1. |

## Roteiro do vídeo de 3 minutos

| Tempo | Na tela | Fala |
|---|---|---|
| 0:00 a 0:20 | Tela de entrada no iPhone. | "Dia de calor extremo na Zona Leste. A Maria é agente comunitária de saúde e tem 40 idosos na microárea. Ela não consegue bater em todas as portas. O Corredor Aceso mostra a quem ir primeiro." |
| 0:20 a 0:45 | Toca em Entrar. Tela do dia crítico. | "O modelo previu dia crítico. O app diz quantas casas estão urgentes, quantas estão com aparelho sem sinal e quantas deram sinal hoje. Verde só aparece com prova de vida." |
| 0:45 a 1:15 | Radar. Rola pelos cards. | "O radar mostra só quem precisa de visita. Cada card diz o motivo em palavras, não só pela cor: sem energia numa casa com concentrador de oxigênio, ninguém passou no corredor, aparelho sem sinal. O cinza nunca vira verde." |
| 1:15 a 1:40 | Abre a casa de Benedito. Mostra vizinho e linha do tempo. | "Na casa, ela vê o vizinho de confiança, liga com um toque e confere a linha do tempo das últimas 24 horas. O nome só existe aqui, no celular. O servidor do radar só conhece um código aleatório." |
| 1:40 a 2:10 | Liga o modo avião. Registra "Estava fora". Mostra "Ainda não enviado". Desliga o modo avião. Mostra "Enviado". | "Na rua, a internet cai. O radar continua na tela e a visita fica guardada no celular. Quando a rede volta, o app manda sozinho, com o mesmo código, e o sistema não registra a visita duas vezes." |
| 2:10 a 2:35 | Mensagem para o grupo: "Ele mora sozinho e toma diurético". Bloqueada. Toca em "Usar esta versão". | "Ela quer pedir ajuda no grupo do bairro. O app barra a mensagem que diz que ele mora sozinho e fala da saúde, explica por quê e oferece uma versão segura." |
| 2:35 a 3:00 | Encerrar o turno. Mostra o indicador. | "No fim do dia, ela encerra o turno e vê o indicador: quantos idosos deram sinal de vida até as 10h. A meta é 100%. Os dados deste vídeo são simulados." |

Para gravar no iPhone sem a API, rode o app sem `EXPO_PUBLIC_API_URL`. O mock lê o estado da rede do `NetInfo` e falha como sem internet quando o celular está desconectado. Isso está coberto pelo teste do mock com rede caída, mas o modo avião no iPhone não foi gravado: ensaie esse trecho antes do vídeo. Com o modo avião, o Expo Go também perde a conexão com o computador, então abra o app antes e não recarregue no meio.

## Limites conhecidos

- A API .NET ainda não existia quando o app foi feito. O cliente HTTP foi provado contra o servidor mock, que segue os contratos da seção 6.3, e não contra a API real.
- O radar não traz a probabilidade nem o motivo do dia crítico. O modelo de dados lista o campo `diaCritico` em `GET /radar` como decisão em aberto. A tela do dia crítico usa só `turnoAberto`.
- O cabeçalho de autenticação é uma suposição. Ver "Dados da API".
- No alvo web, recarregar a página logo depois de fechar outra aba do app pode falhar com `NoModificationAllowedError`, porque o SQLite web ainda segura o arquivo da aba anterior. Fechar a aba e abrir de novo resolve. No celular isso não acontece.
- O CI roda só `check:app`. Os testes do Jest não rodam no CI, porque o `package.json` da raiz não chama `npm --prefix app test`.
