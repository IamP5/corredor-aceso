# QA do Corredor Aceso

Entregável de Testing, Compliance e Quality Assurance. A nota é 50% Features e 50% Test Cases, os dois cadastrados no Azure Boards ([entregaveis.md](../entregaveis.md)).

| Arquivo | O que é |
|---|---|
| [features.md](features.md) | As Features, com descrição e critérios de aceite na forma dado, quando, então |
| [casos-de-teste.md](casos-de-teste.md) | Os Test Cases funcionais de caixa preta, positivos e negativos, cada um com Feature, objetivo, preparação, entradas, saídas esperadas e passos numerados |
| [rastreabilidade.md](rastreabilidade.md) | Feature, Test Case, regra ou rota coberta e teste automatizado, nos dois sentidos |
| [contradicoes.md](contradicoes.md) | Pontos em que os documentos de arquitetura se contradizem, achados ao escrever os casos |
| `azure/features.csv` | Importação das Features no Azure Boards, processo Scrum |
| `azure/features-agile.csv` | A mesma importação para projeto no processo Agile, com os critérios dentro da descrição |
| `azure/casos-de-teste.csv` | Importação de todos os Test Cases no Azure Test Plans |
| `azure/por-feature/Fnn.csv` | Os Test Cases de uma Feature, para importar na suíte baseada em requisito dela |
| `fonte/` | A fonte de tudo acima. É o único lugar editado à mão. |
| `ferramentas/` | `registro.py`, `gerar.py` e `conferir.py` |

## Como a cobertura é provada

`ferramentas/registro.py` lê `docs/arquitetura/` e monta a lista do que precisa de teste, sem nada digitado de memória:

- as 16 rotas da seção 6.3 do modelo de domínio;
- as regras de contato R1 a R4 (5.4), o sinal de vida por perfil (5.3), as linhas das tabelas verdade da situação (5.6) e do atendimento (5.7);
- as invariantes de cada agregado (4.2 a 4.4), a tabela de desfechos, a política de cada canal e as três cores do farol (4.3 e 4.4);
- as chaves de idempotência (4.5) e as dez regras que nunca são cortadas (8.3);
- as políticas P1 a P22 do event storming;
- a fila de desfechos e o cache do app (04-modelo-de-dados.md, 6.2 e 6.3);
- os cenários do teste de falha e os itens da rubrica de Operating Systems (05-infra-e-entrega.md, 4.5 e 7);
- os 42 casos da seção 7 do modelo de domínio.

`ferramentas/conferir.py` lê os documentos gerados, não a fonte, e falha se:

- uma regra ou rota não tem Feature, caso positivo e caso negativo;
- uma linha das tabelas verdade não tem Feature e Test Case (a linha é um fato dentro de uma regra; a regra em si é que tem positivo e negativo);
- um caso da seção 7 não é citado por um Test Case;
- uma Feature não tem descrição ou critério na forma dado, quando, então;
- um Test Case não tem um dos campos, ou cita uma regra que não existe ou que a Feature dele não lista;
- um CSV quebra uma regra de importação (lista abaixo);
- um arquivo de `docs/qa/` ou `docs/entrega/` tem CPF fora dos dois de exemplo, telefone com DDD real, e-mail fora de domínio de exemplo ou caminho de máquina local.

Positivo quer dizer uso válido com o resultado esperado de sucesso. Negativo quer dizer entrada inválida, abuso, falha ou condição em que o sistema precisa recusar ou precisa **não** produzir o resultado indesejado. Os negativos que a arquitetura exige estão todos lá: silêncio nunca vira verde, casa cinza não vira verde sem pacote novo, família sem autorização não é avisada, mensagem com "mora sozinho" e remédio é bloqueada no grupo do bairro, evento reenviado não duplica, vizinho não resolve alerta de energia.

## Como editar

1. Edite `fonte/features.py` ou o arquivo `fonte/casos_*.py` do grupo. Cada caso é um `dict` com `id` (`TC-Fnn-mm`), `feature`, `tipo` (`Positivo` ou `Negativo`), `titulo`, `objetivo`, `preparacao`, `entradas`, `saidas`, `passos` (lista de pares ação e resultado esperado), `cobre` (identificadores do registro), `modelo` (números da seção 7, opcional) e `automatizado` (caminho do teste, opcional).
2. Gere e confira:

```bash
python3 docs/qa/ferramentas/gerar.py
```

```bash
python3 docs/qa/ferramentas/conferir.py
```

3. Para ver o registro com o texto de cada item: `python3 docs/qa/ferramentas/registro.py -v`.

O status "existe" ou "proposto" da coluna de teste automatizado muda sozinho na próxima geração, quando o arquivo do teste chegar ao repositório.

## Importar no Azure

### 1. Criar o projeto

Crie o projeto com o processo **Scrum**. No Scrum, a Feature tem os campos Description e Acceptance Criteria, e o `features.csv` preenche os dois. Se o projeto já existe com o processo Agile, use `features-agile.csv`, que põe os critérios dentro da descrição. Quem importa precisa ser Project Administrator, e quem cria Test Plans precisa do acesso Basic + Test Plans.

### 2. Features no Azure Boards

1. Boards, Queries, **Import work items**.
2. Escolha `docs/qa/azure/features.csv` e clique em Import.
3. Confira a lista: 1 épico e as Features como filhas dele. Clique em **Save items**.

O arquivo segue a página da Microsoft ["Import or update work items in bulk by using CSV files"](https://learn.microsoft.com/en-us/azure/devops/boards/queries/import-work-items-from-csv?view=azure-devops): tem `Work Item Type` e `Title`, não tem `ID`, `State` nem `Assigned To`, a hierarquia vai pelas colunas `Title 1` e `Title 2`, o texto rico vai em HTML com `<p>` no lugar de quebra de linha, e o arquivo tem bem menos que 1.000 itens. A importação de Boards não aceita Test Case; por isso os casos vão pelo Test Plans.

### 3. Test Cases no Azure Test Plans

O arquivo do repositório tem o caminho de área `CorredorAceso` e o responsável `Responsavel QA <qa@exemplo.com>`, que não existem no seu projeto. A importação exige um caminho de área que exista e um usuário válido. Gere os arquivos com os valores reais numa pasta fora do Git, porque o e-mail real não pode entrar no repositório público:

```bash
python3 docs/qa/ferramentas/gerar.py --area "NomeDoProjeto" --responsavel "Nome <email-da-pessoa>" --saida ../azure-importacao
```

Depois:

1. Test Plans, **New Test Plan**, com o nome "Corredor Aceso 19/11".
2. Para cada Feature, crie uma suíte **baseada em requisito** escolhendo a Feature. É o único tipo de suíte que liga o Test Case ao requisito.
3. Em cada suíte, **Import test cases from CSV/XLSX** com o arquivo `por-feature/Fnn.csv` da Feature. Confira o mapeamento: as nove colunas obrigatórias mapeiam sozinhas; mapeie `Description` para Description e `Tags` para Tags.
4. Se preferir uma suíte só, importe `casos-de-teste.csv` numa suíte estática. A ligação com a Feature fica só na tag `Fnn`.

O arquivo segue a página da Microsoft ["Bulk import or export test cases"](https://learn.microsoft.com/en-us/azure/devops/test/bulk-import-export-test-cases?view=azure-devops) e o FAQ do Test Plans: as nove colunas obrigatórias com a grafia exata (`ID`, `Work Item Type`, `Title`, `Test Step`, `Step Action`, `Step Expected`, `Area Path`, `Assigned To`, `State`), `ID` vazio para caso novo, `Work Item Type` igual a `Test Case`, `State` igual a `Design`, um passo por linha com o número do passo crescendo e os outros campos repetidos, UTF-8, aspas em toda célula, título com até 128 caracteres e arquivo abaixo de 20 MB.

## O que foi conferido sem conta no Azure

Conferido por `conferir.py`, a cada geração:

- Os dois CSV de Features abrem como CSV UTF-8, têm as colunas exigidas, não têm as proibidas para item novo, têm uma coluna `Title n` preenchida por linha, todas as Features abaixo do épico, títulos com até 255 caracteres e HTML bem formado em Description e Acceptance Criteria.
- Os CSV de Test Cases têm as nove colunas obrigatórias com a grafia da documentação, `ID` vazio, `Test Case`, `Design`, número de passo crescendo de 1 em 1, os mesmos campos em todas as linhas de um caso, títulos únicos com até 128 caracteres, HTML bem formado na descrição, menos de 20 MB e o mesmo número de casos e de passos que `casos-de-teste.md`.

Não conferido, porque exige uma organização no Azure DevOps:

- Se o projeto da equipe aceita o épico e a hierarquia como estão. Depende do processo escolhido.
- Se a Feature do processo Agile tem o campo Acceptance Criteria. A documentação da Microsoft lista o campo para "Bug, Epic, Feature, Product backlog item (Scrum)". Por isso a recomendação é o Scrum, e o `features-agile.csv` existe.
- Se as colunas opcionais `Description` e `Tags` mapeiam sozinhas no assistente do Test Plans. A documentação diz que o assistente mapeia colunas a campos e permite mapear à mão. Se alguma não mapear, pule a coluna: os passos e o título continuam completos.
- Se a importação numa suíte baseada em requisito cria o link "Tested By" com a Feature. A documentação diz que esse tipo de suíte é o que liga o caso ao requisito, mas não fala da importação por CSV. Se o link não aparecer, adicione os casos à suíte pelo botão "Add existing test cases", que cria o link.
- O limite de tamanho do texto em `Step Action` e `Step Expected`.

O primeiro teste real é importar `por-feature/F01.csv` numa suíte e abrir um caso antes de importar o resto.
