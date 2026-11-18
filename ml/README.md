# ml/: o modelo de dia crítico

O job decide às 05h se hoje é dia crítico para quem tem 60 anos ou mais em São Paulo. Se for, o turno do radar da ACS abre. A decisão cabe hoje à regra da noite quente, porque o modelo não a superou (ver o resultado abaixo). Esta pasta é o entregável da disciplina de IA e Machine Learning.

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

Com 11 anos de dados reais, nem a regra da noite quente nem o XGBoost previram bem os dias de excesso de internação cardiovascular de idosos. O XGBoost ficou um pouco acima do acaso no teste (PR-AUC 0,150 contra 0,130), dentro do ruído, e aprendeu o padrão do inverno, não o do calor. Por isso o job usa a regra (`origem = REGRA_BASE`: mínima acima de 19,9 °C em 2 noites seguidas). Os números estão no relatório.

## Rodar

```bash
cd ml
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt
.venv/bin/jupyter nbconvert --to notebook --execute --inplace corredor_aceso.ipynb
```

O notebook roda sem baixar nada, a partir de `dados/processados/`. O procedimento para refazer os dados a partir da fonte está em `dados/README.md`.

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
- Com `origem = REGRA_BASE`, a probabilidade enviada é a chance de excesso medida no treino quando a regra dispara (0,1098). O valor é baixo por construção, porque o relatório mostra que a noite quente não antecipa internação.

Dias adequados para a demo, todos com a regra disparada: 29/12/2025 (mínimas de 22,6 e 24,3 °C), 25/12/2025 e 12/01/2026.
