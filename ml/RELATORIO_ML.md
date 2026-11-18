# Relatório de Machine Learning: o dia crítico do Corredor Aceso

Global Solution 2026, CitizenTech. Disciplina Artificial Intelligence and Machine Learning.
Notebook executado: `ml/corredor_aceso.ipynb`. Todos os números deste relatório saem dele e de `ml/modelo/metadados.json`.

## 1. Problema

O Corredor Aceso põe um nó com sensores na casa de idosos que moram sozinhos. Num dia crítico, a agente comunitária de saúde (ACS) recebe às 05h um radar com as casas que ficaram em silêncio. O modelo decide se o dia é crítico. Se for, o job `prever_hoje.py` chama `PUT /api/dias-criticos/{hoje}` e o turno do radar abre.

**Pergunta:** com o que se sabe às 05h, hoje será um dia de excesso de internações de urgência por causa cardiovascular de residentes de São Paulo com 60 anos ou mais?

**Alvo:** a contagem diária dessas internações é dividida pelo fator do dia da semana (medido em 2015 a 2023). O dia é de **excesso** quando essa contagem ajustada passa do percentil 90 dos 28 dias anteriores. A régua móvel absorve a sazonalidade lenta, porque a internação cardiovascular de idosos em São Paulo tem pico no inverno. Na base, 572 de 4.141 dias (13,8%) são de excesso.

## 2. Dados reais

| Fonte | Uso | Período |
|---|---|---|
| INMET, estação automática A701, Mirante de Santana, ZIP anual em `portal.inmet.gov.br/uploads/dadoshistoricos/AAAA.zip` | temperatura e umidade de hora em hora | 2014 a out/2026 |
| SIH/SUS, AIH Reduzida de SP, `ftp.datasus.gov.br/dissemin/publicos/SIHSUS/200801_/Dados/RDSPaamm.dbc` | internações | 141 competências, jan/2015 a set/2026 |

O pipeline (`dados/baixar_dados.py` e `dados/processar.py`) baixou todos os arquivos das fontes originais e trata as armadilhas: hora em UTC no INMET, vírgula decimal e latin1, município com 6 dígitos (`355030`), arquivo por competência de pagamento (empilhado e agrupado por `DT_INTER`, com a AIH repetida contada uma vez) e meses recentes incompletos. Com 4 competências de atraso chega 99,8% das internações de um mês, então a base vai até maio de 2026. São 317.329 internações em 4.141 dias, de 29/01/2015 a 31/05/2026. Fonte, link, licença e dicionário estão em `dados/README.md`.

## 3. Variáveis e divisão

As variáveis são só o que se conhece às 05h: mínima da noite (18h a 05h), mínima da noite anterior, máxima e umidade de ontem, noites quentes seguidas (acima do P90 do mês, medido só no treino), mês e dia da semana. A internação de ontem foi excluída deliberadamente, porque o SIH só a publica meses depois e usá-la seria vazamento.

A divisão é no tempo. **Treino:** 29/01/2015 a 31/12/2023, 3.259 dias, 14,0% de excesso. **Teste:** 01/01/2024 a 31/05/2026, 882 dias, 13,0% de excesso. Nenhuma data se repete.

## 4. Baseline e modelo

**Regra da noite quente (baseline obrigatória):** dia crítico quando a mínima passa de X °C em 2 noites seguidas. X = **19,9 °C**, escolhido só no treino pelo melhor F1, com o teto de abrir o radar em no máximo 15% dos dias. O mesmo teto vale para o modelo, porque o produto é um radar que só liga quando precisa.

**XGBoost:** a base é tabular e pequena, com relação não linear e interação entre variáveis (noite quente pesa diferente em janeiro e em julho). O XGBoost lida com isso sem escalar variável, aceita dado ausente e trata classe rara com `scale_pos_weight` (6,13). Está na lista de modelos permitidos.

**Grade:** 24 combinações de 3 hiperparâmetros: `max_depth` {3, 4, 5, 6} x `learning_rate` {0,03; 0,1; 0,3} x `n_estimators` {100, 300}. Validação cruzada `TimeSeriesSplit(n_splits=5, gap=3)` só nos anos de treino, escolhendo pela PR-AUC. A melhor foi `max_depth=3`, `learning_rate=0,03`, `n_estimators=100`, com PR-AUC de validação 0,175 (desvio 0,018). O limiar de decisão (0,558) saiu das previsões fora da dobra, nunca do teste.

## 5. Resultados em treino e teste

| | Recall | Precisão | F1 | PR-AUC | ROC-AUC | Dias com radar |
|---|---|---|---|---|---|---|
| XGBoost, treino | 0,274 | 0,338 | 0,302 | 0,307 | 0,718 | 11,4% |
| XGBoost, teste | 0,122 | 0,140 | 0,130 | 0,150 | 0,549 | 11,3% |
| Regra, treino | 0,098 | 0,110 | 0,104 | 0,124 | 0,456 | 12,6% |
| Regra, teste | 0,087 | 0,060 | 0,071 | 0,116 | 0,441 | 19,0% |

O acaso no teste é a prevalência, 0,130 de PR-AUC. A PR-AUC da regra usa a versão contínua dela (a menor das duas mínimas).

**A noite quente não antecipa excesso.** Nos dias em que a regra disparou no teste, a chance de excesso foi de 6,0% (168 dias), contra 14,7% nos outros dias, uma razão de 0,40. No treino a razão foi 0,76. A regra fica abaixo do acaso (ROC-AUC 0,441).

**Estação quente (outubro a março), no teste, com 11,9% de excesso:**

| | Recall | Precisão | PR-AUC | Dias com radar |
|---|---|---|---|---|
| Modelo do ano todo | 0,037 | 0,182 | 0,146 | 2,4% |
| Modelo treinado só no verão | 0,148 | 0,094 | 0,105 | 18,7% |
| Regra | 0,185 | 0,070 | 0,110 | 31,2% |

No calor, nada passa com folga do acaso.

## 6. Overfitting

- **A grade empata na validação.** As 24 combinações ficam entre 0,149 e 0,175 de PR-AUC, com desvio de 0,01 a 0,02 entre dobras. A ordem do ranking é quase ruído, e a busca escolheu a mais simples.
- **Complexidade decora.** Nas dobras de treino, a PR-AUC vai de 0,438 na combinação escolhida a 1,000 nas mais complexas, enquanto a validação cai para cerca de 0,15.
- **O modelo escolhido cai no teste:** PR-AUC 0,307 no treino contra 0,150 no teste, e recall 0,274 contra 0,122. Sem sinal forte, o que o modelo captura no treino é coincidência daqueles anos.
- **O modelo só de verão é o caso extremo:** a validação escolheu a combinação mais complexa, que acerta 100% no treino (PR-AUC 1,000) e cai para 0,105 no teste, abaixo do acaso.
- **Não há vazamento.** Com o alvo embaralhado, a validação dá 0,154, quase o acaso (0,140). Com o alvo real, 0,175. Um vazamento produziria um resultado bom demais, e o observado é o contrário.

## 7. Decisão

O critério da descoberta era: o modelo vai para produção se, no teste, tiver recall maior ou igual ao da regra e PR-AUC maior. Pelas médias, ele passa (0,122 contra 0,087; 0,150 contra 0,116). Duas checagens mudaram a leitura:

1. **A vantagem cabe no ruído.** Reamostrando o teste em blocos de 28 dias, 2.000 vezes, a diferença de PR-AUC contra a regra é +0,034, com intervalo de 95% de -0,017 a +0,080. Contra o acaso, +0,019, de -0,001 a +0,055.
2. **O modelo acerta pelo motivo errado.** Ele abre o radar em 30%, 28%, 37% e 26% dos dias de maio a agosto, e quase nunca no verão. Aprendeu um efeito da própria régua: no outono a internação sobe mês a mês, a média das 4 semanas anteriores fica para trás e sobra mais excesso no inverno. Não é sinal de calor.

O resultado é tratado como **empate**, e o job usa a regra: `origem = REGRA_BASE` em `modelo/metadados.json`. O filtro do intervalo de confiança foi escrito depois de ver o primeiro resultado do teste, e o notebook registra isso. Esse filtro só torna a decisão mais conservadora, porque nunca promove o modelo. A probabilidade que o job envia é a chance de excesso medida no treino quando a regra dispara (0,110).

## 8. Limites

- O alvo mede internação de urgência cardiovascular pelo SUS. Calor também adoece por desidratação, queda e piora que não viram essa internação, e E86 e T67 quase não aparecem como diagnóstico principal.
- Uma estação só representa a cidade inteira, e a internação particular fica de fora.
- A régua dos 28 dias anteriores fica para trás quando a série sobe. Uma régua centrada ou um modelo de contagem com sazonalidade explícita separaria melhor pico de tendência.
- Feriados e a pandemia (2020 e 2021) não foram tratados à parte.
- A anomalia da noite (mínima contra a média da semana anterior) teve o único gradiente visível no treino: 10,7% de excesso no decil mais frio e 18,0% no mais quente. Ela foi vista depois do teste e por isso não entrou no modelo. O teste justo dela é com os meses que o SIH ainda vai publicar.

## 9. Conclusão para o cidadão

**Em São Paulo, a noite quente não avisa, sozinha, que mais idosos vão ser internados por problema do coração.** Com 11 anos de dados reais, nem a regra simples nem o XGBoost previram bem os dias de excesso. O XGBoost ficou um pouco acima do acaso, dentro do ruído, e aprendeu o inverno, não o calor.

Para o Corredor Aceso, o resultado tem três consequências. O job das 05h usa a regra da noite quente, que é simples de explicar à ACS e liga o radar nos dias de calor, quando quem mora sozinho corre risco direto. O produto **não promete** reduzir internação cardiovascular, e a frase para a ACS é "a noite não esfriou; passe nas casas que ficaram em silêncio". Por fim, o resultado negativo também protege o cidadão, porque evita que a prefeitura confie num alerta que, medido com dado real, não se sustenta.
