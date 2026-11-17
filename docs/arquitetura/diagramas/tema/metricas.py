"""Gera tema/metricas.json: largura de cada caractere nas fontes do tema.

O Pele mede o texto para dimensionar as caixas. Fora do navegador ele usa uma
tabela de larguras de uma fonte sem serifa genérica, que não é a Atkinson
Hyperlegible. O render.mjs usa este arquivo como `measurer`, e as caixas saem
do tamanho do texto que o leitor vê.

Só precisa rodar de novo se as fontes mudarem.
Uso: pip install fonttools brotli && python3 tema/metricas.py
"""
import json
import sys
from pathlib import Path

from fontTools.ttLib import TTFont

PASTA = Path(__file__).parent
FONTES = {
    "regular": "atkinson-hyperlegible-latin-400-normal",
    "negrito": "atkinson-hyperlegible-latin-700-normal",
    "mono": "atkinson-hyperlegible-mono-latin-400-normal",
}
CARACTERES = [chr(c) for c in range(0x20, 0x100)] + list("‘’“”–—…•·€←↑→↓≥≤×")


def abrir(nome):
    # Aceita woff2 (precisa de brotli) ou woff com o mesmo nome, passado como argumento.
    for caminho in [PASTA / "fontes" / f"{nome}.woff2", *map(Path, sys.argv[1:])]:
        if caminho.stem.startswith(nome) and caminho.exists():
            try:
                return TTFont(caminho)
            except Exception:
                continue
    raise SystemExit(f"não consegui abrir {nome}")


saida = {}
for estilo, nome in FONTES.items():
    fonte = abrir(nome)
    upm = fonte["head"].unitsPerEm
    cmap = fonte.getBestCmap()
    hmtx = fonte["hmtx"]
    larguras = {c: round(hmtx[cmap[ord(c)]][0] / upm, 4) for c in CARACTERES if ord(c) in cmap}
    media = round(sum(larguras.values()) / len(larguras), 4)
    saida[estilo] = {"media": media, "larguras": larguras}

(PASTA / "metricas.json").write_text(json.dumps(saida, ensure_ascii=False, separators=(",", ":")))
print({k: (len(v["larguras"]), v["media"]) for k, v in saida.items()})
