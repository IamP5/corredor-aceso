#!/usr/bin/env python3
"""Job das 05h: decide se hoje é dia crítico e, se for, chama PUT /api/dias-criticos/{hoje}.

Lê modelo/metadados.json, que o notebook grava. Se o modelo venceu a regra simples no teste,
a origem é MODELO e a probabilidade sai do XGBoost salvo em modelo/dia_critico_xgb.json.
Se não venceu, a origem é REGRA_BASE e a decisão é a regra da noite quente.

As variáveis do dia saem de dados/processados/clima_diario.csv. A rota só aceita a data de
hoje, então na demo o script usa as variáveis de um dia histórico e envia a data de hoje.

Uso:
  python prever_hoje.py                                   # variáveis e data de hoje
  python prever_hoje.py --dia-das-variaveis 2023-11-14    # demo: variáveis de um dia histórico
  python prever_hoje.py --dia-das-variaveis 2023-11-14 --seco   # mostra o corpo e não envia

Ambiente: CORREDOR_API_URL (padrão http://127.0.0.1:5000) e CORREDOR_DIA_DAS_VARIAVEIS
(mesmo efeito de --dia-das-variaveis, para o timer da VM rodar a demo sem mudar o serviço).
Saída: 0 quando enviou ou quando o dia não é crítico; 1 em erro da API; 2 sem variáveis do dia.
"""
import argparse
import csv
import datetime as dt
import json
import os
import pathlib
import sys
import urllib.error
import urllib.request
from zoneinfo import ZoneInfo

AQUI = pathlib.Path(__file__).resolve().parent
METADADOS = AQUI / "modelo" / "metadados.json"
MODELO = AQUI / "modelo" / "dia_critico_xgb.json"
CLIMA = AQUI / "dados" / "processados" / "clima_diario.csv"
FUSO = ZoneInfo("America/Sao_Paulo")


def ler_variaveis(dia: dt.date) -> dict:
    with CLIMA.open(encoding="utf-8") as f:
        for linha in csv.DictReader(f):
            if linha["data"] == dia.isoformat():
                return {k: (float(v) if v != "" else None) for k, v in linha.items() if k != "data"}
    raise LookupError(dia)


def num(x: float | None) -> str:
    return "sem dado" if x is None else f"{x:.1f}".replace(".", ",")


def prever(meta: dict, v: dict, dia: dt.date) -> tuple[float, bool, str]:
    regra = meta["regra_base"]
    limite = regra["limite_tmin"]
    noites = [v["tmin_noite_anterior"], v["tmin_noite"]]
    disparou = all(t is not None and t > limite for t in noites)
    clima = f"mínimas de {num(noites[0])} e {num(noites[1])} °C nas 2 últimas noites"

    if meta["origem"] == "REGRA_BASE":
        p = regra["prob_excesso_se_dispara"] if disparou else regra["prob_excesso_se_nao_dispara"]
        quadro = "acima" if disparou else "não ambas acima"
        return p, disparou, f"regra da noite quente: {clima}, {quadro} de {num(limite)} °C"

    import numpy as np
    import xgboost as xgb

    calendario = {"mes": dia.month, "dia_da_semana": dia.weekday()}
    linha = [v.get(c, calendario.get(c)) for c in meta["variaveis"]]
    x = np.array([[np.nan if c is None else c for c in linha]], dtype=float)
    booster = xgb.Booster()
    booster.load_model(MODELO)
    p = float(booster.predict(xgb.DMatrix(x, feature_names=meta["variaveis"]))[0])
    limiar = meta["limiar_decisao"]
    return p, p >= limiar, f"modelo XGBoost: {clima}, chance {p:.0%} contra limiar {limiar:.0%}"


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--dia-das-variaveis", type=dt.date.fromisoformat,
                   default=os.environ.get("CORREDOR_DIA_DAS_VARIAVEIS") or None,
                   help="dia histórico cujas variáveis de clima serão usadas (demo); "
                        "padrão: CORREDOR_DIA_DAS_VARIAVEIS")
    p.add_argument("--seco", action="store_true", help="mostra o corpo e não chama a API")
    a = p.parse_args()

    hoje = dt.datetime.now(FUSO).date()
    dia_var = a.dia_das_variaveis or hoje
    meta = json.loads(METADADOS.read_text(encoding="utf-8"))
    try:
        v = ler_variaveis(dia_var)
    except LookupError:
        print(f"sem variáveis de clima para {dia_var} em {CLIMA.relative_to(AQUI)}. "
              "O INMET publica o ano corrente com atraso: rode dados/baixar_dados.py e dados/processar.py, "
              "ou use --dia-das-variaveis com um dia histórico.", file=sys.stderr)
        return 2

    prob, critico, motivo = prever(meta, v, dia_var)
    if a.dia_das_variaveis:
        motivo = f"demo com o clima de {dia_var.strftime('%d/%m/%Y')}; {motivo}"
    corpo = {"probabilidade": round(min(max(prob, 0.0), 1.0), 4), "motivo": motivo[:200], "origem": meta["origem"]}
    print(json.dumps({"dia_das_variaveis": dia_var.isoformat(), "critico": critico, **corpo}, ensure_ascii=False))

    if not critico:
        print("dia normal: o turno do radar não abre")
        return 0
    if a.seco:
        return 0

    base = os.environ.get("CORREDOR_API_URL", "http://127.0.0.1:5000").rstrip("/")
    url = f"{base}/api/dias-criticos/{hoje.isoformat()}"
    req = urllib.request.Request(url, data=json.dumps(corpo).encode(), method="PUT",
                                 headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            print(f"PUT {url} -> {r.status} {r.read().decode()}")
    except urllib.error.HTTPError as e:
        print(f"PUT {url} -> {e.code} {e.read().decode()}", file=sys.stderr)
        return 1
    except urllib.error.URLError as e:
        print(f"PUT {url} falhou: {e.reason}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
