#!/usr/bin/env bash
# Prova que o mapeamento EF Core de ef/Contextos.cs gera o mesmo catalogo que ddl/*.sql.
# Os dois bancos recebem os mesmos ddl/*_gatilhos.sql, que a migracao Gatilhos tambem le.
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p saida
PG="docker exec -i corredor-dados psql -U postgres -v ON_ERROR_STOP=1 -q"
(cd ef && dotnet build -v q -nologo >/dev/null && dotnet run --no-build -- gerar >/dev/null)
$PG -c "DROP DATABASE IF EXISTS ef" -c "CREATE DATABASE ef" -c "DROP DATABASE IF EXISTS mao" -c "CREATE DATABASE mao"
for f in motor cadastro guarda; do
	$PG -d ef < ef/gerado/$f.sql
	$PG -d mao < ddl/$f.sql
done
for f in motor cadastro guarda; do
	$PG -d ef < ddl/${f}_gatilhos.sql
	$PG -d mao < ddl/${f}_gatilhos.sql
done
$PG -d ef -At < catalogo.sql > saida/catalogo-ef.txt
$PG -d mao -At < catalogo.sql > saida/catalogo-mao.txt
if diff saida/catalogo-mao.txt saida/catalogo-ef.txt; then
	echo "catalogos iguais: $(wc -l < saida/catalogo-mao.txt | tr -d ' ') linhas, $(grep -c '^comentario' saida/catalogo-mao.txt) comentarios de classificacao"
else
	echo "catalogos DIFERENTES"; exit 1
fi
