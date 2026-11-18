#!/usr/bin/env bash
# Serviço "preparar" do compose.yml: papéis, migrações e carga das 40 casas.
# Cada passo confere antes de mudar, então rodar de novo a cada "docker compose up" não muda nada.
# Para começar do zero: docker compose down -v.
set -euo pipefail
# A imagem copia este arquivo de infra/config para /config (tools/docker/api.Dockerfile).
# shellcheck source=../../infra/config/corredor.conf.padrao
source /config/corredor.conf.padrao

super() { psql "postgresql://corredor:$PGSENHA@postgres:5432/$1" -X -q -v ON_ERROR_STOP=1 "${@:2}"; }

if [[ -z "$(super postgres -At -c "SELECT 1 FROM pg_database WHERE datname = '$BANCO'")" ]]; then
  super postgres -c "CREATE DATABASE $BANCO"
fi

for m in $MODULOS_BD; do
  super "$BANCO" -v banco="$BANCO" -v esquema="$m" -v dono="corredor_${m}_migra" \
    -v senha_dono="$SENHA_MIGRA" -v papel="corredor_$m" -v senha="$SENHA_API" -f /config/bd-modulo.sql
  psql "postgresql://corredor_${m}_migra:$SENHA_MIGRA@postgres:5432/$BANCO" -X -q -v ON_ERROR_STOP=1 \
    --single-transaction -c 'SET client_min_messages = warning' -f "/migracoes/$m.sql"
  echo "módulo $m: papéis e migrações em dia"
done

cp /semente-original/casas.json /semente/casas.json
dotnet /app/Corredor.Api.dll --semear /semente/casas.json
echo "carga pronta: tokens das casas em /semente/tokens.local.json"
