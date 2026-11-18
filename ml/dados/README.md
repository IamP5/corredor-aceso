# Dados do modelo de dia crítico

Dois conjuntos públicos e reais, baixados em 17/11/2026. Nenhum dado é simulado.

| | INMET, estação A701 | SIH/SUS, AIH Reduzida (RD) |
|---|---|---|
| O que é | Medição de hora em hora da estação automática São Paulo Mirante de Santana (lat -23,496, lon -46,620, 785 m) | Uma linha por internação paga pelo SUS no estado de São Paulo |
| Link | `https://portal.inmet.gov.br/uploads/dadoshistoricos/AAAA.zip`, índice em <https://portal.inmet.gov.br/dadoshistoricos> | `ftp://ftp.datasus.gov.br/dissemin/publicos/SIHSUS/200801_/Dados/RDSPaamm.dbc` |
| Baixado | anos de 2014 a 2026 (o ZIP de 2026 vai até 31/10/2026) | 141 competências, de RDSP1501 a RDSP2609 |
| Período usado | 01/01/2014 a 31/10/2026 | internações de 29/01/2015 a 31/05/2026 |
| Tamanho bruto | 13 CSV, cerca de 0,8 MB cada | 141 DBC, cerca de 2,3 GB no total |
| Licença | Dado público do governo federal, de uso livre com citação da fonte (Lei 12.527/2011 e Política de Dados Abertos, Decreto 8.777/2016). O portal não traz um arquivo de licença próprio. | Dado público do Ministério da Saúde, de uso livre com citação da fonte (mesma base legal). O microdado traz data de nascimento e CEP de pessoa real, então fica fora do Git. |
| Citação | Instituto Nacional de Meteorologia (INMET), Dados Históricos Anuais, estação A701 | Ministério da Saúde, DATASUS, Sistema de Informações Hospitalares do SUS (SIH/SUS) |

## Como baixar e refazer

Os arquivos brutos não entram no Git (`ml/.gitignore` tem `dados/brutos/`). Para refazer tudo a partir das fontes:

```bash
cd ml
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt
.venv/bin/python dados/baixar_dados.py      # cerca de 2,3 GB; 15 a 30 min, conforme a rede
.venv/bin/python dados/processar.py         # cerca de 1 min com 8 núcleos
```

- `baixar_dados.py` pode rodar de novo. O arquivo que já existe e bate com o `sha256` de `manifesto.csv` não é baixado outra vez. O ano corrente do INMET e as três competências mais novas do SIH são sempre baixados de novo, porque a fonte ainda acrescenta dados a eles.
- `manifesto.csv` tem, para cada arquivo bruto, o link exato, o tamanho, o `sha256` e o dia do download. O manifesto comprova a origem dos dados. O DATASUS às vezes republica uma competência. Se o `sha256` mudar, o script baixa o arquivo de novo e atualiza a linha.
- `processar.py` grava em `brutos/sih_filtrado/` só as colunas do filtro por competência, para rodar de novo em segundos. Essa pasta também fica fora do Git, porque ainda tem o número da AIH.

## Armadilhas das fontes

| Armadilha | Tratamento |
|---|---|
| O portal do INMET derruba conexão sem User-Agent de navegador | o script manda um User-Agent |
| O ZIP do ano tem todas as estações do país (80 a 100 MB) | o servidor aceita faixa de bytes, então o script lê só o índice do ZIP e o arquivo da A701 |
| CSV do INMET com `;`, vírgula decimal, latin1, 8 linhas de metadados, `-9999` como ausente | `pandas.read_csv(sep=";", decimal=",", encoding="latin1", skiprows=8, na_values=["-9999"])` |
| Cabeçalho do INMET muda em 2019 (`DATA (YYYY-MM-DD)` e `HORA (UTC)` viram `Data` e `Hora UTC`, data com `/`, hora `0000 UTC`) | data e hora lidas pela posição, nos dois formatos |
| Hora do INMET em UTC, e a máxima e a mínima de uma linha valem para a hora anterior | hora local do início do intervalo = rótulo UTC - 1 h - 3 h |
| `MUNIC_RES` com 6 dígitos | São Paulo é `355030`, não `3550308` |
| O DBC é por competência de pagamento, não por data de internação | as 141 competências são empilhadas, a `N_AIH` repetida fica uma vez só e a contagem é por `DT_INTER` |
| Os meses mais recentes ainda estão incompletos | `completude_sih.csv` mede, nos meses com 12 meses de folga, quanto chega até k competências depois. Com 4 competências chega 99,8%, então o último mês usado é maio de 2026 (a última competência é setembro de 2026) |
| AIH de longa permanência aparece de novo como continuação | só entra `IDENT = 1`, a AIH principal |

## Arquivos de `processados/` (entram no Git)

| Arquivo | Linha | Colunas |
|---|---|---|
| `clima_diario.csv` | um dia, de 01/01/2014 a 01/11/2026 | `data`; `tmin_noite`, mínima de 18h do dia anterior a 05h do dia, com ao menos 8 das 11 horas; `tmin_noite_anterior`, a mesma uma noite antes; `tmax_ontem` e `umidade_media`, máxima e umidade média do dia anterior, com ao menos 18 horas; `noites_quentes_seguidas`, noites seguidas com mínima acima do P90 do mesmo mês nos anos de 2015 a 2023 |
| `internacoes_diarias.csv` | um dia, de 01/01/2015 a 31/05/2026 | `data`; `internacoes`, AIHs com `CAR_INT = 02` (urgência), `MUNIC_RES = 355030`, `COD_IDADE = 4` e `IDADE >= 60`, `IDENT = 1` e `DIAG_PRINC` em I00-I99, E86 ou T67, pela data de internação |
| `completude_sih.csv` | um atraso em meses | `atraso_meses`; `fracao_mediana`, fração das internações de um mês que já chegou até aquela competência |
| `base_modelo.csv` | um dia, de 29/01/2015 a 31/05/2026, 4.141 dias | as colunas de clima, `mes`, `dia_da_semana` (0 é segunda), `internacoes`, `internacoes_ajustadas` (dividida pelo fator do dia da semana medido em 2015 a 2023), `limiar_excesso` (P90 das 28 internações ajustadas anteriores) e `excesso` (1 quando a ajustada passa do limiar) |

O primeiro dia da base é 29/01/2015 porque a régua precisa de 28 dias anteriores, e a primeira competência baixada é janeiro de 2015.

## O que não foi baixado

- O SIM (óbitos, `DOSPaaaa.dbc`), alternativa prevista na fase de descoberta. O alvo de internação foi suficiente.
- As competências anteriores a 2015. A série do INMET começa em 2006, mas o treino começa em 2015, como a descoberta definiu.
- O INMET de novembro de 2026. O ZIP do ano corrente ainda não traz o mês, por isso `prever_hoje.py` sem argumento não acha a linha de hoje (ver `ml/README.md`).
