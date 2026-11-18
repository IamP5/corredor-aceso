# Corredor Aceso

Global Solution FIAP 2026.2, CitizenTech. Engenharia de Software, 3º ano, turma de fevereiro.

**Professores:** o passo a passo para conferir cada disciplina está no **[Guia de avaliação](https://iamp5.github.io/corredor-aceso/)**. Os requisitos de todas as disciplinas, com pontos, formato de entrega e onde cada item está, ficam em [REQUISITOS.md](REQUISITOS.md).

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| Nome da CitizenTech | Corredor Aceso |
| Cidade e bairro | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da Estratégia Saúde da Família, com o Jardim Helena como referência. |
| Cidadão beneficiado | Idosos de 60 anos ou mais que moram sozinhos, e casas com pessoa que depende de aparelho na tomada. Quem opera é a agente comunitária de saúde (ACS). |
| Problema, em uma frase | Em dia de calor extremo ou de apagão, a agente comunitária de saúde (ACS) não tem tempo de visitar todos os idosos que moram sozinhos na microárea e não tem como saber quais deles estão sem dar sinal de vida. |
| Como funciona | Um aparelho instalado no corredor da casa acende a luz quando a pessoa idosa passa à noite. Cada passagem fica registrada como sinal de vida daquela casa. Às 05h, o sistema verifica, com dados públicos do INMET, se a noite foi quente. Quando foi, o dia é declarado crítico. No dia crítico, o aplicativo da ACS lista apenas as casas que ficaram sem sinal de vida, e são essas que ela visita. A casa cujo aparelho parou de transmitir aparece em cinza e nunca é mostrada como verde. Um modelo de ML treinado com dados do INMET e do SIH/DATASUS foi comparado à regra da noite quente e não a superou. Por isso, a regra decide o dia crítico. A luz também atende a um ponto levantado na descoberta, feita com personas simuladas: com o corredor escuro e o interruptor longe, a pessoa idosa bebe menos água para não precisar levantar à noite. Com o caminho iluminado, ela pode se hidratar sem medo de cair. |
| O que cada parte faz | Ver a tabela "Partes do repositório". |
| Link do repositório | https://github.com/IamP5/corredor-aceso |
| Guia de avaliação | https://iamp5.github.io/corredor-aceso/ |
| Indicador de impacto | Percentual de idosos sozinhos da microárea com contato confirmado até as 10h em dia crítico. Meta: 100%. Indicador de segurança: nenhuma casa em verde sem sinal de vida. |
| ODS conectados | ODS 3 (metas 3.4 e 3.d), ODS 10 (meta 10.2), ODS 11 (meta 11.5) e ODS 13 (meta 13.1). |

Os dados de pessoas neste repositório são simulados. Os dados de clima e de internações são públicos, do INMET e do SIH/DATASUS. As entrevistas e os testes da fase de descoberta foram simulados com personas, e isso está dito em cada documento.

## Partes do repositório

| Pasta | O que é | Disciplina |
|---|---|---|
| `api/src/Corredor.Motor` | Regras de contato e radar. Só conhece o token da casa. | C# Software Development |
| `api/src/Corredor.Cadastro` | Idoso, consentimento, vizinho de confiança e visita. Único dono de dado pessoal. | SOA e Web Services |
| `api/src/Corredor.Guarda` | Detecção de PII na entrada, bloqueio nas saídas e tradução por LLM. | Cybersecurity |
| `api/src/Corredor.Api` | Host da Web API, Swagger e injeção de dependência. | C# e SOA |
| `app/` | App Expo da ACS, com o radar e o banco local. | Mobile Development |
| `iot/` | Nó ESP32 para o Wokwi e simulador de casas. | Physical Computing |
| `ml/` | Notebook, relatório e job do modelo de dia crítico. | IA e Machine Learning |
| `infra/` | Scripts Bash, units do systemd e Terraform da VM Linux. | Operating Systems |
| `docs/arquitetura/` | Event storming, modelo de domínio, modelo de dados, infraestrutura e diagramas. | todas |
| `docs/entregaveis.md` | O que cada disciplina pede e onde está. | todas |

## Onde avaliar cada disciplina

Cada linha diz o que olhar, onde estão as evidências e o que vai na entrega. O detalhe de cada critério está no README da pasta, e a situação de cada item está em [docs/entregaveis.md](docs/entregaveis.md).

| Disciplina | O que olhar | Evidências | Entrega |
|---|---|---|---|
| SOA e Web Services | [api/README.md](api/README.md#soa-e-web-services): domínio, banco e migrações do módulo Cadastro, Swagger e testes | [docs/evidencias/api/](docs/evidencias/api/README.md), [relatório da integração](docs/evidencias/integracao/RELATORIO.md) | link do GitHub |
| C# Software Development | [api/README.md](api/README.md#c-software-development): orientação a objetos, interfaces, injeção de dependência, EF Core, LINQ e async no Motor | [docs/evidencias/api/](docs/evidencias/api/README.md), [relatório da integração](docs/evidencias/integracao/RELATORIO.md) | link do GitHub |
| Cybersecurity | `api/src/Corredor.Guarda`: detecção de dado pessoal, bloqueio nas saídas, tradução por LLM e painel Risco Cidadão | [docs/evidencias/cyber/](docs/evidencias/cyber/LEIA-ME.md), rascunho do PDF em [docs/entrega/cyber-documentacao.md](docs/entrega/cyber-documentacao.md) | PDF e link do GitHub |
| IA e Machine Learning | [ml/README.md](ml/README.md), o notebook `ml/corredor_aceso.ipynb` e o [relatório](ml/RELATORIO_ML.md) | o notebook executado | link do GitHub e vídeo |
| Mobile Development | [app/README.md](app/README.md): telas, consumo da API, banco local e acessibilidade | [docs/evidencias/mobile/](docs/evidencias/mobile/) | link do GitHub e vídeo |
| Operating Systems | [infra/README.md](infra/README.md): scripts de rede e da aplicação, monitor, systemd, logs e teste de falha | [docs/evidencias/os/](docs/evidencias/os/README.md) | link do GitHub e vídeo |
| Physical Computing | [iot/README.md](iot/README.md): o nó ESP32 no Wokwi e o [painel web](https://iamp5.github.io/corredor-aceso/painel/) via MQTT | [docs/evidencias/iot/](docs/evidencias/iot/README.md) | link do Wokwi, vídeo e `integrantes.txt` |
| Testing e QA | [docs/qa/features.md](docs/qa/features.md) e [docs/qa/casos-de-teste.md](docs/qa/casos-de-teste.md), cadastrados no Azure Boards | prints do Azure Boards | PDF com o link do Azure Boards e o vídeo de pitch |

## Vídeos

| Vídeo | Duração máxima | Link |
|---|---|---|
| Pitch | 4 min | a publicar |
| Mobile Development | 3 min | a publicar |
| Physical Computing | 4 min | a publicar |
| IA e Machine Learning | 5 min | a publicar |
| Operating Systems | 7 min | a publicar |
| Painel de Cybersecurity | sem limite | vai dentro do PDF de Cyber |

## Documentação

- [Visão geral da arquitetura](docs/arquitetura/01-visao-geral.md)
- [Event storming](docs/arquitetura/02-event-storming.md)
- [Modelo de domínio](docs/arquitetura/03-modelo-de-dominio.md)
- [Modelo de dados](docs/arquitetura/04-modelo-de-dados.md)
- [Infraestrutura e entrega](docs/arquitetura/05-infra-e-entrega.md)
- [Requisitos de todas as disciplinas](REQUISITOS.md)
- [Entregáveis por disciplina](docs/entregaveis.md)
- [Guia de avaliação](https://iamp5.github.io/corredor-aceso/), montado de `site/` por `bash tools/montar-site.sh`
- [Uso de IA no projeto](prompts.md)

A versão em HTML, com os diagramas em tamanho natural, abre por duplo clique em `docs/arquitetura/site/index.html`.

## Como rodar

Você só precisa do Docker com o Docker Compose. O .NET, o Node e o Python rodam dentro das imagens, e nada é instalado no seu computador. Os dados de pessoas são simulados.

### Subir a solução

Na raiz do repositório:

```bash
docker compose up --build
```

A primeira vez leva alguns minutos, porque o Docker baixa as imagens e compila a API. O compose sobe, nesta ordem:

| Serviço | O que faz |
|---|---|
| `postgres` e `mosquitto` | Banco e broker MQTT. |
| `preparar` | Cria os papéis do banco, aplica as migrações e cadastra as 40 casas da demo. Termina e sai. |
| `api` | A Web API, em `http://localhost:5080`. |
| `simulador` | As 39 casas simuladas, publicando no mesmo contrato MQTT do nó do Wokwi. |
| `app` | O app da ACS no navegador, em `http://localhost:8081`. |

Quando aparecer `corredor-api-1 ... Healthy`, abra:

- Swagger: `http://localhost:5080/swagger`. Para as rotas fechadas, clique em **Authorize** e cole `dev-acs-demo-01-so-no-notebook`.
- App da ACS: `http://localhost:8081`. Entre com o código `dev-acs-demo-01-so-no-notebook` e a microárea 3.
- Painel Risco Cidadão da Guarda: `http://localhost:5080/api/guarda/demo/painel`.

Para a Guarda usar o LLM, copie `.env.example` para `.env` e preencha `OPENROUTER_API_KEY` antes do `up`. Sem a chave, a Guarda usa uma frase fixa no lugar da explicação, e o resto funciona igual. O passo a passo para conseguir a chave está logo abaixo.

### Chave do OpenRouter

A chave é opcional. Ela só muda uma coisa: quando a Guarda bloqueia um dado pessoal, a explicação para o cidadão vem de um modelo de linguagem em vez de uma frase pronta.

O [OpenRouter](https://openrouter.ai) é um serviço que dá acesso a vários modelos de linguagem (da OpenAI, da Anthropic, do Google e outros) por uma API só. A chave de API é uma senha que identifica a sua conta nessas chamadas. A Guarda manda a chave em cada pedido de tradução, e o OpenRouter cobra o uso da sua conta.

Para conseguir a chave:

1. Crie uma conta em [openrouter.ai](https://openrouter.ai). Dá para entrar com Google, GitHub ou e-mail.
2. Coloque créditos em [openrouter.ai/settings/credits](https://openrouter.ai/settings/credits). O modelo padrão da demo, `openai/gpt-6-luna`, é pago. Cada tradução usa poucas centenas de tokens, então um crédito pequeno cobre muitas rodadas da demo.
3. Abra [openrouter.ai/keys](https://openrouter.ai/keys) e clique em **Create Key**. Dê um nome, por exemplo `corredor-demo`. Se quiser, defina um limite de gasto para a chave.
4. Copie a chave assim que ela aparecer. Ela começa com `sk-or-v1-`, e o site só mostra o valor completo uma vez. Se perder, apague e crie outra.
5. Na raiz do repositório, copie o modelo e cole a chave depois do `=`, sem aspas e sem espaços:

	```bash
	cp .env.example .env
	```

	```dotenv
	OPENROUTER_API_KEY=sk-or-v1-...
	```

6. Suba a solução de novo com `docker compose up --build`. Se ela já estava de pé, `docker compose up -d api` recria a API com a chave.

Trate a chave como senha. O `.env` está no `.gitignore` e nunca entra no Git. Não cole a chave em issue, chat ou print.

Para conferir, rode o passo de canal inseguro do roteiro da Guarda e olhe `docker compose logs api`. Se a chave falhar, a API registra `LLM respondeu 401` (chave errada ou apagada) ou `LLM respondeu 402` (conta sem crédito), e o painel mostra a frase fixa.

### Ver o dia crítico em 2 minutos

Com o compose de pé, rode em outro terminal:

1. Leve o relógio da demo para as 08h de amanhã. O turno da ACS vai das 05h às 18h, e o relógio da demo só anda para frente, então assim o roteiro funciona a qualquer hora:

	```bash
	curl -s -X PUT http://localhost:5080/api/demo/relogio -H "Content-Type: application/json" \
	  -d "{\"agora\":\"$(date -v+1d +%F 2>/dev/null || date -d tomorrow +%F)T08:00:00-03:00\"}"
	```

2. Declare o dia crítico, como o job de ML faz às 05h:

	```bash
	curl -s -X PUT "http://localhost:5080/api/dias-criticos/$(date -v+1d +%F 2>/dev/null || date -d tomorrow +%F)" \
	  -H "Content-Type: application/json" -d '{"probabilidade":0.11,"motivo":"Noite quente","origem":"MANUAL"}'
	```

3. Espere uns 30 segundos e veja o radar da ACS, ou atualize o app:

	```bash
	curl -s -H "Authorization: Bearer dev-acs-demo-01-so-no-notebook" "http://localhost:5080/api/radar?microarea=3"
	```

	Em `pendentes` aparecem as casas acamadas e as casas com calor em `Vermelho`, e os aparelhos mudos em `Cinza`. A contagem de `verdes` sobe conforme os movimentos chegam. Nenhuma casa fica verde sem sinal de vida.

### Outros comandos

```bash
docker compose logs -f api          # log da API, onde a Guarda mostra o que mascarou
docker compose logs -f simulador    # pacotes de cada casa
docker compose --profile ml run --rm ml python prever_hoje.py --dia-das-variaveis 2025-12-29 --seco
docker compose --profile ml run --rm ml jupyter nbconvert --to notebook --execute --inplace corredor_aceso.ipynb
docker compose down                 # para tudo e guarda o banco
docker compose down -v              # para tudo e apaga o banco, para começar do zero
```

O job das 05h roda sem `--seco` para enviar o pedido à API. Ele usa a data do seu computador, então faça isso antes de mexer no relógio da demo.

### Sem Docker

Os testes automatizados, as verificações do CI e o roteiro completo de cenas rodam com as ferramentas instaladas no computador: SDK do .NET 10 (o `global.json` pede 10.0.x), Node 22.13 ou mais novo, Python 3.11 ou mais novo, e o Docker para o Postgres e o Mosquitto. Os comandos ficam em `package.json`:

```bash
npm run demo        # sobe banco, API, carga e simulador e roda todas as cenas, em cerca de 10 minutos
npm run check       # roda as verificações de todas as partes
npm run test:app    # roda os testes do app
npm run dev:deps    # só sobe o Postgres e o Mosquitto
```

`bash tools/demo.sh subir` sobe a mesma demo sem contêiner para a API, em `http://localhost:5080`. O que se viu em cada cena do `npm run demo` está no [relatório da integração](docs/evidencias/integracao/RELATORIO.md). As instruções de cada parte ficam no `README.md` da pasta dela.

## Integrantes

| Nome | RM |
|---|---|
| Bruno Dominicheli | 554981 |
| Miguel Kapicius | 556198 |
| Thiago Ferreira | 555608 |

A mesma lista está em `integrantes.txt`.
