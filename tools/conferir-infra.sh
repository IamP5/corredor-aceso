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

dia="$(sed -n 's/^Environment=CORREDOR_DIA_DAS_VARIAVEIS=//p' infra/systemd/corredor-ml.service)"
if [[ -z $dia ]] || ! grep -q "^$dia," ml/dados/processados/clima_diario.csv; then
  echo "infra/systemd/corredor-ml.service precisa de CORREDOR_DIA_DAS_VARIAVEIS com um dia de ml/dados/processados/clima_diario.csv" >&2
  exit 1
fi
echo "ok: o job de ML da VM usa o clima de $dia, que existe nos dados processados"

if grep -rIl $'\r' infra tools >/dev/null; then
  echo "há arquivo com fim de linha CRLF em infra/ ou tools/:" >&2
  grep -rIl $'\r' infra tools >&2
  exit 1
fi
echo "ok: nenhum arquivo com CRLF"
