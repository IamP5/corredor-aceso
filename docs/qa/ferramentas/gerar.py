"""Gera os documentos de QA e os arquivos de importação do Azure a partir de docs/qa/fonte/.

A fonte é a única coisa editada à mão. Depois de editar, rode:

    python3 docs/qa/ferramentas/gerar.py
    python3 docs/qa/ferramentas/conferir.py

Para importar no Azure com o caminho de área e o responsável reais, gere numa pasta fora do Git:

    python3 docs/qa/ferramentas/gerar.py --area "NomeDoProjeto" --responsavel "Nome <email>" --saida /tmp/azure

O e-mail real nunca entra no repositório, porque ele é público.
"""
from __future__ import annotations

import argparse
import csv
import html
import importlib.util
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
QA = RAIZ / "docs" / "qa"
FONTE = QA / "fonte"

AREA_PADRAO = "CorredorAceso"
RESPONSAVEL_PADRAO = "Responsavel QA <qa@exemplo.com>"
ESTADO_NOVO = "Design"
EPICO = "Corredor Aceso: radar de silêncio para a ACS em dia crítico"

CABECALHO_TP = ["ID", "Work Item Type", "Title", "Test Step", "Step Action", "Step Expected",
                "Area Path", "Assigned To", "State", "Description", "Tags"]


def _carregar(nome: str):
    spec = importlib.util.spec_from_file_location(nome, FONTE / f"{nome}.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def carregar_fonte():
    features = _carregar("features").FEATURES
    casos = []
    for arq in sorted(FONTE.glob("casos_*.py")):
        casos.extend(_carregar(arq.stem).CASOS)
    ordem = {f["id"]: i for i, f in enumerate(features)}
    casos.sort(key=lambda c: (ordem.get(c["feature"], 999), c["id"]))
    return features, casos


def _h(texto: str) -> str:
    return html.escape(texto, quote=False)


def _md_lista(itens) -> str:
    return "\n".join(f"- {i}" for i in itens)


def _cobre(ids) -> str:
    return ", ".join(ids)


def _auto(caso) -> tuple[str, str]:
    alvo = caso.get("automatizado", "")
    if not alvo:
        return "", ""
    arquivo = alvo.split("::")[0]
    return alvo, ("existe" if (RAIZ / arquivo).exists() else "proposto")


# ---------- Markdown ----------

def md_features(features, casos) -> str:
    por_feature = {}
    for c in casos:
        por_feature.setdefault(c["feature"], []).append(c)
    out = [
        "# Features do Corredor Aceso",
        "",
        "Cada Feature corresponde a um work item do tipo Feature no Azure Boards, filho do épico "
        f"\"{EPICO}\". O procedimento de importação está em [README.md](README.md). Os critérios de aceite seguem a forma dado, quando, então. "
        "A linha **Cobre** lista as regras e as rotas de `docs/arquitetura/` que a Feature cobre, com os identificadores explicados na "
        "[rastreabilidade](rastreabilidade.md).",
        "",
        "| Feature | Módulo | Test Cases |",
        "|---|---|---|",
    ]
    for f in features:
        n = len(por_feature.get(f["id"], []))
        out.append(f"| [{f['id']}](#{f['id'].lower()}) {f['titulo']} | {f['modulo']} | {n} |")
    out.append("")
    for f in features:
        out += [
            f"## {f['id']}",
            "",
            f"**{f['titulo']}**",
            "",
            f"**Módulo:** {f['modulo']}",
            "",
            f"**Descrição:** {f['descricao']}",
            "",
            "**Critérios de aceite:**",
            "",
        ]
        for i, (dado, quando, entao) in enumerate(f["criterios"], 1):
            out.append(f"{i}. **Dado** {dado}, **quando** {quando}, **então** {entao}.")
        out += ["", f"**Cobre:** {_cobre(f['cobre'])}", ""]
        tcs = por_feature.get(f["id"], [])
        if tcs:
            out.append("**Test Cases:** " + ", ".join(f"[{c['id']}](casos-de-teste.md#{c['id'].lower()}) ({c['tipo'].lower()})" for c in tcs))
            out.append("")
    return "\n".join(out)


def md_casos(features, casos) -> str:
    titulos = {f["id"]: f["titulo"] for f in features}
    pos = sum(1 for c in casos if c["tipo"] == "Positivo")
    out = [
        "# Casos de teste do Corredor Aceso",
        "",
        "Arquivo gerado por `docs/qa/ferramentas/gerar.py` a partir de `docs/qa/fonte/casos_*.py`. Edite a fonte, não este arquivo.",
        "",
        f"São {len(casos)} casos funcionais de caixa preta: {pos} positivos e {len(casos) - pos} negativos. "
        "Cada caso nomeia a Feature que verifica, o objetivo, a preparação, as entradas, as saídas esperadas e os passos numerados. "
        "A linha **Caso do modelo** aponta o número do caso na seção 7 de `docs/arquitetura/03-modelo-de-dominio.md` quando o caso parte de lá.",
        "",
        "Convenções que valem para todos os casos:",
        "",
        "- Todo dado de pessoa é simulado. Os nomes, endereços e telefones vêm da carga da demo (`api/semente/casas.json`), com DDD 00.",
        "- \"Dia crítico de 18/11\" quer dizer `PUT /api/dias-criticos/2026-11-18` às 05h00, com o relógio de demo movido por `PUT /api/demo/relogio`.",
        "- As rotas ficam sob `/api` e exigem a ACS `acs-demo-01` autenticada na microárea 3, menos `GET /api/painel` e as rotas que o caso diz.",
        "- Um pacote MQTT é publicado em `corredor/v1/{casaToken}/evento` no Mosquitto local, com o formato da seção 6.2 do modelo de domínio.",
        "- Os horários são de São Paulo (-03:00), salvo quando o caso diz UTC. Com o relógio de demo movido, todo horário citado é o que o Motor enxerga, já no relógio de demo. "
        "Quem publica e quem registra manda a hora real: o servidor soma o deslocamento ao `ts` e à `ocorridaEm` (seção 5.9 do modelo de domínio). "
        "Para um evento \"às 07h00\", o simulador (`iot/simulador`) publica com o `ts` real que vira 07h00; para uma visita \"às 10h30\", registre-a quando o relógio de demo marca 10h30.",
        "- Os parâmetros são os valores de demo da seção 5.9 do modelo de domínio (silêncio máximo de 1 minuto, calor por 2 minutos), "
        "salvo quando o caso diz \"produção\". Os casos escritos em horas de produção valem igual com o relógio de demo.",
        "",
    ]
    atual = None
    for c in casos:
        if c["feature"] != atual:
            atual = c["feature"]
            out += [f"## {atual} {titulos[atual]}", ""]
        auto, status = _auto(c)
        out += [
            f"### {c['id']}",
            "",
            f"**{c['titulo']}**",
            "",
            f"**Feature:** {c['feature']}",
            "",
            f"**Tipo:** {c['tipo']}",
            "",
            f"**Objetivo:** {c['objetivo']}",
            "",
            "**Preparação:**",
            "",
            _md_lista(c["preparacao"]),
            "",
            "**Entradas:**",
            "",
            _md_lista(c["entradas"]),
            "",
            "**Saídas esperadas:**",
            "",
            _md_lista(c["saidas"]),
            "",
            "**Passos:**",
            "",
            "| # | Ação | Resultado esperado |",
            "|---|---|---|",
        ]
        for i, (acao, esperado) in enumerate(c["passos"], 1):
            out.append(f"| {i} | {acao.replace('|', '/')} | {esperado.replace('|', '/')} |")
        out += ["", f"**Cobre:** {_cobre(c['cobre'])}", ""]
        if c.get("modelo"):
            out += ["**Caso do modelo:** " + ", ".join(f"CT-{n}" for n in c["modelo"]), ""]
        if auto:
            out += [f"**Teste automatizado:** `{auto}` ({status})", ""]
    return "\n".join(out)


def md_rastreabilidade(features, casos, registro) -> str:
    out = [
        "# Rastreabilidade",
        "",
        "A matriz liga cada Feature aos seus Test Cases, às regras e rotas que eles cobrem e ao teste automatizado correspondente. "
        "O script `docs/qa/ferramentas/conferir.py` lê esta matriz, as Features e os Test Cases e aponta qualquer regra ou rota sem cobertura.",
        "",
        "A coluna \"Teste automatizado\" indica o teste do repositório que cobre o caso. \"existe\" significa que o arquivo do teste está no "
        "repositório. \"proposto\" significa que o teste ainda não foi escrito e que o nome registrado é o que o caso especifica para o módulo. "
        "\"manual\" significa que o caso não tem teste automatizado associado.",
        "",
        "## Por Feature",
        "",
        "| Feature | Test Case | Tipo | Regras e rotas | Teste automatizado |",
        "|---|---|---|---|---|",
    ]
    for f in features:
        for c in [c for c in casos if c["feature"] == f["id"]]:
            auto, status = _auto(c)
            auto_txt = f"`{auto}` ({status})" if auto else "manual"
            out.append(f"| {f['id']} | [{c['id']}](casos-de-teste.md#{c['id'].lower()}) | {c['tipo']} | {_cobre(c['cobre'])} | {auto_txt} |")
    out += [
        "",
        "## Por regra e por rota",
        "",
        "Cada linha corresponde a um item que `docs/qa/ferramentas/registro.py` extrai de `docs/arquitetura/`. "
        "Cada caso do modelo (CT) exige um Test Case. Os demais itens exigem uma Feature, um caso positivo e um caso negativo.",
        "",
        "| Item | Origem | Features | Positivos | Negativos |",
        "|---|---|---|---|---|",
    ]
    for r in registro:
        fs = [f["id"] for f in features if r.id in f["cobre"]]
        ref = f"CT-" in r.id
        if r.id.startswith("CT-"):
            n = int(r.id[3:])
            ps = [c["id"] for c in casos if n in c.get("modelo", []) and c["tipo"] == "Positivo"]
            ns = [c["id"] for c in casos if n in c.get("modelo", []) and c["tipo"] == "Negativo"]
        else:
            ps = [c["id"] for c in casos if r.id in c["cobre"] and c["tipo"] == "Positivo"]
            ns = [c["id"] for c in casos if r.id in c["cobre"] and c["tipo"] == "Negativo"]
        out.append(f"| {r.id} | {r.origem} | {', '.join(fs) or '-'} | {', '.join(ps) or '-'} | {', '.join(ns) or '-'} |")
    out.append("")
    return "\n".join(out)


# ---------- CSV ----------

def _html_feature_desc(f) -> str:
    return (f"<p>{_h(f['descricao'])}</p>"
            f"<p><strong>Módulo:</strong> {_h(f['modulo'])}</p>"
            f"<p><strong>Cobre:</strong> {_h(_cobre(f['cobre']))}</p>")


def _html_criterios(f) -> str:
    itens = "".join(f"<li><strong>Dado</strong> {_h(d)}, <strong>quando</strong> {_h(q)}, <strong>então</strong> {_h(e)}.</li>"
                    for d, q, e in f["criterios"])
    return f"<ol>{itens}</ol>"


def csv_features(features, caminho: Path, agile: bool) -> None:
    caminho.parent.mkdir(parents=True, exist_ok=True)
    with caminho.open("w", encoding="utf-8", newline="") as arq:
        w = csv.writer(arq, quoting=csv.QUOTE_ALL, lineterminator="\n")
        if agile:
            w.writerow(["Work Item Type", "Title 1", "Title 2", "Description", "Tags"])
            w.writerow(["Epic", EPICO, "", "<p>Features do Corredor Aceso, GS CitizenTech FIAP 2026.</p>", "Corredor Aceso"])
            for f in features:
                desc = _html_feature_desc(f) + "<p><strong>Critérios de aceite:</strong></p>" + _html_criterios(f)
                w.writerow(["Feature", "", f"{f['id']} {f['titulo']}", desc, f"Corredor Aceso; {f['modulo']}"])
        else:
            w.writerow(["Work Item Type", "Title 1", "Title 2", "Description", "Acceptance Criteria", "Tags"])
            w.writerow(["Epic", EPICO, "", "<p>Features do Corredor Aceso, GS CitizenTech FIAP 2026.</p>", "", "Corredor Aceso"])
            for f in features:
                w.writerow(["Feature", "", f"{f['id']} {f['titulo']}", _html_feature_desc(f), _html_criterios(f),
                            f"Corredor Aceso; {f['modulo']}"])


def _html_caso(c) -> str:
    def lista(xs):
        return "<ul>" + "".join(f"<li>{_h(x)}</li>" for x in xs) + "</ul>"
    partes = [
        f"<p><strong>Feature:</strong> {_h(c['feature'])} | <strong>Tipo:</strong> {_h(c['tipo'])}</p>",
        f"<p><strong>Objetivo:</strong> {_h(c['objetivo'])}</p>",
        f"<p><strong>Preparação:</strong></p>{lista(c['preparacao'])}",
        f"<p><strong>Entradas:</strong></p>{lista(c['entradas'])}",
        f"<p><strong>Saídas esperadas:</strong></p>{lista(c['saidas'])}",
        f"<p><strong>Cobre:</strong> {_h(_cobre(c['cobre']))}</p>",
    ]
    return "".join(partes)


def titulo_azure(c) -> str:
    return f"{c['id']} {c['titulo']}"


def csv_casos(casos, caminho: Path, area: str, responsavel: str) -> None:
    caminho.parent.mkdir(parents=True, exist_ok=True)
    with caminho.open("w", encoding="utf-8", newline="") as arq:
        w = csv.writer(arq, quoting=csv.QUOTE_ALL, lineterminator="\n")
        w.writerow(CABECALHO_TP)
        for c in casos:
            desc = _html_caso(c)
            tags = f"{c['feature']}; {c['tipo']}"
            for i, (acao, esperado) in enumerate(c["passos"], 1):
                w.writerow(["", "Test Case", titulo_azure(c), str(i), acao, esperado, area, responsavel, ESTADO_NOVO, desc, tags])


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--area", default=AREA_PADRAO)
    ap.add_argument("--responsavel", default=RESPONSAVEL_PADRAO)
    ap.add_argument("--saida", type=Path, help="pasta para os CSV com área e responsável reais (fora do Git)")
    args = ap.parse_args()

    sys.path.insert(0, str(Path(__file__).parent))
    from registro import regras

    features, casos = carregar_fonte()
    if args.saida:
        csv_casos(casos, args.saida / "casos-de-teste.csv", args.area, args.responsavel)
        for f in features:
            csv_casos([c for c in casos if c["feature"] == f["id"]], args.saida / "por-feature" / f"{f['id']}.csv",
                      args.area, args.responsavel)
        csv_features(features, args.saida / "features.csv", agile=False)
        csv_features(features, args.saida / "features-agile.csv", agile=True)
        print(f"CSV com área e responsável reais em {args.saida}")
        return 0

    (QA / "features.md").write_text(md_features(features, casos), encoding="utf-8")
    (QA / "casos-de-teste.md").write_text(md_casos(features, casos), encoding="utf-8")
    (QA / "rastreabilidade.md").write_text(md_rastreabilidade(features, casos, regras()), encoding="utf-8")
    az = QA / "azure"
    csv_features(features, az / "features.csv", agile=False)
    csv_features(features, az / "features-agile.csv", agile=True)
    csv_casos(casos, az / "casos-de-teste.csv", args.area, args.responsavel)
    for f in features:
        csv_casos([c for c in casos if c["feature"] == f["id"]], az / "por-feature" / f"{f['id']}.csv", args.area, args.responsavel)
    print(f"{len(features)} Features e {len(casos)} Test Cases gerados em docs/qa/")
    return 0


if __name__ == "__main__":
    sys.exit(main())
