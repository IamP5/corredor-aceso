#!/usr/bin/env bash
# Checagens estáticas de infra/ e tools/. O job `infra` do CI roda este script.
# Uso: tools/conferir-infra.sh
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$RAIZ"
# shellcheck source=../infra/config/corredor.conf.padrao
source infra/config/corredor.conf.padrao

scripts=()
while IFS= read -r arquivo; do scripts+=("$arquivo"); done < <(
  find infra tools -type f \( -name '*.sh' -o -path 'infra/bin/*' \) | sort
)
for arquivo in "${scripts[@]}"; do bash -n "$arquivo"; done
shellcheck "${scripts[@]}"
echo "ok: ${#scripts[@]} scripts passam em bash -n e shellcheck"

grep -q "github.com/$CORREDOR_REPO_GH.git" infra/cloud-init/corredor.yaml || {
  echo "infra/cloud-init/corredor.yaml não clona $CORREDOR_REPO_GH (ver infra/config/corredor.conf.padrao)" >&2
  exit 1
}
echo "ok: o cloud-init clona $CORREDOR_REPO_GH"

if grep -rIl $'\r' infra tools >/dev/null; then
  echo "há arquivo com fim de linha CRLF em infra/ ou tools/:" >&2
  grep -rIl $'\r' infra tools >&2
  exit 1
fi
echo "ok: nenhum arquivo com CRLF"
