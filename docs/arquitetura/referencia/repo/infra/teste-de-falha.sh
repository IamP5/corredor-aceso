#!/usr/bin/env bash
# Rubrica: teste de falha e funcionamento completo. 2 pontos.
# Provoca cada falha de propósito e confere que a VM se recupera sozinha.
# Uso: teste-de-falha.sh [broker] [api] [monitor] [carga] [memoria] [disco] [travado]
# Sem argumentos, roda: broker api monitor carga memoria disco.
# Pode rodar de novo: cada cenário devolve a VM ao estado em que a encontrou, e a
# limpeza roda também quando o script é interrompido.
# shellcheck disable=SC2329  # funções chamadas por meio de `esperar` e do trap
set -Eeuo pipefail
INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/comum.sh
source "$INFRA_DIR/lib/comum.sh"
# shellcheck source=lib/auditoria.sh
source "$INFRA_DIR/lib/auditoria.sh"
exigir_root
carregar_config
auditoria_iniciar "$@"

# Limites que o monitor usa nesta VM: os padrões do monitor.sh, ou os de monitor.env.
MONITOR_LIMITE_MEM=85
MONITOR_LIMITE_DISCO=80
if [[ -f $CORREDOR_ETC/monitor.env ]]; then
  # shellcheck source=/dev/null
  source "$CORREDOR_ETC/monitor.env"
fi
# Se o host exigir autenticação na linha do tempo, ponha TESTE_AUTORIZACAO="Bearer ..."
# em /etc/corredor/teste-de-falha.env.
TESTE_AUTORIZACAO=""
if [[ -f $CORREDOR_ETC/teste-de-falha.env ]]; then
  # shellcheck source=/dev/null
  source "$CORREDOR_ETC/teste-de-falha.env"
fi

ALERTAS="$CORREDOR_LOG/monitor/alertas.log"
TOPICO="corredor/v1/$CASA_TESTE_TOKEN/evento"
ARQ_DISCO=/var/tmp/corredor-teste-disco.img
RESULTADOS=()
FALHOU=0

limpar() {
  pkill -f 'stress-ng --vm' 2>/dev/null || true
  rm -f "$ARQ_DISCO"
  systemctl is-active --quiet mosquitto || systemctl start mosquitto || true
}
trap 'codigo=$?; limpar; auditoria_fim "$codigo"' EXIT

titulo() { printf '\n== %s ==\n' "$*"; }
passou() {
  RESULTADOS+=("PASSOU  $*")
  printf '  [PASSOU] %s\n' "$*"
}
pulado() {
  RESULTADOS+=("PULADO  $*")
  printf '  [PULADO] %s\n' "$*"
}
falhou() {
  FALHOU=1
  RESULTADOS+=("FALHOU  $*")
  printf '  [FALHOU] %s\n' "$*"
}

# esperar SEGUNDOS comando...: repete o comando até dar certo ou o tempo acabar.
esperar() {
  local limite=$1
  shift
  for _ in $(seq 1 "$limite"); do
    if "$@" >/dev/null 2>&1; then return 0; fi
    sleep 1
  done
  return 1
}

linhas_de_alerta() { wc -l <"$ALERTAS" 2>/dev/null || echo 0; }
alerta_novo() { tail -n +"$(($1 + 1))" "$ALERTAS" 2>/dev/null | grep -q "$2"; }
primeiro_alerta() { tail -n +"$(($1 + 1))" "$ALERTAS" | grep -m1 "$2"; }
pid_de() { systemctl show -p MainPID --value "$1"; }
religado() { systemctl is-active --quiet "$1" && [[ "$(pid_de "$1")" != "$2" && "$(pid_de "$1")" != 0 ]]; }
api_pronta() { curl -fsS --max-time 2 "http://127.0.0.1:$API_PORTA/healthz/ready"; }
linha_do_tempo() {
  local cabecalho=()
  if [[ -n $TESTE_AUTORIZACAO ]]; then cabecalho=(-H "Authorization: $TESTE_AUTORIZACAO"); fi
  curl -fsS --max-time 3 "${cabecalho[@]}" "http://127.0.0.1:$API_PORTA/api/casas/$CASA_TESTE_TOKEN/linha-do-tempo"
}

evento_json() {
  printf '{"seq":%s,"ts":"%s","tipo":"MOVIMENTO","tempC":31.5,"tensaoV":126.8,"bufferizado":%s}' "$1" "$2" "$3"
}
publicar() {
  mosquitto_pub -h 127.0.0.1 -q 1 -i corredor-teste-de-falha -t "$TOPICO" -m "$(evento_json "$1" "$2" "$3")" 2>/dev/null
}

# Broker local parado. O script faz o papel de um publicador local: tenta publicar,
# guarda o que não saiu e, quando o broker volta, reenvia duas vezes com a hora
# original e bufferizado: true. O Motor grava cada evento uma vez só, pela chave
# (casaToken, seq, ts). O nó do Wokwi não entra aqui: ele publica com QoS 0 no
# broker público, e nada garante que um evento dele sobreviva a esta parada.
cenario_broker() {
  local antes base seq ts buffer recebidos assinante entregues=0 n=3 resposta
  titulo "Mosquitto parado: o monitor alerta, o publicador local guarda e reenvia, o Motor não duplica"
  antes="$(linhas_de_alerta)"
  base="$(date +%s)"
  buffer="$(mktemp)"
  recebidos="$(mktemp)"
  systemctl stop mosquitto
  echo "  mosquitto parado"
  for seq in $(seq "$((base + 1))" "$((base + n))"); do
    ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    if publicar "$seq" "$ts" false; then
      echo "  evento $seq publicado (não era esperado)"
    else
      echo "$seq $ts" >>"$buffer"
      echo "  evento $seq das $ts guardado no buffer"
    fi
    sleep 1
  done
  if esperar 45 alerta_novo "$antes" "ALERTA serviço mosquitto parado"; then
    passou "monitor gerou o alerta: $(primeiro_alerta "$antes" mosquitto)"
  else
    falhou "o monitor não alertou que o mosquitto parou"
  fi

  systemctl start mosquitto
  esperar 20 systemctl is-active --quiet mosquitto || true
  echo "  mosquitto religado"
  # O reenvio espera o Motor reconectar e assinar de novo: /healthz/ready só responde
  # 200 com o cliente MQTT conectado. Sem isto, o reenvio chega antes da assinatura.
  if [[ -L $CORREDOR_RAIZ/current ]]; then
    if esperar 30 api_pronta; then echo "  Motor reconectado ao broker"; else echo "  Motor ainda sem broker depois de 30 s"; fi
  fi
  mosquitto_sub -h 127.0.0.1 -q 1 -t "$TOPICO" -C "$((2 * n))" -W 25 >"$recebidos" 2>/dev/null &
  assinante=$!
  sleep 1
  for rodada in 1 2; do
    while read -r seq ts; do
      if esperar 10 publicar "$seq" "$ts" true; then entregues=$((entregues + 1)); fi
    done <"$buffer"
    echo "  rodada $rodada do reenvio feita"
  done
  wait "$assinante" || true
  if ((entregues == 2 * n)) && (($(grep -c '"bufferizado":true' "$recebidos") == 2 * n)); then
    passou "$n eventos do buffer reenviados duas vezes com a hora original e bufferizado: true"
  else
    falhou "só $entregues de $((2 * n)) reenvios chegaram ao broker"
  fi

  if [[ -L $CORREDOR_RAIZ/current ]]; then
    motor_gravou_uma_vez() {
      resposta="$(linha_do_tempo)" || return 1
      while read -r seq ts; do
        (($(grep -Eo "\"seq\"[[:space:]]*:[[:space:]]*${seq}[,}[:space:]]" <<<"$resposta" | wc -l) == 1)) || return 1
      done <"$buffer"
    }
    if esperar 45 motor_gravou_uma_vez; then
      passou "o Motor gravou cada um dos $n eventos uma vez só (linha do tempo de $CASA_TESTE_TOKEN)"
    else
      falhou "a linha do tempo de $CASA_TESTE_TOKEN não mostrou cada evento exatamente uma vez em 45 s"
    fi
  else
    pulado "conferência no Motor: API sem versão implantada"
  fi
  if esperar 45 alerta_novo "$antes" "NORMALIZADO servico:mosquitto"; then
    passou "monitor registrou a volta do mosquitto"
  else
    falhou "o monitor não registrou a volta do mosquitto"
  fi
  rm -f "$buffer" "$recebidos"
}

cenario_api() {
  local pid inicio
  titulo "API morta com kill -9: o systemd religa"
  if [[ ! -L $CORREDOR_RAIZ/current ]]; then
    pulado "API sem versão implantada"
    return 0
  fi
  pid="$(pid_de corredor-api)"
  inicio="$(date +%s)"
  kill -9 "$pid"
  echo "  processo $pid morto"
  if esperar 60 religado corredor-api "$pid" && esperar 60 api_pronta; then
    passou "API religada e pronta em $(($(date +%s) - inicio)) s, novo processo $(pid_de corredor-api)"
  else
    falhou "a API não voltou em 60 s"
  fi
}

cenario_monitor() {
  local pid inicio
  titulo "Monitor morto com kill -9: o systemd religa (Restart=always)"
  pid="$(pid_de corredor-monitor)"
  inicio="$(date +%s)"
  kill -9 "$pid"
  echo "  processo $pid morto"
  if esperar 30 religado corredor-monitor "$pid"; then
    passou "monitor religado em $(($(date +%s) - inicio)) s, novo processo $(pid_de corredor-monitor)"
  else
    falhou "o monitor não voltou em 30 s"
  fi
}

# Monitor travado, mas vivo: só o watchdog percebe. Demora mais de 60 s.
cenario_travado() {
  local pid
  titulo "Monitor travado com kill -STOP: o watchdog do systemd mata e religa"
  pid="$(pid_de corredor-monitor)"
  kill -STOP "$pid"
  echo "  processo $pid congelado. Aguardando o WatchdogSec de 60 s."
  if esperar 100 religado corredor-monitor "$pid"; then
    passou "watchdog religou o monitor, novo processo $(pid_de corredor-monitor)"
  else
    kill -CONT "$pid" 2>/dev/null || true
    falhou "o watchdog não religou o monitor em 100 s"
  fi
}

cenario_carga() {
  local antes pids=() n
  titulo "Carga de CPU: o monitor alerta e depois normaliza"
  antes="$(linhas_de_alerta)"
  for n in $(seq 1 "$(nproc)"); do
    timeout 120 bash -c 'while :; do :; done' &
    pids+=($!)
  done
  echo "  ${#pids[@]} processo(s) ocupando a CPU"
  if esperar 90 alerta_novo "$antes" "ALERTA CPU em"; then
    passou "monitor gerou o alerta: $(primeiro_alerta "$antes" 'ALERTA CPU')"
  else
    falhou "o monitor não alertou a CPU alta em 90 s"
  fi
  kill "${pids[@]}" 2>/dev/null || true
  wait "${pids[@]}" 2>/dev/null || true
  if esperar 60 alerta_novo "$antes" "NORMALIZADO cpu"; then
    passou "monitor registrou a CPU de volta ao normal"
  else
    falhou "o monitor não registrou a CPU de volta ao normal"
  fi
}

# Memória de verdade: o stress-ng ocupa o que falta para passar 5 pontos do limite
# do monitor, sem chegar perto do OOM killer.
cenario_memoria() {
  local antes total disp alvo_kb stress
  titulo "Memória acima de ${MONITOR_LIMITE_MEM}%: o monitor alerta e depois normaliza"
  total="$(awk '/^MemTotal:/ {print $2}' /proc/meminfo)"
  disp="$(awk '/^MemAvailable:/ {print $2}' /proc/meminfo)"
  # Mesma conta do monitor.sh. Já em alerta, o monitor não grava um ALERTA novo.
  if (((total - disp) * 100 / total >= MONITOR_LIMITE_MEM)); then
    pulado "a memória já está no limite de ${MONITOR_LIMITE_MEM}% antes do teste"
    return 0
  fi
  alvo_kb=$((total * (MONITOR_LIMITE_MEM + 5) / 100 - (total - disp)))
  if ((alvo_kb > disp - 262144)); then
    pulado "ocupar ${alvo_kb} kB deixaria menos de 256 MB livres"
    return 0
  fi
  instalar_pacotes stress-ng
  antes="$(linhas_de_alerta)"
  stress-ng --vm 1 --vm-bytes "${alvo_kb}k" --vm-keep --timeout 120s --quiet &
  stress=$!
  echo "  stress-ng ocupando $((alvo_kb / 1024)) MB"
  if esperar 60 alerta_novo "$antes" "ALERTA memória em"; then
    passou "monitor gerou o alerta: $(primeiro_alerta "$antes" 'ALERTA memória')"
  else
    falhou "o monitor não alertou a memória alta em 60 s"
  fi
  kill "$stress" 2>/dev/null || true
  wait "$stress" 2>/dev/null || true
  if esperar 45 alerta_novo "$antes" "NORMALIZADO memoria"; then
    passou "monitor registrou a memória de volta ao normal"
  else
    falhou "o monitor não registrou a memória de volta ao normal"
  fi
}

# Disco de verdade: um arquivo leva o disco / a 1 ponto acima do limite do monitor.
# O df calcula o uso sobre usado + livre, então a conta usa os mesmos números.
cenario_disco() {
  local antes usado livre alvo_kb
  titulo "Disco / acima de ${MONITOR_LIMITE_DISCO}%: o monitor alerta e depois normaliza"
  read -r usado livre < <(df --output=used,avail -k / | tail -n 1)
  # Mesma leitura do monitor.sh (df -P, que arredonda para cima). Já em alerta, o
  # monitor não grava um ALERTA novo.
  if (($(df -P / | awk 'NR==2 {sub("%","",$5); print $5}') >= MONITOR_LIMITE_DISCO)); then
    pulado "o disco / já está no limite de ${MONITOR_LIMITE_DISCO}% antes do teste"
    return 0
  fi
  alvo_kb=$(((usado + livre) * (MONITOR_LIMITE_DISCO + 1) / 100 - usado))
  if ((alvo_kb > livre - 524288)); then
    pulado "o arquivo de ${alvo_kb} kB deixaria menos de 512 MB livres"
    return 0
  fi
  antes="$(linhas_de_alerta)"
  fallocate -l "${alvo_kb}K" "$ARQ_DISCO"
  echo "  arquivo de $((alvo_kb / 1024)) MB em $ARQ_DISCO"
  if esperar 60 alerta_novo "$antes" "ALERTA disco / em"; then
    passou "monitor gerou o alerta: $(primeiro_alerta "$antes" 'ALERTA disco')"
  else
    falhou "o monitor não alertou o disco cheio em 60 s"
  fi
  rm -f "$ARQ_DISCO"
  if esperar 45 alerta_novo "$antes" "NORMALIZADO disco:/"; then
    passou "monitor registrou o disco de volta ao normal"
  else
    falhou "o monitor não registrou o disco de volta ao normal"
  fi
}

CENARIOS=("$@")
if ((${#CENARIOS[@]} == 0)); then CENARIOS=(broker api monitor carga memoria disco); fi
for c in "${CENARIOS[@]}"; do
  case "$c" in
    broker) cenario_broker ;;
    api) cenario_api ;;
    monitor) cenario_monitor ;;
    carga) cenario_carga ;;
    memoria) cenario_memoria ;;
    disco) cenario_disco ;;
    travado) cenario_travado ;;
    *) erro "cenário desconhecido: $c" ;;
  esac
done

titulo "Resumo do teste de falha"
printf '  %s\n' "${RESULTADOS[@]}"
exit "$FALHOU"
