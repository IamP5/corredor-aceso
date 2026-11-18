"""Lista de regras e rotas que o QA precisa cobrir, lida dos documentos de arquitetura.

Nada aqui é digitado à mão a partir da memória: cada identificador sai de uma tabela ou lista
de docs/arquitetura/. Se um documento mudar, o registro muda junto e a conferência aponta o buraco.

Uso: python3 docs/qa/ferramentas/registro.py   (imprime o registro)
"""
from __future__ import annotations

import re
import sys
from dataclasses import dataclass
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
ARQ = RAIZ / "docs" / "arquitetura"
DOMINIO = ARQ / "03-modelo-de-dominio.md"
EVENTOS = ARQ / "02-event-storming.md"
DADOS = ARQ / "04-modelo-de-dados.md"
INFRA = ARQ / "05-infra-e-entrega.md"


@dataclass(frozen=True)
class Regra:
    id: str
    grupo: str
    origem: str
    texto: str
    exige_pos_neg: bool = True


def _secao(texto: str, titulo_re: str) -> str:
    """Devolve o corpo da seção cujo título casa com titulo_re, até o próximo título de mesmo nível ou acima."""
    m = re.search(rf"^(#+) +{titulo_re}.*$", texto, re.M)
    if not m:
        raise SystemExit(f"registro: seção não encontrada: {titulo_re}")
    nivel = len(m.group(1))
    resto = texto[m.end():]
    fim = re.search(rf"^#{{1,{nivel}}} ", resto, re.M)
    return resto[: fim.start()] if fim else resto


def _linhas_tabela(corpo: str) -> list[list[str]]:
    linhas = []
    for linha in corpo.splitlines():
        if not linha.startswith("|") or re.match(r"^\|[-| :]+\|$", linha):
            continue
        celulas = [c.strip() for c in linha.strip().strip("|").split("|")]
        linhas.append(celulas)
    return linhas[1:] if linhas else []  # sem o cabeçalho


def _primeiro_codigo(celula: str) -> str:
    m = re.search(r"`([^`]+)`", celula)
    return m.group(1) if m else celula.split()[0]


def _lista_numerada(corpo: str) -> list[str]:
    return [m.group(2).strip() for m in re.finditer(r"^(\d+)\. (.+)$", corpo, re.M)]


def regras() -> list[Regra]:
    dom = DOMINIO.read_text(encoding="utf-8")
    eve = EVENTOS.read_text(encoding="utf-8")
    dad = DADOS.read_text(encoding="utf-8")
    inf = INFRA.read_text(encoding="utf-8")
    saida: list[Regra] = []

    # Rotas HTTP, 03 seção 6.3
    for c in _linhas_tabela(_secao(dom, r"6\.3 Rotas HTTP")):
        if len(c) >= 5 and re.match(r"`(GET|POST|PUT|DELETE) ", c[0]):
            rota = c[0].strip("`")
            saida.append(Regra(f"`{rota}`", "Rota", "03 §6.3", f"{c[1]}; sucesso {c[3]}; erros {c[4] or 'nenhum'}"))

    # Regras de contato, 03 seção 5.4
    for c in _linhas_tabela(_secao(dom, r"5\.4 As regras em ordem")):
        if c[0].isdigit() and "Fora da entrega" not in " ".join(c):
            saida.append(Regra(f"R{c[0]}", "Regra de contato", "03 §5.4", f"{c[1].strip('`')}: {c[3]}"))

    # Sinal de vida por perfil, 03 seção 5.3
    for c in _linhas_tabela(_secao(dom, r"5\.3 Sinal de vida")):
        saida.append(Regra(f"SV-{c[0].strip('`')}", "Sinal de vida", "03 §5.3", c[1]))

    # Tabela verdade da situação, 03 seção 5.6. Cada linha é um fato dentro das regras R1 a R4:
    # precisa de Feature e de um Test Case; a regra em si é que precisa de positivo e negativo.
    for c in _linhas_tabela(_secao(dom, r"5\.6 Tabela verdade da situação")):
        if c[0].isdigit():
            saida.append(Regra(f"TV-SIT-{c[0]}", "Tabela verdade da situação", "03 §5.6",
                               f"turno {c[1]}, R1 {c[2]}, R2 {c[3]}, R3 {c[4]}, R4 {c[5]}, sinal {c[6]} -> {c[7]} {c[8]}",
                               exige_pos_neg=False))

    # Tabela verdade do atendimento, 03 seção 5.7
    for i, c in enumerate(_linhas_tabela(_secao(dom, r"5\.7 Tabela verdade do atendimento")), 1):
        saida.append(Regra(f"TV-ATD-{i}", "Tabela verdade do atendimento", "03 §5.7",
                           f"{c[0].strip('`')} + {c[1]} + {c[2]} -> atendida {c[3]}", exige_pos_neg=False))

    # Invariantes de cada agregado, 03 seções 4.2 a 4.4
    for m in re.finditer(r"^#### Agregado `(\w+)`$", dom, re.M):
        nome = m.group(1)
        corpo = dom[m.end():]
        fim = re.search(r"^#{2,4} ", corpo, re.M)
        corpo = corpo[: fim.start()] if fim else corpo
        inv = re.search(r"^Invariantes:\n\n((?:\d+\. .+\n)+)", corpo, re.M)
        if not inv:
            raise SystemExit(f"registro: agregado sem invariantes: {nome}")
        for j, item in enumerate(_lista_numerada(inv.group(1)), 1):
            saida.append(Regra(f"INV-{nome}-{j}", "Invariante", "03 §4", item))

    # TradutorDeDesfecho, 03 seção 4.3
    tab = re.search(r"\| Desfecho \| A pessoa foi vista\? .*?\n\n", dom, re.S)
    for c in _linhas_tabela(tab.group(0)):
        saida.append(Regra(f"DESF-{c[0].strip('`')}", "Desfecho", "03 §4.3", f"{c[2]}; {c[3]}"))

    # PoliticaDeCanal, 03 seção 4.4
    tab = re.search(r"\| Canal \| Aceita \| Exposição \|.*?\n\n", dom, re.S)
    for c in _linhas_tabela(tab.group(0)):
        saida.append(Regra(f"CANAL-{c[0].strip('`')}", "Política de canal", "03 §4.4", f"aceita {c[1]}, exposição {c[2]}"))

    # Regra do farol, 03 seção 4.4
    farol = re.search(r"^Regra do farol: (.+)$", dom, re.M)
    if not farol:
        raise SystemExit("registro: regra do farol não encontrada")
    for cor in ("Vermelho", "Amarelo", "Verde"):
        if f"`{cor}`" not in farol.group(1):
            raise SystemExit(f"registro: cor {cor} sumiu da regra do farol")
        saida.append(Regra(f"FAROL-{cor}", "Farol", "03 §4.4", farol.group(1)))

    # Chaves de idempotência, 03 seção 4.5
    for c in _linhas_tabela(_secao(dom, r"4\.5 Chave de idempotência")):
        cmd = _primeiro_codigo(c[1])
        if "Pedido de contato" in c[1]:
            cmd = "PedidoDeContato"
        saida.append(Regra(f"IDEM-{c[0]}-{cmd}", "Idempotência", "03 §4.5", f"{c[1]}: {c[2]}"))

    # Regras essenciais, 03 seção 8.1
    for j, item in enumerate(_lista_numerada(_secao(dom, r"8\.1 Regras essenciais")), 1):
        saida.append(Regra(f"NC-{j}", "Regra essencial", "03 §8.1", item))

    # Políticas, 02 sessão 1
    for c in _linhas_tabela(_secao(eve, r"Políticas")):
        if re.fullmatch(r"P\d+", c[0]):
            saida.append(Regra(c[0], "Política", "02 Políticas", c[1]))

    # App: cache e fila de desfechos, 04 seções 6.2 e 6.3
    cache = _secao(dad, r"6\.2 Cache da lista de nomes e do radar")
    for j, m in enumerate(re.finditer(r"^- \*\*(.+?)\*\* (.+)$", cache, re.M), 1):
        saida.append(Regra(f"APP-CACHE-{j}", "App offline", "04 §6.2", f"{m.group(1)} {m.group(2)}"))
    fila = _secao(dad, r"6\.3 Fila de desfechos")
    for j, c in enumerate(_linhas_tabela(fila), 1):
        saida.append(Regra(f"APP-FILA-{j}", "App offline", "04 §6.3", f"{c[0]}: {c[1]}"))

    # Infra observável: cenários do teste de falha e rubrica de Operating Systems, 05 seções 4.5 e 7
    for c in _linhas_tabela(_secao(inf, r"4\.5 Teste de falha")):
        nome = c[0].strip("`").split("`")[0]
        saida.append(Regra(f"INFRA-{nome}", "Infra", "05 §4.5", f"{c[1]} -> {c[2]}"))
    for j, c in enumerate(_linhas_tabela(_secao(inf, r"7\. Como cada item da rubrica")), 1):
        saida.append(Regra(f"INFRA-RUB-{j}", "Infra", "05 §7", f"{c[0]}: {c[3]}"))

    # Casos de teste do modelo, 03 seção 7. Precisam de ao menos um Test Case que os cite.
    for c in _linhas_tabela(_secao(dom, r"7\. Casos de teste")):
        if c[0].isdigit():
            saida.append(Regra(f"CT-{c[0]}", "Caso do modelo", "03 §7", " | ".join(c[1:]), exige_pos_neg=False))

    ids = [r.id for r in saida]
    repetidos = {i for i in ids if ids.count(i) > 1}
    if repetidos:
        raise SystemExit(f"registro: identificadores repetidos: {sorted(repetidos)}")
    return saida


if __name__ == "__main__":
    lista = regras()
    grupos: dict[str, int] = {}
    for r in lista:
        grupos[r.grupo] = grupos.get(r.grupo, 0) + 1
        if "-v" in sys.argv:
            print(f"{r.id:40} {r.origem:14} {r.texto[:110]}")
        else:
            print(r.id)
    print(f"\n{len(lista)} itens: " + ", ".join(f"{g} {n}" for g, n in grupos.items()), file=sys.stderr)
