#!/usr/bin/env bash
# Monta o guia de avaliação em _site/, o mesmo que o GitHub Pages publica.
# Falha se o guia ou um documento do leitor apontar para um arquivo que não existe no repositório,
# ou se um desenho .mmd citado não tiver o SVG escuro ao lado.
#
# As evidências abrem dentro do guia, então cada `caminho:` do guia.js é copiado para
# _site/repo/<caminho>, junto com docs/evidencias inteira. _site/repo/indice.json lista
# tudo o que foi copiado; a gaveta usa a lista para abrir pastas e seguir links internos.
# Cada `leitura:` do guia.js (REQUISITOS.md e docs/arquitetura) abre no leitor, renderizado do Markdown.
# Os desenhos entram pelo nome.escuro.svg que fica ao lado de cada .mmd; o guia troca as cores por CSS.
#
# Uso: bash tools/montar-site.sh [saida]   (padrão: _site)
# Ver no navegador: python3 -m http.server -d _site 8000
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
SAIDA="${1:-_site}"

evidencias() { grep -oE "caminho: '[^']+'" site/guia.js | sed -E "s/^caminho: '([^']+)'$/\1/"; }

# Lê os documentos do leitor: imprime o que copiar (cada .md e o SVG de cada desenho citado)
# e sai com 1 se algum link relativo ou desenho não existir.
leituras() {
  grep -oE "leitura: '[^']+'" site/guia.js | sed -E "s/^leitura: '([^']+)'$/\1/" | python3 -c '
import os, re, sys, urllib.parse
falhou = False
def quebrado(msg):
    global falhou
    falhou = True
    print(msg, file=sys.stderr)
for md in sys.stdin.read().split():
    if not os.path.isfile(md):
        quebrado(f"link quebrado: {md}")
        continue
    print(md)
    texto = open(md, encoding="utf-8").read()
    texto = re.sub(r"^[ \t]*(`{3,}|~{3,})[^\n]*\n.*?^[ \t]*\1[ \t]*$", "", texto, flags=re.S | re.M)
    texto = re.sub(r"`[^`\n]*`", "", texto)
    for alvo in re.findall(r"\]\(\s*<?([^)\s>]+)", texto):
        if re.match(r"([a-z][a-z0-9+.-]*:|#|/)", alvo, re.I):
            continue
        caminho = os.path.normpath(os.path.join(os.path.dirname(md), urllib.parse.unquote(alvo.split("#")[0])))
        if not os.path.exists(caminho):
            quebrado(f"link quebrado em {md}: {alvo}")
            continue
        desenho = re.fullmatch(r"(docs/arquitetura/diagramas/.+?)(\.escuro)?\.(mmd|svg)", caminho)
        if desenho:
            svg = desenho.group(1) + ".escuro.svg"
            print(svg) if os.path.isfile(svg) else quebrado(f"desenho sem SVG em {md}: {svg}")
sys.exit(1 if falhou else 0)'
}

faltando=0
COPIAR_DO_LEITOR="$(leituras)" || faltando=1
while IFS= read -r caminho; do
  [[ -e $caminho ]] || { echo "link quebrado: $caminho" >&2; faltando=1; }
done < <(
  {
    grep -oE "(arq|pasta)\('[^']+'\)" site/guia.js | sed -E "s/^(arq|pasta)\('([^']+)'\)$/\2/"
    evidencias
  } | sort -u
)
[[ $faltando -eq 0 ]] || exit 1

rm -rf "$SAIDA"
mkdir -p "$SAIDA/repo"
cp site/index.html site/estilo.css site/guia.js "$SAIDA/"

{ echo docs/evidencias/; evidencias; echo "$COPIAR_DO_LEITOR"; } | sort -u | while IFS= read -r caminho; do
  mkdir -p "$SAIDA/repo/$(dirname "$caminho")"
  cp -R "${caminho%/}" "$SAIDA/repo/$(dirname "$caminho")/"
done

(cd "$SAIDA/repo" && find . -type f ! -name indice.json | sed 's#^\./##' | sort | python3 -c 'import json,sys; print(json.dumps([l.strip() for l in sys.stdin]))') > "$SAIDA/repo/indice.json"
touch "$SAIDA/.nojekyll"
echo "guia montado em $SAIDA/ ($(du -sh "$SAIDA" | cut -f1))"
