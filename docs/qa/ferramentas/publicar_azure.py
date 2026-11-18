"""Publica o backlog e os Test Cases de docs/qa/fonte/ no Azure DevOps pela API REST.

Cria ou atualiza, procurando pelo título: épicos, Features, um PBI por Feature e os Test Cases com os passos.
Cada caso é ligado à Feature e ao PBI por "Tests / Tested By", e as consultas compartilhadas da pasta
"Corredor Aceso QA" listam Features, backlog e casos. Se o usuário tem o acesso Basic + Test Plans, o script também
monta o Test Plan com uma suíte estática por épico e uma suíte baseada em requisito por PBI. Com o acesso Basic, o
Test Plan é pulado: os Test Cases ficam no Boards, como no exemplo da aula.

O PAT é lido de um arquivo fora do Git (padrão ~/.azdo-pat) e nunca é impresso. Escopos: Work Items (read, write
& manage), Test Management (read & write) e Project and Team (read, write & manage).

Piloto com uma Feature e um caso:

    python3 docs/qa/ferramentas/publicar_azure.py --org https://dev.azure.com/ORG --projeto "Corredor Aceso" \\
        --features F01 --max-casos 1

Tudo:

    python3 docs/qa/ferramentas/publicar_azure.py --org https://dev.azure.com/ORG --projeto "Corredor Aceso"
"""
from __future__ import annotations

import argparse
import base64
import json
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from xml.sax.saxutils import escape as xml_escape

sys.path.insert(0, str(Path(__file__).parent))
import gerar  # noqa: E402

API = "api-version=7.1"
TAG = "Corredor Aceso"

# O Azure esconde do backlog os itens em Done; "andamento" mantém tudo visível em Boards, Backlogs.
ESTADOS = {
    "novo": {},
    "andamento": {"Epic": "In Progress", "Feature": "In Progress", "Product Backlog Item": "Committed",
                  "Test Case": "Ready"},
    "concluido": {"Epic": "Done", "Feature": "Done", "Product Backlog Item": "Done", "Test Case": "Ready"},
}

_COLUNAS = "[System.Id], [System.WorkItemType], [System.Title], [System.State], [System.Tags]"
CONSULTAS = {
    "1. Features": f"SELECT {_COLUNAS} FROM WorkItems WHERE [System.TeamProject] = @project "
                   "AND [System.WorkItemType] = 'Feature' ORDER BY [System.Title]",
    "2. Backlog (Epic, Feature e PBI)": (
        f"SELECT {_COLUNAS} FROM WorkItemLinks WHERE ([Source].[System.TeamProject] = @project "
        "AND [Source].[System.WorkItemType] = 'Epic') AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') "
        "AND ([Target].[System.WorkItemType] IN ('Feature', 'Product Backlog Item')) "
        "ORDER BY [System.Title] MODE (Recursive)"),
    "3. Test Cases por Feature": (
        f"SELECT {_COLUNAS} FROM WorkItemLinks WHERE ([Source].[System.TeamProject] = @project "
        "AND [Source].[System.WorkItemType] = 'Feature') AND ([System.Links.LinkType] = 'Microsoft.VSTS.Common.TestedBy-Forward') "
        "AND ([Target].[System.WorkItemType] = 'Test Case') ORDER BY [System.Title] MODE (MustContain)"),
    "4. Test Cases positivos": f"SELECT {_COLUNAS} FROM WorkItems WHERE [System.TeamProject] = @project "
                               "AND [System.WorkItemType] = 'Test Case' AND [System.Tags] CONTAINS 'Positivo' "
                               "ORDER BY [System.Title]",
    "5. Test Cases negativos": f"SELECT {_COLUNAS} FROM WorkItems WHERE [System.TeamProject] = @project "
                               "AND [System.WorkItemType] = 'Test Case' AND [System.Tags] CONTAINS 'Negativo' "
                               "ORDER BY [System.Title]",
}


class Azure:
    def __init__(self, org: str, projeto: str, pat: str):
        self.org = org.rstrip("/")
        self.projeto = projeto
        self.base = f"{self.org}/{urllib.parse.quote(projeto)}"
        self._auth = "Basic " + base64.b64encode(f":{pat}".encode()).decode()

    def req(self, metodo: str, url: str, corpo=None, tipo: str = "application/json", aceita: tuple = ()):
        dados = None if corpo is None else json.dumps(corpo).encode()
        r = urllib.request.Request(url, data=dados, method=metodo,
                                   headers={"Authorization": self._auth, "Content-Type": tipo})
        try:
            with urllib.request.urlopen(r) as resp:
                texto = resp.read().decode()
                return json.loads(texto) if texto else None
        except urllib.error.HTTPError as e:
            if e.code in aceita:
                return e.code
            raise SystemExit(f"{metodo} {url} -> {e.code}: {e.read().decode()[:800]}")

    # ---------- Work items ----------

    def itens_existentes(self) -> dict[tuple[str, str], dict]:
        wiql = {"query": "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project"}
        ids = [w["id"] for w in self.req("POST", f"{self.base}/_apis/wit/wiql?{API}", wiql)["workItems"]]
        mapa = {}
        for i in range(0, len(ids), 200):
            lote = ",".join(map(str, ids[i:i + 200]))
            for w in self.req("GET", f"{self.org}/_apis/wit/workitems?ids={lote}&$expand=relations&{API}")["value"]:
                mapa[(w["fields"]["System.WorkItemType"], w["fields"]["System.Title"])] = w
        return mapa

    def url_item(self, id_: int) -> str:
        return f"{self.org}/_apis/wit/workItems/{id_}"

    def salvar(self, existentes, tipo: str, campos: dict, links: list[tuple[str, int]]) -> int:
        """Cria ou atualiza o item. links: (tipo de relação, id do alvo)."""
        atual = existentes.get((tipo, campos["System.Title"]))
        ops = [{"op": "add", "path": f"/fields/{k}", "value": v} for k, v in campos.items()]
        ja = set()
        if atual:
            ja = {(r["rel"], r["url"].rsplit("/", 1)[1]) for r in atual.get("relations") or []}
        for rel, alvo in links:
            if (rel, str(alvo)) not in ja:
                ops.append({"op": "add", "path": "/relations/-", "value": {"rel": rel, "url": self.url_item(alvo)}})
        if atual:
            w = self.req("PATCH", f"{self.org}/_apis/wit/workitems/{atual['id']}?{API}", ops, "application/json-patch+json")
            acao = "atualizado"
        else:
            # Item novo nasce no estado inicial do processo; o estado pedido vem num segundo passo.
            estado = campos.get("System.State")
            ops = [o for o in ops if o["path"] != "/fields/System.State"]
            w = self.req("POST", f"{self.base}/_apis/wit/workitems/${urllib.parse.quote(tipo)}?{API}", ops,
                         "application/json-patch+json")
            if estado and w["fields"]["System.State"] != estado:
                w = self.req("PATCH", f"{self.org}/_apis/wit/workitems/{w['id']}?{API}",
                             [{"op": "add", "path": "/fields/System.State", "value": estado}],
                             "application/json-patch+json")
            acao = "criado"
        existentes[(tipo, campos["System.Title"])] = w
        print(f"  {tipo} {w['id']} {acao}: {campos['System.Title']}")
        return w["id"]

    # ---------- Sprint ----------

    def datas_sprint(self, sprint: str, inicio: str, fim: str) -> None:
        url = f"{self.base}/_apis/wit/classificationnodes/Iterations/{urllib.parse.quote(sprint)}?{API}"
        self.req("PATCH", url, {"attributes": {"startDate": f"{inicio}T00:00:00Z", "finishDate": f"{fim}T00:00:00Z"}})
        print(f"  {sprint}: {inicio} a {fim}")

    # ---------- Consultas compartilhadas ----------

    def consultas(self, pasta: str, consultas: dict[str, str]) -> None:
        raiz = f"{self.base}/_apis/wit/queries/{urllib.parse.quote('Shared Queries')}"
        if self.req("GET", f"{raiz}/{urllib.parse.quote(pasta)}?{API}", aceita=(404,)) == 404:
            self.req("POST", f"{raiz}?{API}", {"name": pasta, "isFolder": True})
        for nome, wiql in consultas.items():
            url = f"{raiz}/{urllib.parse.quote(pasta)}/{urllib.parse.quote(nome)}?{API}"
            if self.req("GET", url, aceita=(404,)) == 404:
                self.req("POST", f"{raiz}/{urllib.parse.quote(pasta)}?{API}", {"name": nome, "wiql": wiql})
                print(f"  Consulta criada: {pasta}/{nome}")
            else:
                self.req("PATCH", url, {"wiql": wiql})
                print(f"  Consulta atualizada: {pasta}/{nome}")

    # ---------- Test Plans ----------

    def plano(self, nome: str, area: str, iteracao: str) -> dict | None:
        for p in self.req("GET", f"{self.base}/_apis/testplan/plans?{API}")["value"]:
            if p["name"] == nome:
                return p
        p = self.req("POST", f"{self.base}/_apis/testplan/plans?{API}",
                     {"name": nome, "areaPath": area, "iteration": iteracao}, aceita=(401, 403))
        if p in (401, 403):
            return None
        print(f"  Test Plan {p['id']} criado: {nome}")
        return p

    def suites(self, plano: int) -> list[dict]:
        return self.req("GET", f"{self.base}/_apis/testplan/Plans/{plano}/suites?{API}")["value"]

    def suite(self, plano: int, existentes: list[dict], pai: int, nome: str, requisito: int | None = None) -> int:
        for s in existentes:
            if s.get("parentSuite", {}).get("id") != pai:
                continue
            if (requisito and s.get("requirementId") == requisito) or (not requisito and s["name"] == nome):
                return s["id"]
        corpo = {"name": nome, "parentSuite": {"id": pai}, "suiteType": "staticTestSuite"}
        if requisito:
            corpo.update(suiteType="requirementTestSuite", requirementId=requisito)
        s = self.req("POST", f"{self.base}/_apis/testplan/Plans/{plano}/suites?{API}", corpo)
        existentes.append(s)
        print(f"  Suíte {s['id']} criada: {s['name']}")
        return s["id"]

    def incluir_casos(self, plano: int, suite: int, ids: list[int]) -> None:
        url = f"{self.base}/_apis/testplan/Plans/{plano}/Suites/{suite}/TestCase?{API}"
        ja = {c["workItem"]["id"] for c in self.req("GET", url)["value"]}
        novos = [i for i in ids if i not in ja]
        if novos:
            self.req("POST", url, [{"workItem": {"id": i}} for i in novos])
            print(f"  {len(novos)} caso(s) na suíte {suite}")


# ---------- Conteúdo ----------

def html_historia(hist) -> str:
    _, como, quero, para_que = hist
    return (f"<div><p><b>COMO</b> {gerar._h(como)},</p><p><b>EU QUERO</b> {gerar._h(quero)},</p>"
            f"<p><b>PARA QUE</b> {gerar._h(para_que)}.</p></div>")


def html_criterios_pbi(f, casos) -> str:
    partes = []
    for n, (d, q, e) in enumerate(f["criterios"], 1):
        partes.append(f"<p><b>Cenário {n}</b><br>&nbsp; <b>Dado</b> {gerar._h(d)}<br>&nbsp; <b>Quando</b> {gerar._h(q)}"
                      f"<br>&nbsp; <b>Então</b> {gerar._h(e)}</p>")
    dod = [f"Os {len(casos)} Test Cases ({casos[0]['id']} a {casos[-1]['id']}) aprovados no Test Plan."]
    autos = []
    for c in casos:
        alvo, status = gerar._auto(c)
        if alvo and status == "existe" and alvo not in autos:
            autos.append(alvo)
    if autos:
        dod.append("Testes automatizados verdes: " + ", ".join(f"`{a}`" for a in autos) + ".")
    partes.append("<p><b>DoD:</b></p><ul>" + "".join(f"<li>{gerar._h(x)}</li>" for x in dod) + "</ul>")
    return "".join(partes)


def xml_passos(passos) -> str:
    out = [f'<steps id="0" last="{len(passos) + 1}">']
    for i, (acao, esperado) in enumerate(passos, 2):
        a = xml_escape(f"<P>{gerar._h(acao)}</P>")
        e = xml_escape(f"<P>{gerar._h(esperado)}</P>")
        out.append(f'<step id="{i}" type="ValidateStep"><parameterizedString isformatted="true">{a}</parameterizedString>'
                   f'<parameterizedString isformatted="true">{e}</parameterizedString><description/></step>')
    out.append("</steps>")
    return "".join(out)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--org", required=True, help="https://dev.azure.com/ORG")
    ap.add_argument("--projeto", required=True)
    ap.add_argument("--pat-arquivo", type=Path, default=Path.home() / ".azdo-pat")
    ap.add_argument("--sprint", default="Sprint 1")
    ap.add_argument("--inicio", default="2026-11-10", help="início da sprint (AAAA-MM-DD)")
    ap.add_argument("--fim", default="2026-11-19", help="fim da sprint (AAAA-MM-DD)")
    ap.add_argument("--plano", default="Corredor Aceso - Testes de aceitação")
    ap.add_argument("--features", help="lista separada por vírgula, por exemplo F01,F02 (padrão: todas)")
    ap.add_argument("--max-casos", type=int, help="limite de casos por Feature (para o piloto)")
    ap.add_argument("--estado", choices=sorted(ESTADOS), default="novo",
                    help="novo: estado inicial; andamento: Epic e Feature In Progress, PBI Committed, Test Case Ready "
                         "(aparecem no backlog); concluido: Done e Ready (somem do backlog)")
    args = ap.parse_args()

    az = Azure(args.org, args.projeto, args.pat_arquivo.read_text().strip())
    features, casos = gerar.carregar_fonte()
    backlog = gerar._carregar("backlog")
    escolhidas = set(args.features.split(",")) if args.features else {f["id"] for f in features}
    area = args.projeto
    iteracao = f"{args.projeto}\\{args.sprint}"
    comuns = {"System.AreaPath": area, "System.IterationPath": iteracao}

    def com_estado(tipo: str) -> dict:
        estado = ESTADOS[args.estado].get(tipo)
        return {**comuns, **({"System.State": estado} if estado else {})}

    contador = iter(range(1000, 10**9, 1000))

    def ordem() -> int:
        return next(contador)

    print("Sprint")
    az.datas_sprint(args.sprint, args.inicio, args.fim)

    print("Work items")
    existentes = az.itens_existentes()
    por_id = {f["id"]: f for f in features}
    ids_pbi, ids_casos, epico_de = {}, {}, {}
    for ep in backlog.EPICOS:
        fs = [fid for fid in ep["features"] if fid in escolhidas]
        if not fs:
            continue
        id_ep = az.salvar(existentes, "Epic", {
            "System.Title": ep["titulo"], "System.Description": f"<p>{gerar._h(ep['descricao'])}</p>",
            "System.Tags": TAG, "Microsoft.VSTS.Common.BacklogPriority": ordem(), **com_estado("Epic")}, [])
        for fid in fs:
            f = por_id[fid]
            epico_de[fid] = ep["titulo"]
            cs = [c for c in casos if c["feature"] == fid]
            id_f = az.salvar(existentes, "Feature", {
                "System.Title": f"{fid} {f['titulo']}",
                "System.Description": gerar._html_feature_desc(f),
                "Microsoft.VSTS.Common.AcceptanceCriteria": gerar._html_criterios(f),
                "System.Tags": f"{TAG}; {f['modulo']}", "Microsoft.VSTS.Common.BacklogPriority": ordem(),
                **com_estado("Feature")},
                [("System.LinkTypes.Hierarchy-Reverse", id_ep)])
            hist = backlog.HISTORIAS[fid]
            id_pbi = az.salvar(existentes, "Product Backlog Item", {
                "System.Title": f"PBI-{fid[1:]}: {hist[0]}",
                "System.Description": html_historia(hist),
                "Microsoft.VSTS.Common.AcceptanceCriteria": html_criterios_pbi(f, cs),
                "System.Tags": f"{TAG}; {fid}", "Microsoft.VSTS.Common.BacklogPriority": ordem(),
                **com_estado("Product Backlog Item")},
                [("System.LinkTypes.Hierarchy-Reverse", id_f)])
            ids_pbi[fid] = id_pbi
            ids_casos[fid] = []
            for c in cs[:args.max_casos]:
                ids_casos[fid].append(az.salvar(existentes, "Test Case", {
                    "System.Title": gerar.titulo_azure(c),
                    "System.Description": gerar._html_caso(c),
                    "Microsoft.VSTS.TCM.Steps": xml_passos(c["passos"]),
                    "System.Tags": f"{fid}; {c['tipo']}", **com_estado("Test Case")},
                    [("Microsoft.VSTS.Common.TestedBy-Reverse", id_f),
                     ("Microsoft.VSTS.Common.TestedBy-Reverse", id_pbi)]))

    print("Consultas")
    az.consultas("Corredor Aceso QA", CONSULTAS)

    print("Test Plan")
    plano = az.plano(args.plano, area, iteracao)
    if plano is None:
        print("  Sem permissão para criar Test Plan: o usuário precisa do acesso Basic + Test Plans. "
              "Os Test Cases ficam no Boards, ligados à Feature e ao PBI por Tests/Tested By.")
        return 0
    suites = az.suites(plano["id"])
    raiz = plano["rootSuite"]["id"]
    for fid, id_pbi in ids_pbi.items():
        s_ep = az.suite(plano["id"], suites, raiz, epico_de[fid])
        s_req = az.suite(plano["id"], suites, s_ep, f"PBI-{fid[1:]}", requisito=id_pbi)
        az.incluir_casos(plano["id"], s_req, ids_casos[fid])

    print(f"Pronto: {args.org}/{urllib.parse.quote(args.projeto)}/_testPlans/define?planId={plano['id']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
