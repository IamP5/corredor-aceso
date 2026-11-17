#!/usr/bin/env python3
"""Confere que casas.json so tem dado fabricado pelas regras do gerador."""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import gerar_semente as g

dados = json.loads(g.ARQUIVO.read_text(encoding="utf-8"))
casas = dados["casas"]
erros = []


def exige(condicao: bool, texto: str) -> None:
	if not condicao:
		erros.append(texto)


exige(g.ARQUIVO.read_text(encoding="utf-8") == g.texto(), "casas.json difere do que o gerador produz")
exige(len(casas) == 40, "nao sao 40 casas")
exige(len({c["id"] for c in casas}) == 40, "id repetido")
exige(all(re.fullmatch(r"[0-9a-f]{8}-[0-9a-f]{4}-5[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}", c["id"]) for c in casas), "id fora do formato")
exige(not re.search(r"[0-9a-f]{32}", g.ARQUIVO.read_text(encoding="utf-8")), "a semente tem algo com forma de token")
exige(len({c["nome"] for c in casas}) == 40 and len({c["endereco"] for c in casas}) == 40, "nome ou endereco repetido")
for c in casas:
	pessoas = [c["nome"]] + ([c["vizinho"]["nome"]] if c["vizinho"] else [])
	for nome in pessoas:
		prenome, sobrenome = nome.split(" ", 1)
		exige(prenome in g.PRENOMES and sobrenome in g.ARVORES, f"casa {c['n']}: nome fora das listas: {nome}")
	exige(c["endereco"].endswith(" - " + g.BAIRRO), f"casa {c['n']}: endereco fora do bairro ficticio")
	if c["vizinho"]:
		exige(re.fullmatch(r"\(00\) 90000-\d{4}", c["vizinho"]["telefone"]) is not None, f"casa {c['n']}: telefone fora do DDD 00")
		exige(c["autorizacoes"]["avisarVizinho"], f"casa {c['n']}: vizinho sem autorizacao")
	exige(not re.search(r"\d{3}\.\d{3}\.\d{3}-\d{2}", json.dumps(c)), f"casa {c['n']}: CPF")
contagem = {k: sum(1 for c in casas if c["perfil"]["contato"] == k) for k in ("Sozinho", "Acamado", "Coabitado")}
eletro = sum(1 for c in casas if c["perfil"]["eletrodependente"])
com_vizinho = sum(1 for c in casas if c["vizinho"])
if erros:
	print("\n".join(erros))
	sys.exit(1)
print(f"40 casas: {contagem}, {eletro} eletrodependentes, {com_vizinho} com vizinho, "
	f"{sum(1 for c in casas if c['autorizacoes']['avisarVizinho'])} com AvisarVizinho, "
	f"{sum(1 for c in casas if c['autorizacoes']['avisarFamilia'])} com AvisarFamilia")
