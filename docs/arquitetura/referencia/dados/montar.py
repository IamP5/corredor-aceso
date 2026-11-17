#!/usr/bin/env python3
"""Monta docs/arquitetura/04-modelo-de-dados.md a partir de modelo-de-dados.fonte.md.

Todo bloco de codigo conferido entra por diretiva, entao o documento nao tem copia manual:
  @@arquivo caminho@@                              o arquivo inteiro
  @@trecho caminho ## primeira ## ultima@@         da linha que contem `primeira` ate a que contem `ultima`
Com --conferir, monta de novo e falha se o resultado difere do arquivo gravado.
"""
import re
import sys
from pathlib import Path

AQUI = Path(__file__).parent
FONTE = AQUI / "modelo-de-dados.fonte.md"
DESTINO = AQUI.parent.parent / "04-modelo-de-dados.md"


def trecho(caminho: str, primeira: str, ultima: str) -> str:
	linhas = (AQUI / caminho).read_text(encoding="utf-8").splitlines()
	inicio = next(i for i, l in enumerate(linhas) if primeira in l)
	fim = next(i for i, l in enumerate(linhas) if i >= inicio and ultima in l)
	corte = linhas[inicio:fim + 1]
	recuo = min(len(l) - len(l.lstrip("\t")) for l in corte if l.strip())
	return "\n".join(l[recuo:] for l in corte)


def resolver(linha: str) -> str:
	m = re.fullmatch(r"@@arquivo (.+?)@@", linha.strip())
	if m:
		return (AQUI / m.group(1)).read_text(encoding="utf-8").rstrip("\n")
	m = re.fullmatch(r"@@trecho (.+?) ## (.+?) ## (.+?)@@", linha.strip())
	if m:
		return trecho(m.group(1), m.group(2), m.group(3))
	if "@@" in linha:
		raise SystemExit(f"diretiva mal escrita: {linha}")
	return linha


texto = "\n".join(resolver(l) for l in FONTE.read_text(encoding="utf-8").splitlines()) + "\n"
if "--conferir" in sys.argv:
	if DESTINO.read_text(encoding="utf-8") != texto:
		raise SystemExit("04-modelo-de-dados.md difere da fonte montada. Rode montar.py.")
	print(f"04-modelo-de-dados.md confere com a fonte e com os arquivos conferidos ({texto.count(chr(10))} linhas)")
else:
	DESTINO.write_text(texto, encoding="utf-8")
	print(f"{DESTINO.name}: {texto.count(chr(10))} linhas")
