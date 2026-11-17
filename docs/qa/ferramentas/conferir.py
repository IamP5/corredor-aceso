"""Confere o entregável de QA lendo os documentos, não a fonte.

1. Toda regra e toda rota de docs/arquitetura/ (registro.py) tem uma Feature, um Test Case positivo e um negativo.
   Toda linha das tabelas verdade 5.6 e 5.7 tem uma Feature e um Test Case.
   Todo caso da seção 7 do modelo de domínio é citado por um Test Case.
2. Toda Feature tem descrição e critérios de aceite na forma dado, quando, então.
3. Todo Test Case tem Feature, tipo, objetivo, preparação, entradas, saídas esperadas e passos numerados.
4. Nenhum documento cita uma regra que não existe.
5. Os CSV seguem as regras de importação do Azure Boards e do Azure Test Plans (ver README.md).
6. Toda casa da carga citada com perfil nos casos tem esse perfil em casas.json.
7. Nenhum arquivo de docs/qa/ e docs/entrega/ tem CPF fora da lista de exemplo, telefone com DDD real,
   e-mail fora de domínio de exemplo ou caminho de máquina local.

Uso: python3 docs/qa/ferramentas/conferir.py      Sai com 1 se achar qualquer buraco.
"""
from __future__ import annotations

import csv
import re
import sys
from collections import defaultdict
from html.parser import HTMLParser
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from registro import RAIZ, regras  # noqa: E402

QA = RAIZ / "docs" / "qa"
ENTREGA = RAIZ / "docs" / "entrega"
CAMPOS_TC = ["Feature", "Tipo", "Objetivo", "Preparação", "Entradas", "Saídas esperadas", "Passos", "Cobre"]
OBRIGATORIOS_TP = ["ID", "Work Item Type", "Title", "Test Step", "Step Action", "Step Expected",
                   "Area Path", "Assigned To", "State"]
PROIBIDOS_BOARDS = {"ID", "Assigned To", "State", "Changed Date", "Created By"}
CPF_PERMITIDOS = {"123.456.789-00", "123.456.789-09"}

erros: list[str] = []


def erro(msg: str) -> None:
    erros.append(msg)


def ids_cobre(linha: str) -> list[str]:
    return [p.strip() for p in linha.split(", ") if p.strip()]


# ---------- leitura dos documentos ----------

def ler_features() -> dict[str, dict]:
    texto = (QA / "features.md").read_text(encoding="utf-8")
    feats = {}
    for bloco in re.split(r"^## ", texto, flags=re.M)[1:]:
        fid = bloco.split("\n", 1)[0].strip()
        if not re.fullmatch(r"F\d{2}", fid):
            continue
        desc = re.search(r"^\*\*Descrição:\*\* (.+)$", bloco, re.M)
        crit = re.findall(r"^\d+\. \*\*Dado\*\* .+?, \*\*quando\*\* .+?, \*\*então\*\* .+\.$", bloco, re.M)
        cobre = re.search(r"^\*\*Cobre:\*\* (.+)$", bloco, re.M)
        titulo = re.search(r"^\*\*(.+)\*\*$", bloco, re.M)
        feats[fid] = {"desc": desc, "crit": crit, "cobre": ids_cobre(cobre.group(1)) if cobre else [],
                      "titulo": titulo.group(1) if titulo else ""}
        if not titulo:
            erro(f"{fid}: sem título")
        if not desc or len(desc.group(1)) < 40:
            erro(f"{fid}: sem descrição")
        if not crit:
            erro(f"{fid}: sem critério de aceite na forma dado, quando, então")
        if not cobre:
            erro(f"{fid}: sem linha Cobre")
    return feats


def ler_casos() -> dict[str, dict]:
    texto = (QA / "casos-de-teste.md").read_text(encoding="utf-8")
    casos = {}
    for bloco in re.split(r"^### ", texto, flags=re.M)[1:]:
        cid = bloco.split("\n", 1)[0].strip()
        if not re.fullmatch(r"TC-F\d{2}-\d{2}", cid):
            erro(f"identificador de caso fora do padrão: {cid}")
            continue
        if cid in casos:
            erro(f"{cid}: repetido")
        primeira = next((l for l in bloco.split("\n")[1:] if l.strip()), "")
        m_titulo = re.fullmatch(r"\*\*(.+)\*\*", primeira.strip())
        c = {"titulo": m_titulo.group(1) if m_titulo else ""}
        if not c["titulo"]:
            erro(f"{cid}: sem título na linha logo abaixo do identificador")
        for campo in CAMPOS_TC:
            m = re.search(rf"^\*\*{campo}:\*\*(.*)$", bloco, re.M)
            if not m:
                erro(f"{cid}: falta o campo {campo}")
                continue
            if campo in ("Preparação", "Entradas", "Saídas esperadas"):
                resto = bloco[m.end():]
                itens = re.match(r"\n\n((?:- .+\n?)+)", resto)
                if not itens:
                    erro(f"{cid}: campo {campo} vazio")
            c[campo] = m.group(1).strip()
        passos = re.findall(r"^\| (\d+) \| (.+?) \| (.+?) \|$", bloco, re.M)
        if not passos:
            erro(f"{cid}: sem passos")
        elif [int(p[0]) for p in passos] != list(range(1, len(passos) + 1)):
            erro(f"{cid}: passos fora de ordem")
        c["passos"] = passos
        if c.get("Tipo") not in ("Positivo", "Negativo"):
            erro(f"{cid}: tipo deve ser Positivo ou Negativo, veio {c.get('Tipo')}")
        c["cobre"] = ids_cobre(c.get("Cobre", ""))
        m = re.search(r"^\*\*Caso do modelo:\*\* (.+)$", bloco, re.M)
        c["modelo"] = ids_cobre(m.group(1)) if m else []
        if not cid.startswith(f"TC-{c.get('Feature', '')}-"):
            erro(f"{cid}: identificador não bate com a Feature {c.get('Feature')}")
        casos[cid] = c
    return casos


# ---------- cobertura ----------

def conferir_cobertura(registro, feats, casos) -> dict:
    conhecidos = {r.id for r in registro}
    for fid, f in feats.items():
        for i in f["cobre"]:
            if i not in conhecidos:
                erro(f"{fid}: cita item que não existe no registro: {i}")
    for cid, c in casos.items():
        if c.get("Feature") not in feats:
            erro(f"{cid}: Feature desconhecida {c.get('Feature')}")
        for i in c["cobre"] + c["modelo"]:
            if i not in conhecidos:
                erro(f"{cid}: cita item que não existe no registro: {i}")
        if not c["cobre"]:
            erro(f"{cid}: não cobre nenhuma regra nem rota")
        fcobre = set(feats.get(c.get("Feature"), {}).get("cobre", []))
        fora = [i for i in c["cobre"] if i not in fcobre]
        if fora:
            erro(f"{cid}: cobre itens que a Feature {c.get('Feature')} não lista: {fora}")

    lacunas = defaultdict(list)
    for r in registro:
        if not r.exige_pos_neg:
            if r.id.startswith("CT-"):
                if not any(r.id in c["modelo"] for c in casos.values()):
                    lacunas["caso do modelo sem Test Case"].append(r.id)
                continue
            if not any(r.id in f["cobre"] for f in feats.values()):
                lacunas["sem Feature"].append(r.id)
            if not any(r.id in c["cobre"] for c in casos.values()):
                lacunas["linha de tabela verdade sem Test Case"].append(r.id)
            continue
        if not any(r.id in f["cobre"] for f in feats.values()):
            lacunas["sem Feature"].append(r.id)
        if not any(r.id in c["cobre"] and c.get("Tipo") == "Positivo" for c in casos.values()):
            lacunas["sem caso positivo"].append(r.id)
        if not any(r.id in c["cobre"] and c.get("Tipo") == "Negativo" for c in casos.values()):
            lacunas["sem caso negativo"].append(r.id)
    for tipo, ids in lacunas.items():
        erro(f"{len(ids)} item(ns) {tipo}: {', '.join(ids)}")
    return lacunas


def conferir_rastreabilidade(casos) -> None:
    texto = (QA / "rastreabilidade.md").read_text(encoding="utf-8")
    for cid in casos:
        if f"[{cid}]" not in texto:
            erro(f"rastreabilidade: falta {cid}")


# ---------- CSV ----------

class _Html(HTMLParser):
    VAZIOS = {"br", "hr", "img"}

    def __init__(self):
        super().__init__()
        self.pilha: list[str] = []
        self.ruim = False

    def handle_starttag(self, tag, attrs):
        if tag not in self.VAZIOS:
            self.pilha.append(tag)

    def handle_endtag(self, tag):
        if not self.pilha or self.pilha.pop() != tag:
            self.ruim = True


def html_ok(valor: str) -> bool:
    p = _Html()
    p.feed(valor)
    p.close()
    return not p.ruim and not p.pilha


def ler_csv(caminho: Path) -> tuple[list[str], list[dict]]:
    dados = caminho.read_bytes()
    if dados.startswith(b"\xef\xbb\xbf"):
        dados = dados[3:]
    texto = dados.decode("utf-8")  # falha se não for UTF-8
    leitor = csv.DictReader(texto.splitlines(keepends=True))
    return leitor.fieldnames or [], list(leitor)


def conferir_csv_boards(caminho: Path, feats, com_criterios: bool) -> None:
    nome = caminho.relative_to(RAIZ)
    cab, linhas = ler_csv(caminho)
    if "Work Item Type" not in cab:
        erro(f"{nome}: falta a coluna Work Item Type")
    titulos = [c for c in cab if re.fullmatch(r"Title( \d+)?", c)]
    if not titulos:
        erro(f"{nome}: falta a coluna Title")
    for c in cab:
        if c in PROIBIDOS_BOARDS:
            erro(f"{nome}: a importação de itens novos não aceita a coluna {c}")
    if com_criterios != ("Acceptance Criteria" in cab):
        erro(f"{nome}: coluna Acceptance Criteria {'faltando' if com_criterios else 'sobrando'}")
    if len(linhas) > 1000:
        erro(f"{nome}: mais de 1000 itens")
    vistos = set()
    for n, l in enumerate(linhas, 2):
        tipo = l.get("Work Item Type")
        if tipo not in ("Epic", "Feature"):
            erro(f"{nome}:{n}: tipo {tipo} inesperado")
        preenchidos = [t for t in titulos if l.get(t)]
        if len(preenchidos) != 1:
            erro(f"{nome}:{n}: cada linha precisa de exatamente uma coluna Title preenchida")
            continue
        t = l[preenchidos[0]]
        if len(t) > 255:
            erro(f"{nome}:{n}: título com mais de 255 caracteres")
        if tipo == "Feature" and preenchidos[0] == titulos[0]:
            erro(f"{nome}:{n}: Feature precisa ficar sob o épico (Title 2)")
        if tipo == "Feature":
            vistos.add(t.split(" ", 1)[0])
        for col in ("Description", "Acceptance Criteria"):
            if l.get(col) and not html_ok(l[col]):
                erro(f"{nome}:{n}: HTML malformado em {col}")
        if "\n" in "".join(l.values()):
            erro(f"{nome}:{n}: quebra de linha dentro de célula; use <p> no HTML")
    if vistos != set(feats):
        erro(f"{nome}: Features do CSV {sorted(vistos ^ set(feats))} não batem com features.md")


def conferir_csv_tp(caminho: Path, casos_esperados: dict) -> None:
    nome = caminho.relative_to(RAIZ)
    if caminho.stat().st_size > 20 * 1024 * 1024:
        erro(f"{nome}: maior que 20 MB")
    cab, linhas = ler_csv(caminho)
    for c in OBRIGATORIOS_TP:
        if c not in cab:
            erro(f"{nome}: falta o cabeçalho obrigatório {c}")
    grupos: dict[str, list[dict]] = defaultdict(list)
    for n, l in enumerate(linhas, 2):
        if l["ID"]:
            erro(f"{nome}:{n}: ID deve ficar vazio para caso novo")
        if l["Work Item Type"] != "Test Case":
            erro(f"{nome}:{n}: Work Item Type deve ser exatamente 'Test Case'")
        if l["State"] != "Design":
            erro(f"{nome}:{n}: State de caso novo deve ser Design")
        if len(l["Title"]) > 128:
            erro(f"{nome}:{n}: título com mais de 128 caracteres ({len(l['Title'])})")
        if not l["Test Step"].isdigit():
            erro(f"{nome}:{n}: Test Step deve ser número")
        for col in OBRIGATORIOS_TP[1:]:
            if col != "ID" and not l[col]:
                erro(f"{nome}:{n}: {col} vazio")
        if "\\" in l["Area Path"] and not l["Area Path"].split("\\")[0]:
            erro(f"{nome}:{n}: Area Path malformado")
        if l.get("Description") and not html_ok(l["Description"]):
            erro(f"{nome}:{n}: HTML malformado em Description")
        grupos[l["Title"]].append(l)
    # Linhas consecutivas com o mesmo título são o mesmo caso; um título repetido em outro bloco fundiria dois casos.
    blocos = []
    for l in linhas:
        if not blocos or blocos[-1] != l["Title"]:
            blocos.append(l["Title"])
    if len(blocos) != len(set(blocos)):
        erro(f"{nome}: dois Test Cases com o mesmo título; a importação os fundiria")
    for titulo, ls in grupos.items():
        if [int(l["Test Step"]) for l in ls] != list(range(1, len(ls) + 1)):
            erro(f"{nome}: '{titulo[:40]}' com Test Step fora da sequência 1, 2, 3")
        fixos = {(l["Area Path"], l["Assigned To"], l["State"], l["Description"], l["Tags"]) for l in ls}
        if len(fixos) > 1:
            erro(f"{nome}: '{titulo[:40]}' com campos diferentes entre as linhas do mesmo caso")
        cid = titulo.split(" ", 1)[0]
        esperado = casos_esperados.get(cid)
        if not esperado:
            erro(f"{nome}: caso {cid} não existe em casos-de-teste.md")
        elif len(esperado["passos"]) != len(ls):
            erro(f"{nome}: {cid} tem {len(ls)} passos no CSV e {len(esperado['passos'])} no documento")
    faltando = set(casos_esperados) - {t.split(" ", 1)[0] for t in grupos}
    if faltando:
        erro(f"{nome}: faltam os casos {sorted(faltando)}")


# ---------- casas da carga ----------

def conferir_casas_da_carga() -> None:
    """Toda menção "casa N (`Perfil`...)" nos casos bate com o perfil da casa N na carga da demo."""
    import json
    for semente in (RAIZ / "api" / "semente" / "casas.json",
                    RAIZ / "docs" / "arquitetura" / "referencia" / "dados" / "semente" / "casas.json"):
        if semente.exists():
            break
    else:
        erro("carga da demo não encontrada; não deu para conferir o perfil das casas")
        return
    dados = json.loads(semente.read_text(encoding="utf-8"))
    dados = dados if isinstance(dados, list) else dados["casas"]
    perfil = {c["n"]: (c["perfil"]["contato"], c["perfil"]["eletrodependente"]) for c in dados}
    texto = (QA / "casos-de-teste.md").read_text(encoding="utf-8")
    for m in re.finditer(r"[Cc]asa (\d+) \(`(Sozinho|Acamado|Coabitado)`(, (não )?eletrodependente)?", texto):
        n, contato = int(m.group(1)), m.group(2)
        if n not in perfil:
            erro(f"casos-de-teste.md: casa {n} não existe na carga")
            continue
        eletro = None if m.group(3) is None else m.group(4) is None
        if perfil[n][0] != contato or (eletro is not None and eletro != perfil[n][1]):
            erro(f"casos-de-teste.md: '{m.group(0)}' não bate com a carga, que diz {perfil[n]}")


# ---------- privacidade ----------

def conferir_privacidade() -> None:
    padroes = {
        "CPF": re.compile(r"\b\d{3}\.\d{3}\.\d{3}-\d{2}\b"),
        "telefone com DDD real": re.compile(r"\((?!00\))\d{2}\) ?9?\d{4}-\d{4}"),
        "e-mail fora de domínio de exemplo": re.compile(r"[\w.+-]+@(?!(?:exemplo|example)\.(?:com|org|net)\b)[\w-]+\.[\w.]+"),
        "caminho de máquina local": re.compile(r"(/Users/|/home/[a-z]|C:\\\\Users)"),
    }
    for pasta in (QA, ENTREGA):
        if not pasta.exists():
            continue
        for arq in pasta.rglob("*"):
            if not arq.is_file() or arq.suffix not in (".md", ".csv", ".py", ".txt"):
                continue
            texto = arq.read_text(encoding="utf-8")
            for nome, rx in padroes.items():
                for m in rx.finditer(texto):
                    if nome == "CPF" and m.group(0) in CPF_PERMITIDOS:
                        continue
                    if arq.name == "conferir.py":
                        continue
                    erro(f"{arq.relative_to(RAIZ)}: {nome}: {m.group(0)}")


def main() -> int:
    registro = regras()
    feats = ler_features()
    casos = ler_casos()
    lacunas = conferir_cobertura(registro, feats, casos)
    conferir_rastreabilidade(casos)
    az = QA / "azure"
    conferir_csv_boards(az / "features.csv", feats, com_criterios=True)
    conferir_csv_boards(az / "features-agile.csv", feats, com_criterios=False)
    conferir_csv_tp(az / "casos-de-teste.csv", casos)
    for fid in feats:
        arq = az / "por-feature" / f"{fid}.csv"
        if not arq.exists():
            erro(f"falta {arq.relative_to(RAIZ)}")
            continue
        conferir_csv_tp(arq, {k: v for k, v in casos.items() if v.get("Feature") == fid})
    conferir_casas_da_carga()
    conferir_privacidade()

    exige = [r for r in registro if r.exige_pos_neg]
    pos = sum(1 for c in casos.values() if c.get("Tipo") == "Positivo")
    print(f"Registro: {len(registro)} itens ({len(exige)} regras e rotas, {len(registro) - len(exige)} casos do modelo)")
    print(f"Documentos: {len(feats)} Features, {len(casos)} Test Cases ({pos} positivos, {len(casos) - pos} negativos)")
    if erros:
        print(f"\n{len(erros)} problema(s):")
        for e in erros:
            print(f"  - {e}")
        return 1
    print("Nenhuma lacuna: toda regra e rota tem Feature, caso positivo e caso negativo; os CSV passam nas regras de importação.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
