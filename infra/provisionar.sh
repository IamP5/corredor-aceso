#!/usr/bin/env bash
# Ponto de entrada único do provisionamento. O cloud-init, o Multipass e a
# pessoa no terminal chamam só este arquivo.
# Roda as etapas em ordem e para na primeira que falhar.
# No fim, compara o InvocationID da API e do monitor com o de antes: numa segunda
# execução sem mudança, os dois ficam iguais, prova de que nada foi reiniciado.
set -Eeuo pipefail
INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/comum.sh
source "$INFRA_DIR/lib/comum.sh"
# shellcheck source=lib/auditoria.sh
source "$INFRA_DIR/lib/auditoria.sh"
exigir_root
carregar_config
auditoria_iniciar "$@"
travar provisionamento

invocacao() { systemctl show -p InvocationID --value "$1" 2>/dev/null || true; }
declare -A ANTES=()
for u in corredor-api corredor-monitor; do ANTES[$u]="$(invocacao "$u")"; done

CORREDOR_MUDANCAS_ARQ="$(mktemp)"
export CORREDOR_MUDANCAS_ARQ
for etapa in 01-rede.sh 02-app.sh 03-monitor.sh verificar.sh; do
  bash "$INFRA_DIR/$etapa"
done
MUDANCAS="$(awk '{s += $1} END {print s + 0}' "$CORREDOR_MUDANCAS_ARQ")"
rm -f "$CORREDOR_MUDANCAS_ARQ"
unset CORREDOR_MUDANCAS_ARQ

echo "== processos antes e depois =="
for u in corredor-api corredor-monitor; do
  depois="$(invocacao "$u")"
  if [[ -z $depois ]]; then
    echo "  $u: não está rodando"
  elif [[ -z ${ANTES[$u]} ]]; then
    echo "  $u: subiu nesta execução (InvocationID $depois)"
  elif [[ ${ANTES[$u]} == "$depois" ]]; then
    echo "  $u: não reiniciou (InvocationID $depois antes e depois)"
  else
    echo "  $u: reiniciou (InvocationID ${ANTES[$u]} antes, $depois depois)"
  fi
done
echo "== provisionamento concluído em $(($(date +%s) - AUD_INICIO)) s, $MUDANCAS mudança(s) =="
