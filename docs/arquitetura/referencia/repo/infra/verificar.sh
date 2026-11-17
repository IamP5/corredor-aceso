#!/usr/bin/env bash
# Confere a VM de ponta a ponta e mostra o que está no ar. Não muda nada.
# É a última etapa do provisionamento e é o `sudo corredor status`.
# Sai com código 1 se algum item falhar.
set -uo pipefail
INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/comum.sh
source "$INFRA_DIR/lib/comum.sh"
exigir_root
carregar_config

FALHAS=0
ok() { printf '  [OK]       %s\n' "$*"; }
pendente() { printf '  [PENDENTE] %s\n' "$*"; }
falha() {
  FALHAS=$((FALHAS + 1))
  printf '  [FALHA]    %s\n' "$*"
}
conferir() {
  local descricao=$1
  shift
  if "$@" >/dev/null 2>&1; then ok "$descricao"; else falha "$descricao"; fi
}

resolve_para_ip() { [[ "$(getent hosts "$1" | awk '{print $1}')" == "$CORREDOR_IP_SERVICO" ]]; }
zona_responde() { [[ "$(dig +short +time=2 +tries=1 @"$CORREDOR_IP_SERVICO" "$1")" == "$CORREDOR_IP_SERVICO" ]]; }
ip_de_servico() { ip -4 -o addr show dev corredor0 | grep -q " $CORREDOR_IP_SERVICO/"; }
porta_liberada() { ufw show added | grep -qx "ufw allow $1"; }
firewall_ativo() { ufw status | grep -q '^Status: active'; }
esquema_existe() {
  psql_admin -d "$BD_NOME" -tAc "SELECT 1 FROM pg_namespace WHERE nspname = '$1'" | grep -q 1
}
mqtt_entrega() {
  local saida
  saida="$(mktemp)"
  mosquitto_sub -h 127.0.0.1 -t corredor/verificacao -C 1 -W 5 >"$saida" &
  sleep 0.5
  mosquitto_pub -h 127.0.0.1 -t corredor/verificacao -m ok
  wait $!
  grep -q ok "$saida"
}
ponte_conectada() { ss -Htn state established "( dport = :$MQTT_PUBLICO_PORTA )" | grep -q .; }
# Logo depois de instalado, o monitor leva alguns segundos para gravar a primeira linha.
monitor_medindo() {
  local arquivo="$CORREDOR_LOG/monitor/monitor.log"
  for _ in $(seq 1 15); do
    if [[ -f $arquivo ]] && (($(date +%s) - $(stat -c %Y "$arquivo") < 90)); then return 0; fi
    sleep 1
  done
  return 1
}

# Depois de um reinício, a API pode levar alguns segundos para ficar pronta.
api_pronta() {
  for _ in $(seq 1 30); do
    if curl -fsS --max-time 3 "http://127.0.0.1:$API_PORTA/healthz/ready"; then return 0; fi
    sleep 1
  done
  return 1
}

echo "== Corredor Aceso em $(hostname), ambiente $CORREDOR_AMBIENTE, $(date -Is) =="

echo "Versão"
if [[ -L $CORREDOR_RAIZ/current ]]; then
  echo "  no ar:     $(cat "$CORREDOR_RAIZ/current/VERSION") (commit $(cut -c1-7 "$CORREDOR_RAIZ/current/COMMIT" 2>/dev/null || echo '?'))"
  echo "  anterior:  $(cat "$CORREDOR_RAIZ/previous/VERSION" 2>/dev/null || echo nenhuma)"
  echo "  último registro: $(grep -E 'evento=(deploy|rollback)' "$CORREDOR_LOG/deploy.log" 2>/dev/null | tail -n 1)"
else
  echo "  nenhuma versão implantada"
fi

echo "Rede"
conferir "hostname $CORREDOR_HOSTNAME" test "$(hostname)" = "$CORREDOR_HOSTNAME"
conferir "fuso $CORREDOR_FUSO" test "$(timedatectl show -p Timezone --value)" = "$CORREDOR_FUSO"
conferir "IP fixo $CORREDOR_IP_SERVICO em corredor0" ip_de_servico
for nome in api painel; do
  conferir "$nome.$CORREDOR_DOMINIO_INTERNO resolve para $CORREDOR_IP_SERVICO" resolve_para_ip "$nome.$CORREDOR_DOMINIO_INTERNO"
done
if [[ $CORREDOR_DNS_ZONA == sim ]]; then
  conferir "dnsmasq em $CORREDOR_IP_SERVICO responde api.$CORREDOR_DOMINIO_INTERNO" zona_responde "api.$CORREDOR_DOMINIO_INTERNO"
fi
conferir "firewall ativo" firewall_ativo
for porta in 22/tcp 80/tcp 443/tcp; do
  conferir "firewall libera $porta" porta_liberada "$porta"
done

echo "Serviços"
conferir "Postgres aceita conexão" pg_isready -q -h 127.0.0.1
for m in $MODULOS_BD; do
  conferir "banco $BD_NOME tem o schema $m" esquema_existe "$m"
done
conferir "Mosquitto ativo" systemctl is-active --quiet mosquitto
conferir "Mosquitto entrega mensagem local" mqtt_entrega
if ponte_conectada; then
  ok "ponte MQTT conectada a $MQTT_PUBLICO_HOST:$MQTT_PUBLICO_PORTA"
else
  pendente "ponte MQTT sem conexão com $MQTT_PUBLICO_HOST:$MQTT_PUBLICO_PORTA (o broker público pode estar fora)"
fi
conferir "Nginx ativo" systemctl is-active --quiet nginx
conferir "monitor ativo" systemctl is-active --quiet corredor-monitor
conferir "monitor mediu nos últimos 90 s" monitor_medindo

echo "Aplicação"
if [[ -L $CORREDOR_RAIZ/current ]]; then
  conferir "API pronta em /healthz/ready" api_pronta
  conferir "corredor-api ativa" systemctl is-active --quiet corredor-api
  conferir "Nginx entrega /api/versao em api.$CORREDOR_DOMINIO_INTERNO" \
    curl -fsS --max-time 3 "http://api.$CORREDOR_DOMINIO_INTERNO/api/versao"
  conferir "Nginx esconde /healthz" test "$(curl -s -o /dev/null -w '%{http_code}' "http://api.$CORREDOR_DOMINIO_INTERNO/healthz/ready")" = 404
  if [[ -f $CORREDOR_RAIZ/current/painel/index.html ]]; then
    conferir "Nginx entrega /painel/ em painel.$CORREDOR_DOMINIO_INTERNO" \
      curl -fsS --max-time 3 "http://painel.$CORREDOR_DOMINIO_INTERNO/painel/"
  fi
else
  pendente "API sem versão implantada. Rode: sudo corredor deploy vX.Y.Z"
fi

if systemctl is-active --quiet corredor-tunel; then
  echo "  túnel HTTPS: $(journalctl -u corredor-tunel -o cat --no-pager 2>/dev/null | grep -o 'https://[a-z0-9-]*\.trycloudflare\.com' | tail -n 1)"
fi

echo "Auditoria"
conferir "auditoria.log tem registros" test -s "$CORREDOR_LOG/auditoria.log"
echo "  últimos alertas:"
tail -n 3 "$CORREDOR_LOG/monitor/alertas.log" 2>/dev/null | sed 's/^/    /'

if ((FALHAS)); then
  echo "== $FALHAS item(ns) com falha =="
  exit 1
fi
echo "== tudo certo =="
