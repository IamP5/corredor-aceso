#!/usr/bin/env bash
# Confere que um pacote de release tem o que o infra/deploy.sh e o Nginx esperam.
# Roda no ci.yml (pacote x64 de teste) e no release.yml (os dois pacotes publicados).
# Uso: tools/conferir-pacote.sh dist/corredor-linux-x64.tar.gz v0.3.0
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACOTE="${1:?informe o pacote, por exemplo dist/corredor-linux-x64.tar.gz}"
TAG="${2:?informe a tag que o pacote deve ter}"
# shellcheck source=../infra/config/corredor.conf.padrao
source "$RAIZ/infra/config/corredor.conf.padrao"

conteudo="$(tar -tzf "$PACOTE")"
exigidos=(./VERSION ./COMMIT ./api/Corredor.Api)
for m in $MODULOS_BD; do
  nome="$(printf '%s' "${m:0:1}" | tr '[:lower:]' '[:upper:]')${m:1}"
  if [[ -n "$(find "$RAIZ/api/src/Corredor.$nome" -type d -name Migrations 2>/dev/null)" ]]; then
    exigidos+=("./migracoes/$m.sql")
  fi
done
if [[ -f $RAIZ/api/src/Corredor.Guarda/painel/index.html ]]; then exigidos+=(./painel/index.html); fi

faltam=0
for item in "${exigidos[@]}"; do
  if ! grep -qxF "$item" <<<"$conteudo"; then
    echo "falta $item em $PACOTE" >&2
    faltam=1
  fi
done
versao="$(tar -xOzf "$PACOTE" ./VERSION)"
if [[ $versao != "$TAG" ]]; then
  echo "VERSION do pacote diz '$versao', esperado '$TAG'" >&2
  faltam=1
fi
((faltam == 0)) || exit 1
echo "ok: $PACOTE tem ${exigidos[*]#./} e VERSION $TAG"
