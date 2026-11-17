# Corredor Aceso

Global Solution FIAP 2026.2, CitizenTech. Engenharia de Software, 3º ano, turma de fevereiro.

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| Nome da CitizenTech | Corredor Aceso |
| Cidade e bairro | São Paulo (SP), Zona Leste. Piloto em uma microárea de UBS da Estratégia Saúde da Família, com o Jardim Helena como referência. |
| Cidadão beneficiado | Idosos de 60 anos ou mais que moram sozinhos, e casas com pessoa que depende de aparelho na tomada. Quem opera é a agente comunitária de saúde (ACS). |
| Problema, em uma frase | Em dia de calor extremo ou de apagão, a ACS não consegue checar todos os idosos sozinhos da microárea e não sabe quem está em risco. |
| Como funciona | Um aparelho no corredor acende a luz quando a idosa passa, e esse movimento vale como "estou bem". Um modelo treinado com dados públicos decide se o dia é crítico. No dia crítico, o celular da ACS mostra só as casas que ficaram em silêncio. Aparelho mudo aparece em cinza e nunca vira verde. |
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

As instruções de cada parte ficam no `README.md` da pasta dela. Os comandos da raiz estão em `package.json`:

```bash
npm run dev:deps    # sobe Postgres e Mosquitto para desenvolvimento
npm run dev:api     # roda a API
npm run check       # roda as verificações de todas as partes
npm run test:app    # roda os testes do app
npm run demo        # sobe banco, API, carga e simulador e roda as cenas da demo
```

A demo integrada, com os comandos de cada cena e as evidências, está em [docs/evidencias/integracao/RELATORIO.md](docs/evidencias/integracao/RELATORIO.md).

## Integrantes

Ver `integrantes.txt`.
