# Corredor Aceso

Global Solution FIAP 2026.2, CitizenTech. Engenharia de Software, 3º ano, turma de fevereiro.

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| Nome da CitizenTech | Corredor Aceso |
| Cidade e bairro | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da Estratégia Saúde da Família, com o Jardim Helena como referência. |
| Cidadão beneficiado | Idosos de 60 anos ou mais que moram sozinhos, e casas com pessoa que depende de aparelho na tomada. Quem opera é a agente comunitária de saúde (ACS). |
| Problema, em uma frase | Em dia de calor extremo ou de apagão, a ACS não consegue checar todos os idosos sozinhos da microárea e não sabe quem está em risco. |
| Como funciona | Um aparelho no corredor acende a luz quando a idosa passa, e esse movimento vale como "estou bem". Às 05h, a regra da noite quente, com dados públicos do INMET, decide se o dia é crítico. Um modelo de ML treinado com INMET e SIH/DATASUS foi testado contra essa regra e não a superou. No dia crítico, o celular da ACS mostra só as casas que ficaram em silêncio. Aparelho mudo aparece em cinza e nunca vira verde. |
| O que cada parte faz | Ver a tabela "Partes do repositório". |
| Link do repositório | https://github.com/IamP5/corredor-aceso |
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
| Physical Computing | [iot/README.md](iot/README.md): o nó ESP32 no Wokwi e o simulador das casas | [docs/evidencias/iot/](docs/evidencias/iot/README.md) | link do Wokwi, vídeo e `integrantes.txt` |
| Testing e QA | [docs/qa/features.md](docs/qa/features.md) e [docs/qa/casos-de-teste.md](docs/qa/casos-de-teste.md), cadastrados no Azure Boards | prints do Azure Boards | PDF com o link do Azure Boards e o vídeo de pitch |

## Vídeos

| Vídeo | Duração máxima | Roteiro | Link |
|---|---|---|---|
| Pitch | 4 min | [docs/entrega/roteiros.md](docs/entrega/roteiros.md#pitch-4-minutos) | a publicar |
| Mobile Development | 3 min | [app/README.md](app/README.md#roteiro-do-vídeo-de-3-minutos) | a publicar |
| Physical Computing | 4 min | [iot/README.md](iot/README.md#roteiro-do-vídeo-de-physical-computing-até-4-minutos) | a publicar |
| IA e Machine Learning | 5 min | [ml/README.md](ml/README.md#roteiro-do-vídeo-até-5-minutos) | a publicar |
| Operating Systems | 7 min | [docs/entrega/roteiros.md](docs/entrega/roteiros.md#operating-systems-7-minutos) | a publicar |
| Painel de Cybersecurity | sem limite | [docs/entrega/roteiros.md](docs/entrega/roteiros.md#dashboard-de-cyber) | vai dentro do PDF de Cyber |

## Documentação

- [Visão geral da arquitetura](docs/arquitetura/01-visao-geral.md)
- [Event storming](docs/arquitetura/02-event-storming.md)
- [Modelo de domínio](docs/arquitetura/03-modelo-de-dominio.md)
- [Modelo de dados](docs/arquitetura/04-modelo-de-dados.md)
- [Infraestrutura e entrega](docs/arquitetura/05-infra-e-entrega.md)
- [Entregáveis por disciplina](docs/entregaveis.md)
- [Uso de IA no projeto](prompts.md)

A versão em HTML, com os diagramas em tamanho natural, abre por duplo clique em `docs/arquitetura/site/index.html`.

## Como rodar

### Pré-requisitos

- Docker com o Docker Compose.
- SDK do .NET 10. O `global.json` exige a versão 10.0.x.
- Node 22.13 ou mais novo.
- Python 3.11 ou mais novo.

Para a Guarda usar o LLM, copie `.env.example` para `.env` e preencha `OPENROUTER_API_KEY`. Sem a chave, a Guarda usa uma frase fixa no lugar da explicação do LLM, e o resto funciona igual.

As instruções de cada parte ficam no `README.md` da pasta dela.

### Ver funcionando em 5 minutos

Rode tudo na raiz do repositório. Os dados de pessoas são simulados.

1. Suba o banco, as migrações, a carga das 40 casas e a API:

	```bash
	bash tools/demo.sh subir
	```

	Termina com `API pronta em http://localhost:5080`. A porta é 5080 porque, no macOS, o AirPlay ocupa a 5000. O Swagger fica em `http://localhost:5080/swagger`. Para usar as rotas fechadas, clique em **Authorize** e cole `dev-acs-demo-01-so-no-notebook`.

2. Em outro terminal, ligue o simulador das 39 casas e deixe rodando:

	```bash
	python3 -m venv .demo/venv
	.demo/venv/bin/pip install -r iot/simulador/requirements.txt
	.demo/venv/bin/python iot/simulador/simulador.py --cenario demo
	```

	Ele imprime os pacotes de cada casa: `TELEMETRIA`, `MOVIMENTO` e botões.

3. Declare o dia crítico, como o job de ML faz às 05h:

	```bash
	curl -s -X PUT "http://localhost:5080/api/dias-criticos/$(date +%F)" -H "Content-Type: application/json" \
	  -d '{"probabilidade":0.11,"motivo":"Noite quente","origem":"MANUAL"}'
	```

4. Depois de uns 30 segundos, veja o radar da ACS:

	```bash
	curl -s -H "Authorization: Bearer dev-acs-demo-01-so-no-notebook" "http://localhost:5080/api/radar?microarea=3"
	```

	Em `pendentes` aparecem as casas acamadas e a casa com calor em `Vermelho`, e os aparelhos mudos em `Cinza`. A contagem de `verdes` sobe conforme os movimentos chegam. Nenhuma casa fica verde sem sinal de vida.

5. Abra o app da ACS no navegador, ligado à mesma API:

	```bash
	bash tools/demo.sh app
	```

	Abra `http://localhost:8081`, entre com o código `dev-acs-demo-01-so-no-notebook` e a microárea 3.

6. Pare tudo:

	```bash
	bash tools/demo.sh parar --tudo
	```

`npm run demo` roda todas as cenas sozinho, em cerca de 10 minutos: dia crítico, aparelho mudo, mensagem bloqueada, queda de energia, desfechos da visita e reenvio do buffer. O que se viu em cada cena está no [relatório da integração](docs/evidencias/integracao/RELATORIO.md).

### Comandos da raiz

Os comandos ficam em `package.json`:

```bash
npm run demo        # sobe banco, API, carga e simulador e roda todas as cenas
npm run check       # roda as verificações de todas as partes
npm run test:app    # roda os testes do app
npm run dev:deps    # só sobe o Postgres e o Mosquitto
```

## Integrantes

| Nome | RM |
|---|---|
| Bruno Dominicheli | 554981 |
| Miguel Kapicius | 556198 |
| Thiago Ferreira | 555608 |

A mesma lista está em `integrantes.txt`.
