#!/usr/bin/env bash
# Auditoria: quem rodou qual script, quando, com quais argumentos e como terminou.
# Uma linha por evento em /var/log/corredor/auditoria.log e no journal.
# A saída completa de cada execução fica em /var/log/corredor/execucao/.

auditoria_registrar() {
  local evento=$1 linha
  shift
  linha="$(date -Is) id=$AUD_ID script=$AUD_SCRIPT usuario=$AUD_USUARIO ambiente=${CORREDOR_AMBIENTE:-?} evento=$evento $*"
  printf '%s\n' "$linha" >>"$CORREDOR_LOG/auditoria.log"
  logger -t corredor-auditoria -- "$linha" 2>/dev/null || true
}

auditoria_fim() {
  local codigo=$1
  auditoria_registrar fim "codigo=$codigo duracao_s=$(($(date +%s) - AUD_INICIO)) mudancas=${MUDANCAS:-0}"
}

auditoria_iniciar() {
  AUD_SCRIPT="$(basename "$0")"
  AUD_ID="$(date +%Y%m%dT%H%M%S)-$$"
  AUD_INICIO="$(date +%s)"
  AUD_USUARIO="${SUDO_USER:-$(id -un)}"
  install -d -m 0755 "$CORREDOR_LOG"
  install -d -m 0750 "$CORREDOR_LOG/execucao"
  touch "$CORREDOR_LOG/auditoria.log"
  chmod 0640 "$CORREDOR_LOG/auditoria.log"
  exec > >(tee -a "$CORREDOR_LOG/execucao/$AUD_ID-${AUD_SCRIPT%.sh}.log") 2>&1
  auditoria_registrar inicio "args=\"$*\""
  trap 'auditoria_registrar erro "linha=$LINENO comando=\"$BASH_COMMAND\""' ERR
  trap 'auditoria_fim $?' EXIT
}
