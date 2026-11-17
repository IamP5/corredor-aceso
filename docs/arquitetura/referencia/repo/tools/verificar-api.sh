#!/usr/bin/env bash
# Verificação da API, igual no notebook, no ci.yml e no release.yml:
# compila, gera os scripts de migração, aplica duas vezes em um Postgres de
# teste e roda os testes. Sem CORREDOR_BD_TESTE, pula a parte do banco.
# Cada <modulo>.sql roda como corredor_<modulo>_migra, com os papéis criados pelo
# mesmo infra/config/bd-modulo.sql da VM. O CI prova o caminho que o deploy usa.
# Uso: CORREDOR_BD_TESTE=postgresql://postgres:postgres@localhost:5432/corredor_ci tools/verificar-api.sh
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$RAIZ"
# shellcheck source=../infra/config/corredor.conf.padrao
source infra/config/corredor.conf.padrao

dotnet build api/Corredor.slnx --configuration Release --nologo

if [[ -n ${CORREDOR_BD_TESTE:-} ]]; then
  saida="$(mktemp -d)"
  bash tools/migracoes.sh "$saida"
  banco="${CORREDOR_BD_TESTE##*/}"
  for m in $MODULOS_BD; do
    psql "$CORREDOR_BD_TESTE" -X -q -v ON_ERROR_STOP=1 -v banco="$banco" -v esquema="$m" \
      -v dono="corredor_${m}_migra" -v senha_dono=ci -v papel="corredor_$m" -v senha=ci \
      -f infra/config/bd-modulo.sql
  done
  for rodada in 1 2; do
    for sql in "$saida"/*.sql; do
      m="$(basename "$sql" .sql)"
      psql "postgresql://corredor_${m}_migra:ci@${CORREDOR_BD_TESTE#*@}" -X -q -v ON_ERROR_STOP=1 \
        --single-transaction -c 'SET client_min_messages = warning' -f "$sql"
      echo "rodada $rodada: $(basename "$sql") aplicado como corredor_${m}_migra"
    done
  done
else
  echo "CORREDOR_BD_TESTE não definido: migrações não aplicadas"
fi

dotnet test api/Corredor.slnx --configuration Release --no-build --nologo
