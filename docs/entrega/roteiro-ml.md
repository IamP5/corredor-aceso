# Roteiro do vídeo de IA e Machine Learning (até 5 minutos)

Roteiro de gravação de uso da equipe. Saiu de `ml/README.md`.

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
