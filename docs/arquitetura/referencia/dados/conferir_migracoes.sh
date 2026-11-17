#!/usr/bin/env bash
# Prova o caminho das migracoes que o deploy usa:
#  1. as migracoes de ef/Migrations batem com o modelo (has-pending-model-changes);
#  2. dotnet ef migrations script --idempotent --no-transactions gera um SQL por contexto;
#  3. os papeis saem do bd-modulo.sql da infra, e cada SQL roda como corredor_<modulo>_migra,
#     com psql --single-transaction, depois de um pg_dump, duas vezes;
#  4. o banco migrado tem o mesmo catalogo de ddl/*.sql, com gatilhos e comentarios;
#  5. um erro no fim do script nao deixa nada aplicado, e sem --no-transactions deixaria;
#  6. a conferencia de migracao aditiva passa nos scripts gerados e recusa um DROP COLUMN.
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p saida/migracoes
C=corredor-dados
PG="docker exec -i $C psql -U postgres -v ON_ERROR_STOP=1 -q"
BD_MODULO=../repo/infra/config/bd-modulo.sql
dotnet tool restore >/dev/null
cd ef
for c in Motor Cadastro Guarda; do
	dotnet ef migrations has-pending-model-changes --context ${c}DbContext >/dev/null \
		|| { echo "o modelo de $c mudou sem migracao"; exit 1; }
	m=$(echo $c | tr A-Z a-z)
	dotnet ef migrations script --idempotent --no-transactions --no-build --context ${c}DbContext \
		--output ../saida/migracoes/$m.sql >/dev/null
done
cd ..
echo "migracoes: modelo e migracoes conferem nos 3 contextos; SQL idempotente em saida/migracoes/"

papeis() {
	docker cp "$BD_MODULO" $C:/tmp/bd-modulo.sql >/dev/null
	for m in motor cadastro guarda; do
		$PG -d "$1" -v banco="$1" -v esquema=$m -v dono=corredor_${m}_migra -v senha_dono=corredor \
			-v papel=corredor_$m -v senha=corredor -f /tmp/bd-modulo.sql
	done
}
aplicar_arquivo() {
	docker cp saida/migracoes/$2.sql $C:/tmp/$2.sql >/dev/null
	docker exec -e PGPASSWORD=corredor $C psql -X -q -v ON_ERROR_STOP=1 --single-transaction \
		-h 127.0.0.1 -U corredor_$3_migra -d "$1" -c 'SET client_min_messages = warning' -f /tmp/$2.sql
}
aplicar() { aplicar_arquivo "$1" "$2" "$2"; }

$PG -c "DROP DATABASE IF EXISTS migrado" -c "CREATE DATABASE migrado"
papeis migrado
for rodada in 1 2; do
	docker exec $C pg_dump -U postgres -Fc -f /tmp/antes-da-rodada-$rodada.dump migrado
	for m in motor cadastro guarda; do aplicar migrado $m; done
done
$PG -d migrado -At < catalogo.sql > saida/catalogo-migrado.txt
diff saida/catalogo-mao.txt saida/catalogo-migrado.txt
echo "migracoes: 2 rodadas como corredor_<modulo>_migra, depois de pg_dump; catalogo igual ao DDL ($(wc -l < saida/catalogo-migrado.txt | tr -d ' ') linhas); historico em $($PG -d migrado -Atc "select string_agg(table_schema || '.' || table_name, ', ' order by table_schema) from information_schema.tables where table_name = '__EFMigrationsHistory'")"

$PG -c "DROP DATABASE IF EXISTS falha" -c "CREATE DATABASE falha"
papeis falha
{ cat saida/migracoes/motor.sql; echo "SELECT 1/0;"; } > saida/migracoes/motor-com-erro.sql
if aplicar_arquivo falha motor-com-erro motor 2>/dev/null; then echo "FALHOU: o script com erro foi aceito"; exit 1; fi
[ "$($PG -d falha -Atc "select count(*) from information_schema.tables where table_schema = 'motor'")" = 0 ] \
	|| { echo "FALHOU: --single-transaction deixou tabela criada"; exit 1; }
echo "migracoes: script com erro no fim nao deixou nenhuma tabela em motor"
(cd ef && dotnet ef migrations script --idempotent --no-build --context MotorDbContext --output ../saida/migracoes/motor-com-transacoes.sql >/dev/null)
{ cat saida/migracoes/motor-com-transacoes.sql; echo "SELECT 1/0;"; } > saida/migracoes/motor-com-erro.sql
$PG -c "DROP DATABASE IF EXISTS falha" -c "CREATE DATABASE falha"
papeis falha
aplicar_arquivo falha motor-com-erro motor >/dev/null 2>&1 || true
sobrou=$($PG -d falha -Atc "select count(*) from information_schema.tables where table_schema = 'motor'")
[ "$sobrou" -gt 0 ] || { echo "FALHOU: o contraste sem --no-transactions nao deixou tabela"; exit 1; }
echo "migracoes: sem --no-transactions, o mesmo erro deixou $sobrou tabelas em motor, porque o COMMIT do script fecha a transacao do psql"
rm saida/migracoes/motor-com-erro.sql saida/migracoes/motor-com-transacoes.sql

for m in motor cadastro guarda; do ./conferir_migracao_aditiva.sh --sql saida/migracoes/$m.sql >/dev/null; done
printf 'ALTER TABLE motor.evento_sensor DROP COLUMN bufferizado;\n' > saida/migracoes/destrutiva.sql
if ./conferir_migracao_aditiva.sh --sql saida/migracoes/destrutiva.sql >/dev/null 2>&1; then echo "FALHOU: DROP COLUMN passou"; exit 1; fi
rm saida/migracoes/destrutiva.sql
echo "migracoes: conferencia aditiva aceita os 3 scripts gerados e recusa DROP COLUMN"
