#!/usr/bin/env bash
# Recusa migracao destrutiva ate 19/11. Le o SQL que as migracoes novas geram, entao pega tambem
# AlterColumn que muda tipo e o DROP escrito dentro de migrationBuilder.Sql.
#   conferir_migracao_aditiva.sh <ref-base>   no CI: confere as migracoes que entraram depois de <ref-base>
#   conferir_migracao_aditiva.sh --sql <arq>  confere um script SQL ja gerado
set -euo pipefail
DESTRUTIVO='DROP (TABLE|COLUMN|INDEX|SCHEMA|TRIGGER|FUNCTION)|RENAME|ALTER COLUMN .* (TYPE|SET NOT NULL)'

conferir_sql() {
	if grep -n -E -i "$DESTRUTIVO" "$1"; then
		echo "comando destrutivo em $1" >&2
		return 1
	fi
	echo "ok: $1 so cria"
}

if [ "${1:-}" = "--sql" ]; then
	conferir_sql "$2"
	exit
fi

base="${1:?informe a ref base, como origin/main}"
falhou=0
saida="$(mktemp -d)"
for ctx in Motor Cadastro Guarda; do
	pasta="api/src/Corredor.$ctx/Infra/Migrations"
	if git diff --name-status "$base"...HEAD -- "$pasta" | grep -v 'ModelSnapshot.cs' | grep -E '^[MDR]'; then
		echo "migracao que ja estava em $base foi alterada em $pasta" >&2
		falhou=1
	fi
	ultima=$(git ls-tree -r --name-only "$base" -- "$pasta" | sed -n 's#.*/\([0-9]\{14\}_[^./]*\)\.cs$#\1#p' | sort | tail -1)
	dotnet ef migrations script ${ultima:+"$ultima"} --context "${ctx}DbContext" \
		--project "api/src/Corredor.$ctx" --startup-project api/src/Corredor.Api --output "$saida/$ctx.sql"
	conferir_sql "$saida/$ctx.sql" || falhou=1
done
exit $falhou
