#!/usr/bin/env bash
# Aplica ddl/*.sql num banco novo e roda testes.sql e testes_expurgo.sql como postgres.
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p saida
PG="docker exec -i corredor-dados psql -U postgres -v ON_ERROR_STOP=1 -q"
$PG -c "DROP DATABASE IF EXISTS teste" -c "CREATE DATABASE teste"
for f in motor cadastro guarda motor_gatilhos cadastro_gatilhos guarda_gatilhos; do $PG -d teste < ddl/$f.sql; done
docker exec corredor-dados rm -rf /tmp/dados && docker exec corredor-dados mkdir -p /tmp/dados
docker cp consultas corredor-dados:/tmp/dados/consultas >/dev/null
docker cp testes.sql corredor-dados:/tmp/dados/ >/dev/null
docker cp testes_expurgo.sql corredor-dados:/tmp/dados/ >/dev/null
docker exec -w /tmp/dados corredor-dados psql -U postgres -d teste -At -f testes.sql > saida/testes.txt
docker exec -w /tmp/dados corredor-dados psql -U postgres -d teste -At -f testes_expurgo.sql >> saida/testes.txt
if grep -q FALHOU saida/testes.txt; then grep FALHOU saida/testes.txt; exit 1; fi
echo "$(grep -c '^ok' saida/testes.txt) conferencias de SQL passaram"
