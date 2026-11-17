#!/usr/bin/env python3
"""Falha se um arquivo de texto do repositorio tem dado com formato de dado pessoal real.

uso: verificar_dados_pessoais.py <pasta> [permitidos.txt]

Procura CPF pontuado, CPF sem pontuacao com digito verificador valido, CNS, telefone com DDD que existe
e e-mail fora de dominio de exemplo.
Os literais de permitidos.txt sao os exemplos de teste que a equipe escolheu e declarou.
"""
import re
import sys
from pathlib import Path

REGRAS = {
	"CPF": re.compile(r"(?<![\d.])\d{3}\.\d{3}\.\d{3}-\d{2}(?!\d)"),
	"CNS": re.compile(r"(?<!\d)[1-2789]\d{2} ?\d{4} ?\d{4} ?\d{4}(?!\d)"),
	"telefone": re.compile(r"(?<![\d.])\(?(?:[1-9][1-9])\)? ?9\d{4}-\d{4}(?!\d)"),
	"e-mail": re.compile(r"[\w.+-]+@(?!example\.|exemplo\.)[\w-]+(?:\.[a-z]{2,})+"),
}
CPF_SEM_PONTO = re.compile(r"(?<![\d.-])\d{11}(?![\d-])")


def cpf_valido(numero: str) -> bool:
	if len(set(numero)) == 1:
		return False
	for tamanho in (9, 10):
		soma = sum(int(d) * peso for d, peso in zip(numero[:tamanho], range(tamanho + 1, 1, -1)))
		if (soma * 10 % 11) % 10 != int(numero[tamanho]):
			return False
	return True


TEXTO = {".md", ".json", ".cs", ".sql", ".py", ".ts", ".tsx", ".txt", ".csv", ".ino", ".h", ".sh", ".yml", ".yaml", ".html"}
IGNORAR = {".git", "node_modules", "bin", "obj", "brutos", ".venv", "saida"}


def main() -> int:
	raiz = Path(sys.argv[1])
	permitidos = set()
	if len(sys.argv) > 2:
		permitidos = {l.strip() for l in Path(sys.argv[2]).read_text(encoding="utf-8").splitlines() if l.strip() and not l.startswith("#")}
	achados = 0
	arquivos = 0
	for caminho in sorted(raiz.rglob("*")):
		if not caminho.is_file() or caminho.suffix.lower() not in TEXTO or IGNORAR & set(caminho.parts):
			continue
		arquivos += 1
		for numero, linha in enumerate(caminho.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
			encontrados = [(tipo, a) for tipo, regra in REGRAS.items() for a in regra.findall(linha)]
			encontrados += [("CPF sem pontuacao", a) for a in CPF_SEM_PONTO.findall(linha) if cpf_valido(a)]
			for tipo, achado in encontrados:
				if achado not in permitidos:
					achados += 1
					print(f"{caminho.relative_to(raiz)}:{numero}: {tipo}: {achado}")
	print(f"{arquivos} arquivos lidos, {achados} achado(s) fora da lista de permitidos")
	return 1 if achados else 0


if __name__ == "__main__":
	sys.exit(main())
