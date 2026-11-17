#!/usr/bin/env bash
# Rubrica: script de monitoramento que gera alertas de CPU, disco e memória. 2 pontos.
# Mede a cada ciclo, grava uma linha de métricas e gera alerta quando um valor
# passa do limite ou um serviço para. O monitor só avisa: quem religa serviço é
# o systemd. Roda como corredor-monitor.service.
# Uso manual: monitor.sh --uma-vez   (um ciclo, saída na tela)
set -uo pipefail

INTERVALO_S="${MONITOR_INTERVALO_S:-15}"
LIMITE_CPU="${MONITOR_LIMITE_CPU:-85}"
LIMITE_MEM="${MONITOR_LIMITE_MEM:-85}"
LIMITE_DISCO="${MONITOR_LIMITE_DISCO:-80}"
CICLOS_CPU="${MONITOR_CICLOS_CPU:-2}"
REPETIR_A_CADA="${MONITOR_REPETIR_A_CADA:-20}"
DISCOS="${MONITOR_DISCOS:-/}"
SERVICOS="${MONITOR_SERVICOS:-mosquitto nginx}"
API_SAUDE="${MONITOR_API_SAUDE:-http://127.0.0.1:5000/healthz/ready}"
DIR_LOG="${MONITOR_DIR_LOG:-/var/log/corredor/monitor}"
UMA_VEZ=0
[[ ${1:-} == --uma-vez ]] && UMA_VEZ=1

declare -A ESTADO=() SEGUIDOS=() CICLOS_EM_ALERTA=()
CPU_ANTES=""

agora() { date -Is; }

# Alerta vai para alertas.log e para o journal. O prefixo <4> é a prioridade
# "warning" do journald, então `journalctl -u corredor-monitor -p warning` filtra.
emitir() {
  local nivel=$1 texto=$2
  if ((UMA_VEZ)); then
    echo "$nivel $texto"
    return 0
  fi
  echo "$(agora) $nivel $texto" >>"$DIR_LOG/alertas.log"
  if [[ $nivel == ALERTA ]]; then echo "<4>ALERTA $texto"; else echo "<5>$nivel $texto"; fi
}

# Máquina de estados por chave: OK -> ALERTA quando o problema dura `minimo`
# ciclos, ALERTA -> OK quando some. Em ALERTA, repete o aviso de tempos em tempos.
avaliar() {
  local chave=$1 ruim=$2 texto=$3 minimo=${4:-1}
  if ((ruim)); then
    SEGUIDOS[$chave]=$((${SEGUIDOS[$chave]:-0} + 1))
    if [[ ${ESTADO[$chave]:-OK} == OK ]] && ((${SEGUIDOS[$chave]} >= minimo)); then
      ESTADO[$chave]=ALERTA
      CICLOS_EM_ALERTA[$chave]=0
      emitir ALERTA "$texto"
    elif [[ ${ESTADO[$chave]:-OK} == ALERTA ]]; then
      CICLOS_EM_ALERTA[$chave]=$((${CICLOS_EM_ALERTA[$chave]} + 1))
      if ((${CICLOS_EM_ALERTA[$chave]} % REPETIR_A_CADA == 0)); then emitir ALERTA "$texto (continua)"; fi
    fi
  else
    SEGUIDOS[$chave]=0
    if [[ ${ESTADO[$chave]:-OK} == ALERTA ]]; then
      ESTADO[$chave]=OK
      emitir NORMALIZADO "$chave voltou ao normal"
    fi
  fi
}

# Uso de CPU entre duas leituras de /proc/stat, em porcento.
medir_cpu() {
  local agora_stat total ocioso total_antes ocioso_antes
  agora_stat="$(awk '/^cpu / {print $2+$3+$4+$5+$6+$7+$8+$9, $5+$6}' /proc/stat)"
  if [[ -z $CPU_ANTES ]]; then
    CPU_ANTES="$agora_stat"
    sleep 1
    agora_stat="$(awk '/^cpu / {print $2+$3+$4+$5+$6+$7+$8+$9, $5+$6}' /proc/stat)"
  fi
  read -r total ocioso <<<"$agora_stat"
  read -r total_antes ocioso_antes <<<"$CPU_ANTES"
  CPU_ANTES="$agora_stat"
  if ((total == total_antes)); then
    echo 0
  else
    echo $((100 - (ocioso - ocioso_antes) * 100 / (total - total_antes)))
  fi
}

medir_memoria() {
  awk '/^MemTotal:/ {t=$2} /^MemAvailable:/ {d=$2} END {printf "%d", (t-d)*100/t}' /proc/meminfo
}

medir_disco() {
  df -P "$1" | awk 'NR==2 {sub("%","",$5); print $5}'
}

rotulo() { if (($1)); then echo "$2"; else echo ok; fi; }

ciclo() {
  local cpu mem uso disco s linha ruim
  cpu="$(medir_cpu)"
  mem="$(medir_memoria)"
  linha="cpu=${cpu}% mem=${mem}%"
  avaliar cpu $((cpu >= LIMITE_CPU)) "CPU em ${cpu}% (limite ${LIMITE_CPU}%)" "$CICLOS_CPU"
  avaliar memoria $((mem >= LIMITE_MEM)) "memória em ${mem}% (limite ${LIMITE_MEM}%)"
  for disco in $DISCOS; do
    uso="$(medir_disco "$disco")"
    linha+=" disco:${disco}=${uso}%"
    avaliar "disco:$disco" $((uso >= LIMITE_DISCO)) "disco $disco em ${uso}% (limite ${LIMITE_DISCO}%)"
  done
  for s in $SERVICOS; do
    ruim=0
    systemctl is-active --quiet "$s" || ruim=1
    linha+=" $s=$(rotulo "$ruim" parado)"
    avaliar "servico:$s" "$ruim" "serviço $s parado"
  done
  ruim=0
  pg_isready -q -h 127.0.0.1 2>/dev/null || ruim=1
  linha+=" postgres=$(rotulo "$ruim" parado)"
  avaliar servico:postgres "$ruim" "Postgres não aceita conexão"
  if [[ -e /opt/corredor/current ]]; then
    ruim=0
    curl -fsS --max-time 3 "$API_SAUDE" >/dev/null 2>&1 || ruim=1
    linha+=" api=$(rotulo "$ruim" falhou)"
    avaliar api "$ruim" "API não está pronta (processo, banco ou MQTT)"
  fi
  if ((UMA_VEZ)); then
    echo "METRICAS $linha"
  else
    echo "$(agora) $linha" >>"$DIR_LOG/monitor.log"
  fi
}

if ((UMA_VEZ)); then
  CICLOS_CPU=1
  ciclo
  exit 0
fi

mkdir -p "$DIR_LOG"
trap 'echo "<5>monitor encerrado"; exit 0' TERM INT
echo "<5>monitor iniciado: a cada ${INTERVALO_S}s, limites cpu=${LIMITE_CPU}% mem=${LIMITE_MEM}% disco=${LIMITE_DISCO}%"
[[ -n ${NOTIFY_SOCKET:-} ]] && systemd-notify --ready
while true; do
  ciclo
  # Avisa o systemd que o laço está vivo. Sem este aviso por 60 s, o WatchdogSec
  # da unit mata e religa o monitor, mesmo que o processo ainda exista.
  [[ -n ${NOTIFY_SOCKET:-} ]] && systemd-notify WATCHDOG=1
  sleep "$INTERVALO_S" &
  wait $!
done
