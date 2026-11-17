#!/usr/bin/env python3
"""Checagem leve de Python para ml/ e iot/, sem instalar dependência.

Compila cada .py e exige que todo notebook tenha sido entregue executado,
como o enunciado de IA e ML pede.
Uso: python3 tools/conferir-python.py ml iot/simulador
"""
import json
import pathlib
import py_compile
import sys

erros = []
for pasta in sys.argv[1:]:
    for arquivo in sorted(pathlib.Path(pasta).rglob("*.py")):
        try:
            py_compile.compile(str(arquivo), doraise=True)
        except py_compile.PyCompileError as erro:
            erros.append(f"{arquivo}: {erro.msg.strip()}")
    for caderno in sorted(pathlib.Path(pasta).rglob("*.ipynb")):
        if ".ipynb_checkpoints" in caderno.parts:
            continue
        celulas = json.loads(caderno.read_text(encoding="utf-8"))["cells"]
        vazias = [
            i for i, c in enumerate(celulas, start=1)
            if c["cell_type"] == "code" and "".join(c["source"]).strip() and c.get("execution_count") is None
        ]
        if vazias:
            erros.append(f"{caderno}: células de código sem execução: {vazias}")

if erros:
    print("\n".join(erros))
    sys.exit(1)
print(f"ok: {', '.join(sys.argv[1:])}")
