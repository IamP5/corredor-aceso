# ml/: o modelo de dia crítico

O job decide às 05h se hoje é dia crítico para quem tem 60 anos ou mais em São Paulo. Se for, o turno do radar da ACS abre. Quem decide hoje é a regra da noite quente, porque o modelo não a superou (veja o resultado abaixo). Entregável da disciplina de IA e Machine Learning.

| Arquivo | O que é |
|---|---|
| `corredor_aceso.ipynb` | notebook executado, da pergunta à conclusão |
| `RELATORIO_ML.md` | relatório de 2 a 4 páginas |
| `dados/README.md` | fonte, link, período, licença e armadilhas de cada dado |
| `dados/baixar_dados.py`, `dados/processar.py` | baixam os dados públicos e montam `dados/processados/` |
| `dados/manifesto.csv` | link, tamanho e `sha256` de cada arquivo bruto |
| `modelo/metadados.json` | variáveis, limiar, métricas do teste, regra e a origem que o job manda |
| `modelo/dia_critico_xgb.json` | o XGBoost treinado, no formato JSON da biblioteca |
| `prever_hoje.py` | o job das 05h |
| `requirements.txt`, `requirements-vm.txt` | dependências do notebook e da VM |

## Resultado em uma frase

Com 11 anos de dados reais, nem a regra da noite quente nem o XGBoost previram bem os dias de excesso de internação cardiovascular de idosos. O XGBoost ficou um pouco acima do acaso no teste (PR-AUC 0,150 contra 0,130), dentro do ruído e aprendendo o inverno. Por isso o job usa a regra (`origem = REGRA_BASE`: mínima acima de 19,9 °C em 2 noites seguidas). Os números estão no relatório.

## Rodar

```bash
cd ml
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt
.venv/bin/jupyter nbconvert --to notebook --execute --inplace corredor_aceso.ipynb
```

O notebook roda sem baixar nada, a partir de `dados/processados/`. Para refazer os dados desde a fonte, veja `dados/README.md`.

## O job `prever_hoje.py`

Na VM, `infra/systemd/corredor-ml.timer` roda `python prever_hoje.py` às 05h, dentro de `ml/`, com `CORREDOR_API_URL=http://127.0.0.1:5000`.

```bash
python prever_hoje.py --dia-das-variaveis 2025-12-29          # demo: abre o turno de hoje com o clima de 29/12/2025
python prever_hoje.py --dia-das-variaveis 2025-12-29 --seco   # mostra o corpo e não envia
python prever_hoje.py --dia-das-variaveis 2025-07-15          # dia normal: não chama a API
```

- A rota só aceita a data de hoje. Com `--dia-das-variaveis`, o script usa o clima do dia histórico e envia a data de hoje, como o modelo de domínio decidiu.
- O corpo segue a seção 6.3 de `docs/arquitetura/03-modelo-de-dominio.md`: `{"probabilidade", "motivo", "origem"}`, com `motivo` de até 200 caracteres e `origem` igual a `MODELO` ou `REGRA_BASE`, conforme `modelo/metadados.json`.
- Em dia normal, o script não chama a API e sai com 0. Em erro da API, sai com 1, o que dispara o `OnFailure` do serviço.
- **Sem argumento, hoje, ele sai com 2.** O INMET publica o ano corrente com atraso (o ZIP de 2026 vai até 31/10), então não existe linha de clima para hoje em `dados/processados/clima_diario.csv`. Para o timer da VM rodar a demo sem mudar o serviço, defina `CORREDOR_DIA_DAS_VARIAVEIS=2025-12-29` no ambiente do serviço.
- Com `origem = REGRA_BASE`, a probabilidade enviada é a chance de excesso medida no treino quando a regra dispara (0,1098). É baixa de propósito: o relatório mostra que a noite quente não antecipa internação.

Dias bons para a demo, todos com a regra disparada: 29/12/2025 (mínimas de 22,6 e 24,3 °C), 25/12/2025 e 12/01/2026.

## Roteiro do vídeo (até 5 minutos)

Vídeo no YouTube, não listado. Uma pessoa narra e outra compartilha a tela. Abrir o notebook já executado no GitHub ou no Jupyter.

| Tempo | Tela | Fala |
|---|---|---|
| 0:00-0:30 | slide ou README do projeto | "Num dia de calor, a ACS vê no radar só as casas de idosos sozinhos que ficaram em silêncio. Quem decide se o dia é crítico é um job que roda às 5 da manhã. Testamos se um modelo de ML decidiria melhor do que uma regra simples de noite quente. Este vídeo mostra como ele foi feito e o que descobrimos." |
| 0:30-1:10 | notebook, seção 1 e 2 | "A pergunta: com o que se sabe às 5h, hoje vai ter excesso de internação de urgência cardiovascular de idosos em São Paulo? Usamos dois dados públicos e reais: o termômetro do INMET no Mirante de Santana e 141 meses de internações do SUS, 317 mil internações em 11 anos." |
| 1:10-1:50 | `dados/README.md` e o gráfico de completude | "Os dados têm armadilhas: hora em UTC, município com 6 dígitos, arquivo por mês de pagamento e meses recentes incompletos. Este gráfico mostra que só depois de 4 meses chega 99% das internações, então paramos em maio de 2026." |
| 1:50-2:20 | gráfico mensal | "Primeira surpresa: a internação do coração sobe no inverno, não no verão. Por isso o alvo compara cada dia com as 4 semanas anteriores." |
| 2:20-3:00 | seções 4 a 7 | "Só entra o que se sabe às 5h. A internação de ontem fica de fora, porque o SUS só publica meses depois. Treino de 2015 a 2023, teste de 2024 em diante. A regra simples é a noite quente. No XGBoost testamos 24 combinações de 3 hiperparâmetros, com validação cruzada no tempo só no treino." |
| 3:00-3:50 | tabela e gráficos de treino contra teste, gráfico de overfitting | "No treino o modelo parece bom; no teste cai para perto do acaso. As combinações complexas decoram o treino, chegam a 100%, e não generalizam. Com o alvo embaralhado o resultado é o mesmo do acaso, então não há vazamento." |
| 3:50-4:30 | seção 11, tabela por mês | "O XGBoost passou na regra por pouco, mas a vantagem cabe no ruído, e ele abre o radar no inverno: aprendeu a estação, não o calor. Tratamos como empate. O job usa a regra da noite quente." |
| 4:30-5:00 | terminal com `prever_hoje.py --dia-das-variaveis 2025-12-29` e o radar aberto | "Para o cidadão: em São Paulo, a noite quente sozinha não avisa que mais idosos vão ser internados. O radar continua ligando no calor, pelo risco de quem mora só, mas sem prometer o que os dados não mostram." |
