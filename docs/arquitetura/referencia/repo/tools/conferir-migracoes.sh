#!/usr/bin/env bash
# Recusa migração destrutiva do EF Core até o fim da entrega: apagar ou renomear
# tabela ou coluna no Up() de qualquer migração. Só migração aditiva deixa a versão
# anterior do binário rodar no schema novo, e é isso que torna o rollback seguro
# sem mexer no banco. O Down() fica de fora, porque desfazer uma criação é apagar.
# Uso: tools/conferir-migracoes.sh   (o job api do ci.yml roda este script)
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$RAIZ"
FIM_DA_REGRA="2026-11-20"

if [[ "$(date -u +%F)" > "$FIM_DA_REGRA" || "$(date -u +%F)" == "$FIM_DA_REGRA" ]]; then
  echo "regra de migração aditiva encerrada em $FIM_DA_REGRA: nada conferido"
  exit 0
fi

achados=0
total=0
while IFS= read -r -d '' arquivo; do
  total=$((total + 1))
  linhas="$(awk '/override void Up\(/ {dentro=1} /override void Down\(/ {dentro=0} dentro {print FILENAME ":" FNR ": " $0}' "$arquivo" |
    grep -E '\.(DropTable|DropColumn|RenameTable|RenameColumn)\(' || true)"
  if [[ -n $linhas ]]; then
    echo "$linhas" >&2
    achados=1
  fi
done < <(find api/src -path '*/Migrations/*.cs' ! -name '*.Designer.cs' ! -name '*ModelSnapshot.cs' -print0 2>/dev/null)

if ((achados)); then
  echo "migração destrutiva no Up(): até $FIM_DA_REGRA só entra criar tabela ou coluna que aceita nulo" >&2
  exit 1
fi
echo "ok: $total migração(ões) do EF Core, nenhuma apaga ou renomeia tabela ou coluna"
