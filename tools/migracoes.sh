#!/usr/bin/env bash
# Gera um script SQL idempotente por módulo a partir das migrações do EF Core.
# O CI aplica estes scripts duas vezes em um Postgres de teste. O deploy aplica
# os mesmos scripts na VM. A VM não precisa do SDK do .NET.
# Um módulo sem pasta Migrations ainda não tem banco e fica de fora, sem erro.
# Uso: tools/migracoes.sh <pasta-de-saida>
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SAIDA="${1:?informe a pasta de saída}"
# shellcheck source=../infra/config/corredor.conf.padrao
source "$RAIZ/infra/config/corredor.conf.padrao"

mkdir -p "$SAIDA"
SAIDA="$(cd "$SAIDA" && pwd)"
cd "$RAIZ"
dotnet tool restore >/dev/null
for modulo in $MODULOS_BD; do
  nome="$(printf '%s' "${modulo:0:1}" | tr '[:lower:]' '[:upper:]')${modulo:1}"
  if [[ -z "$(find "api/src/Corredor.$nome" -type d -name Migrations 2>/dev/null)" ]]; then
    echo "módulo $modulo sem migrações: nenhum script gerado"
    continue
  fi
  dotnet ef migrations script --idempotent --no-transactions \
    --project "api/src/Corredor.$nome" --startup-project api/src/Corredor.Api \
    --context "${nome}DbContext" --configuration Release --output "$SAIDA/$modulo.sql"
  echo "migrações de $modulo em $SAIDA/$modulo.sql"
done
