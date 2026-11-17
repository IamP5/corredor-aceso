#!/usr/bin/env python3
"""Refaz ml/dados/processados/ a partir de ml/dados/brutos/.

Saídas (pequenas, entram no Git):
  clima_diario.csv         uma linha por dia, só com o que se sabe às 05h daquele dia
  internacoes_diarias.csv  internações de urgência cardiovasculares de residentes 60+ por dia
  completude_sih.csv       que fração das internações de cada mês chega até cada competência
  base_modelo.csv          clima + calendário + internações + alvo, só os dias completos

O microdado do SIH nunca sai de brutos/. Este script lê do .dbc só as colunas do filtro e
guarda em brutos/sih_filtrado/ uma cópia mínima por competência, para rodar de novo rápido.

Uso: python processar.py
"""
import datetime as dt
import pathlib
import sys
from concurrent.futures import ProcessPoolExecutor

import numpy as np
import pandas as pd

AQUI = pathlib.Path(__file__).resolve().parent
BRUTOS = AQUI / "brutos"
SAIDA = AQUI / "processados"

# Anos de treino. Tudo o que é limiar fixo (P90 mensal da mínima, fator de dia da semana)
# é calculado só com eles, para o teste nunca influenciar a régua.
ANOS_TREINO = range(2015, 2024)
# Uma internação de um mês continua chegando em competências posteriores.
# O mês só entra quando ao menos esta fração já chegou (estimada em completude_sih.csv).
COMPLETUDE_MINIMA = 0.99
JANELA_BASE = 28
PERCENTIL_EXCESSO = 0.90

COLS_SIH = ["N_AIH", "IDENT", "DT_INTER", "CAR_INT", "MUNIC_RES", "COD_IDADE", "IDADE", "DIAG_PRINC"]


# ---------------------------------------------------------------- INMET

def ler_inmet(caminho: pathlib.Path) -> pd.DataFrame:
    """Lê um CSV anual da A701: 8 linhas de metadados, ';', vírgula decimal, latin1, hora UTC."""
    df = pd.read_csv(caminho, sep=";", decimal=",", encoding="latin1", skiprows=8, na_values=["-9999"])
    df.columns = [c.strip() for c in df.columns]
    data, hora = df.columns[0], df.columns[1]  # "Data"/"Hora UTC" ou "DATA (YYYY-MM-DD)"/"HORA (UTC)"
    # Datas vêm como 2025/01/01 ou 2015-01-01; horas como "0000 UTC" ou "00:00".
    hhmm = df[hora].astype(str).str.replace(" UTC", "").str.replace(":", "").str.zfill(4)
    utc = pd.to_datetime(df[data].astype(str).str.replace("/", "-") + " " + hhmm, format="%Y-%m-%d %H%M")
    return pd.DataFrame({
        "utc": utc,
        "temp": df["TEMPERATURA DO AR - BULBO SECO, HORARIA (\u00b0C)"],
        "tmax": df["TEMPERATURA M\u00c1XIMA NA HORA ANT. (AUT) (\u00b0C)"],
        "tmin": df["TEMPERATURA M\u00cdNIMA NA HORA ANT. (AUT) (\u00b0C)"],
        "umidade": df["UMIDADE RELATIVA DO AR, HORARIA (%)"],
    })


def clima_diario() -> pd.DataFrame:
    h = pd.concat([ler_inmet(p) for p in sorted((BRUTOS / "inmet").glob("A701_*.csv"))])
    h = h.drop_duplicates("utc").set_index("utc").sort_index()
    # O valor de máxima e mínima da linha "08 UTC" vale para a hora 07-08 UTC.
    # Hora local de início do intervalo = rótulo UTC - 1 h - 3 h.
    h.index = h.index - pd.Timedelta(hours=4)
    h["tmin"] = h["tmin"].fillna(h["temp"])
    h["tmax"] = h["tmax"].fillna(h["temp"])

    # Noite que termina às 05h do dia D: intervalos locais de 18h de D-1 até 05h de D (11 horas).
    noite = h.copy()
    noite["dia"] = (noite.index + pd.Timedelta(hours=6)).normalize()
    noite = noite[(noite.index + pd.Timedelta(hours=6)).hour < 11]
    g = noite.groupby("dia")["tmin"]
    tmin_noite = g.min().where(g.count() >= 8)

    dia_local = h.groupby(h.index.normalize())
    tmax_dia = dia_local["tmax"].max().where(dia_local["tmax"].count() >= 18)
    umid_dia = dia_local["umidade"].mean().where(dia_local["umidade"].count() >= 18)

    dias = pd.date_range(tmin_noite.index.min(), tmin_noite.index.max(), freq="D")
    c = pd.DataFrame(index=dias)
    c["tmin_noite"] = tmin_noite.reindex(dias)
    c["tmin_noite_anterior"] = c["tmin_noite"].shift(1)
    c["tmax_ontem"] = tmax_dia.reindex(dias - pd.Timedelta(days=1)).to_numpy()
    c["umidade_media"] = umid_dia.reindex(dias - pd.Timedelta(days=1)).to_numpy()

    # Noite quente = mínima acima do P90 das mínimas do mesmo mês nos anos de treino.
    treino = c[c.index.year.isin(ANOS_TREINO)]
    p90_mes = treino.groupby(treino.index.month)["tmin_noite"].quantile(0.9)
    quente = (c["tmin_noite"] > c.index.month.map(p90_mes).to_numpy()).astype(int)
    c["noites_quentes_seguidas"] = quente.groupby((quente == 0).cumsum()).cumsum()
    c.index.name = "data"
    return c.round(2), p90_mes


# ---------------------------------------------------------------- SIH

def ler_dbf(caminho: pathlib.Path, colunas: list[str]) -> pd.DataFrame:
    """Leitor de DBF de largura fixa com numpy: lê só as colunas pedidas, como texto."""
    bruto = caminho.read_bytes()
    n = int.from_bytes(bruto[4:8], "little")
    inicio = int.from_bytes(bruto[8:10], "little")
    tam = int.from_bytes(bruto[10:12], "little")
    campos, pos, off = {}, 32, 1  # o 1º byte do registro é a marca de apagado
    while bruto[pos] != 0x0D:
        nome = bruto[pos:pos + 11].split(b"\0")[0].decode("latin1")
        largura = bruto[pos + 16]
        campos[nome] = (off, largura)
        off += largura
        pos += 32
    regs = np.frombuffer(bruto, dtype=np.uint8, count=n * tam, offset=inicio).reshape(n, tam)
    vivo = regs[:, 0] != ord("*")
    saida = {}
    for c in colunas:
        o, w = campos[c]
        saida[c] = pd.Series(regs[vivo, o:o + w].copy().view(f"S{w}").ravel()).str.decode("latin1").str.strip()
    return pd.DataFrame(saida)


def filtrar_competencia(dbc: pathlib.Path) -> pd.DataFrame:
    """Linhas do alvo de uma competência: urgência, residente em SP (355030), 60+, CID I00-I99, E86 ou T67."""
    import tempfile

    import datasus_dbc

    with tempfile.TemporaryDirectory() as tmp:
        dbf = pathlib.Path(tmp) / "x.dbf"
        datasus_dbc.decompress(str(dbc), str(dbf))
        df = ler_dbf(dbf, COLS_SIH)
    idade = pd.to_numeric(df["IDADE"], errors="coerce")
    cid = df["DIAG_PRINC"]
    alvo = (
        (df["MUNIC_RES"] == "355030")
        & (df["CAR_INT"] == "02")
        & (df["COD_IDADE"] == "4")
        & (idade >= 60)
        & (df["IDENT"] == "1")  # AIH principal; a 5 é continuação de longa permanência
        & (cid.str.startswith("I") | cid.str.startswith("E86") | cid.str.startswith("T67"))
    )
    out = df.loc[alvo, ["N_AIH", "DT_INTER", "DIAG_PRINC"]].copy()
    out["competencia"] = "20" + dbc.stem[4:8]
    return out


def internacoes() -> tuple[pd.DataFrame, pd.DataFrame]:
    cache = BRUTOS / "sih_filtrado"
    cache.mkdir(exist_ok=True)
    arquivos = sorted((BRUTOS / "sih").glob("RDSP*.dbc"))
    pendentes = [d for d in arquivos if not (cache / f"{d.stem}.csv").exists()
                 or (cache / f"{d.stem}.csv").stat().st_mtime < d.stat().st_mtime]
    with ProcessPoolExecutor() as ex:
        for dbc, filtrado in zip(pendentes, ex.map(filtrar_competencia, pendentes)):
            filtrado.to_csv(cache / f"{dbc.stem}.csv", index=False)
            print(f"  {dbc.name} filtrado", flush=True)
    aih = pd.concat([pd.read_csv(cache / f"{d.stem}.csv", dtype=str) for d in arquivos], ignore_index=True)
    # A mesma AIH pode ser reapresentada em outra competência: fica a primeira.
    aih = aih.sort_values("competencia").drop_duplicates("N_AIH", keep="first")
    aih["data"] = pd.to_datetime(aih["DT_INTER"], format="%Y%m%d", errors="coerce")
    aih = aih.dropna(subset=["data"])

    # Completude: para cada mês de internação, fração acumulada que já chegou k meses depois.
    aih["mes"] = aih["data"].dt.to_period("M")
    comp = pd.PeriodIndex(aih["competencia"], freq="M")
    aih["atraso"] = (comp - aih["mes"]).map(lambda x: x.n)
    ultima = comp.max()
    tab = aih[aih["atraso"] >= 0].groupby(["mes", "atraso"]).size().unstack(fill_value=0)
    acum = tab.cumsum(axis=1)
    # Fração típica que chega até o atraso k, medida só em meses com 12+ meses de folga.
    maduros = acum[acum.index <= ultima - 12]
    fracao = (maduros.div(maduros.iloc[:, -1], axis=0)).median()
    atraso_ok = int((fracao >= COMPLETUDE_MINIMA).idxmax())
    ultimo_mes_completo = ultima - atraso_ok
    completude = pd.DataFrame({"atraso_meses": fracao.index, "fracao_mediana": fracao.round(4).to_numpy()})

    primeiro = comp.min()  # meses antes da 1ª competência baixada chegaram em arquivos que não temos
    serie = aih.groupby("data").size()
    dias = pd.date_range(primeiro.start_time, ultimo_mes_completo.end_time.normalize(), freq="D")
    diaria = serie.reindex(dias, fill_value=0).rename("internacoes").to_frame()
    diaria.index.name = "data"
    print(f"  SIH: {len(aih)} AIHs no alvo; competências até {ultima}; meses completos até {ultimo_mes_completo} "
          f"(atraso de {atraso_ok} meses cobre {COMPLETUDE_MINIMA:.0%})")
    return diaria, completude


# ---------------------------------------------------------------- alvo

def base_modelo(clima: pd.DataFrame, inter: pd.DataFrame) -> pd.DataFrame:
    b = inter.join(clima, how="inner")
    b["mes"] = b.index.month
    b["dia_da_semana"] = b.index.day_of_week  # 0 = segunda
    # Fator de dia da semana medido nos anos de treino: quanto cada dia recebe em relação à média.
    treino = b[b.index.year.isin(ANOS_TREINO)]
    fator = treino.groupby("dia_da_semana")["internacoes"].mean() / treino["internacoes"].mean()
    b["internacoes_ajustadas"] = b["internacoes"] / b["dia_da_semana"].map(fator)
    # Régua móvel: P90 dos 28 dias anteriores, já ajustados por dia da semana.
    regua = b["internacoes_ajustadas"].shift(1).rolling(JANELA_BASE, min_periods=JANELA_BASE)
    b["limiar_excesso"] = regua.quantile(PERCENTIL_EXCESSO)
    b = b.dropna(subset=["limiar_excesso"])
    b["excesso"] = (b["internacoes_ajustadas"] > b["limiar_excesso"]).astype(int)
    return b.round(3)


def main() -> int:
    SAIDA.mkdir(exist_ok=True)
    print("clima...", flush=True)
    clima, p90 = clima_diario()
    clima.to_csv(SAIDA / "clima_diario.csv")
    print(f"  {len(clima)} dias, de {clima.index.min().date()} a {clima.index.max().date()}")
    print("  P90 mensal da mínima noturna (treino):", p90.round(1).to_dict())
    print("internações...", flush=True)
    inter, comp = internacoes()
    inter.to_csv(SAIDA / "internacoes_diarias.csv")
    comp.to_csv(SAIDA / "completude_sih.csv", index=False)
    b = base_modelo(clima, inter)
    b.to_csv(SAIDA / "base_modelo.csv")
    print(f"base_modelo: {len(b)} dias, de {b.index.min().date()} a {b.index.max().date()}, "
          f"{b['excesso'].mean():.1%} de dias de excesso")
    return 0


if __name__ == "__main__":
    sys.exit(main())
