#!/usr/bin/env bash
# Ensaio de Operating Systems num contêiner Ubuntu 24.04 com systemd, sem VM.
# Roda os scripts de infra/ do commit atual (HEAD) do zero, implanta o pacote do
# tools/empacotar.sh, roda o teste de falha e grava as evidências em texto.
# Não substitui a VM: ver docs/evidencias/os/README.md, que diz o que o contêiner não prova.
#
# Uso: bash tools/ensaio-os.sh [pasta de saída] [--manter]
#   pasta de saída: padrão docs/evidencias/os
#   --manter: não apaga o contêiner no fim (entre com: docker exec -it corredor-os-ensaio bash)
# Docker antigo: exporte DOCKER_API_VERSION=1.43 antes.
# shellcheck disable=SC2016  # os comandos entre aspas simples expandem dentro do contêiner
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SAIDA="$RAIZ/docs/evidencias/os"
MANTER=0
for a in "$@"; do
  case "$a" in
    --manter) MANTER=1 ;;
    *) SAIDA="$a" ;;
  esac
done
NOME="corredor-os-ensaio"
IMAGEM="corredor-ensaio:24.04"
case "$(uname -m)" in
  arm64 | aarch64) RID=linux-arm64 ;;
  *) RID=linux-x64 ;;
esac
COMMIT="$(git -C "$RAIZ" rev-parse --short HEAD)"
mkdir -p "$SAIDA"

# Tira da saída o caminho do notebook (os símbolos do .NET guardam o caminho do build) e as senhas do api.env.
limpar() { sed -e "s#$RAIZ#<repo>#g" -e 's/Password=[^;[:space:]]*/Password=***/g'; }
# Um comando que falha não para o ensaio: a falha fica escrita na evidência.
na_vm() { { docker exec "$NOME" bash -c "$*" || echo "(código de saída $?)"; } 2>&1 | limpar; }
secao() { printf '\n$ %s\n' "$*"; }
grava() { # grava <arquivo> <comando...>: roda na VM e anexa a saída, com o comando antes
  local arquivo="$SAIDA/$1"
  shift
  {
    secao "$*"
    na_vm "$*"
  } >>"$arquivo"
}
cabecalho() {
  printf '# %s\n# Ensaio em contêiner %s com systemd, commit %s, %s\n' \
    "$2" "$IMAGEM" "$COMMIT" "$(date -Iseconds)" >"$SAIDA/$1"
}

[[ -z "$(git -C "$RAIZ" status --porcelain -- infra tools api)" ]] ||
  echo "AVISO: há mudança não commitada em infra/, tools/ ou api/. O ensaio usa o HEAD." >&2

echo "== pacote $RID do HEAD (tools/empacotar.sh)"
bash "$RAIZ/tools/empacotar.sh" v0.0.1 "$RID" >/dev/null

echo "== imagem $IMAGEM"
docker build -q -t "$IMAGEM" - >/dev/null <<'DOCKERFILE'
FROM ubuntu:24.04
ENV DEBIAN_FRONTEND=noninteractive container=docker
RUN rm -f /usr/sbin/policy-rc.d \
 && apt-get update -qq \
 && apt-get install -y -qq --no-install-recommends systemd systemd-sysv dbus sudo iproute2 ca-certificates git python3 \
 && apt-get clean \
 && systemctl enable systemd-networkd \
 && systemctl mask systemd-networkd-wait-online.service
STOPSIGNAL SIGRTMIN+3
CMD ["/sbin/init"]
DOCKERFILE

echo "== contêiner $NOME"
docker rm -f "$NOME" >/dev/null 2>&1 || true
docker run -d --name "$NOME" --hostname ubuntu-ensaio --privileged --cgroupns=private \
  --tmpfs /run --tmpfs /run/lock "$IMAGEM" >/dev/null
if ((MANTER == 0)); then trap 'docker rm -f "$NOME" >/dev/null 2>&1 || true' EXIT; fi
for _ in $(seq 30); do
  [[ "$(docker exec "$NOME" systemctl is-system-running 2>/dev/null)" =~ running|degraded ]] && break
  sleep 1
done
# O Docker monta /etc/hostname por cima do arquivo, e o hostnamectl não consegue trocar o nome.
# Numa VM esse arquivo é comum. Desmontar deixa o 01-rede.sh trocar o nome como numa VM.
docker exec "$NOME" bash -c 'umount /etc/hostname; systemctl stop systemd-hostnamed'
docker exec "$NOME" mkdir -p /opt/corredor/repo
git -C "$RAIZ" archive HEAD | docker exec -i "$NOME" tar -x -C /opt/corredor/repo
docker cp "$RAIZ/dist/corredor-$RID.tar.gz" "$NOME:/root/"

echo "== 1. provisionamento do zero"
cabecalho 01-provisionamento.txt "Primeiro provisionamento: sudo bash infra/provisionar.sh numa máquina nova"
grava 01-provisionamento.txt 'hostname; systemctl status nginx --no-pager 2>&1 | head -n 2'
grava 01-provisionamento.txt 'time bash /opt/corredor/repo/infra/provisionar.sh'

echo "== 2. deploy do pacote e carga da demo"
cabecalho 02-deploy-e-semente.txt "Deploy do pacote do tools/empacotar.sh e carga da demo"
grava 02-deploy-e-semente.txt "corredor deploy --arquivo /root/corredor-$RID.tar.gz"
grava 02-deploy-e-semente.txt 'corredor semear 2>&1 | grep -B1 "semente:"'
grava 02-deploy-e-semente.txt 'curl -s http://api.corredor.internal/api/versao; echo'

echo "== 3. provisionamento de novo, com a API no ar"
cabecalho 03-reprovisionamento.txt "Segundo provisionamento: nada muda e nada reinicia"
grava 03-reprovisionamento.txt 'bash /opt/corredor/repo/infra/provisionar.sh'

echo "== 4. serviços"
cabecalho 04-servicos.txt "sudo corredor status e systemctl status dos serviços"
grava 04-servicos.txt 'corredor status'
grava 04-servicos.txt 'systemctl status corredor-api corredor-monitor mosquitto nginx postgresql@16-main corredor-ml.timer --no-pager --lines=3'
grava 04-servicos.txt 'cat /etc/corredor/api.env'

echo "== 5. rede, IP e DNS"
cabecalho 05-rede.txt "Hostname, IP fixo, nomes, firewall e zona DNS"
grava 05-rede.txt 'hostnamectl --static; timedatectl show -p Timezone --value'
grava 05-rede.txt 'ip -4 addr show dev corredor0; cat /etc/systemd/network/10-corredor0.netdev /etc/systemd/network/10-corredor0.network'
grava 05-rede.txt 'sed -n "/corredor-aceso/,/corredor-aceso/p" /etc/hosts'
grava 05-rede.txt 'ip -br -4 addr; ip -4 route'
grava 05-rede.txt 'getent hosts api.corredor.internal; getent hosts painel.corredor.internal'
grava 05-rede.txt 'getent hosts github.com; getent hosts broker.hivemq.com'
grava 05-rede.txt 'ufw status verbose'
grava 05-rede.txt 'curl -s http://api.corredor.internal/api/versao; echo; curl -s -o /dev/null -w "painel %{http_code}\n" http://painel.corredor.internal/painel/; curl -s -o /dev/null -w "healthz pelo Nginx %{http_code}\n" http://api.corredor.internal/healthz/ready'
grava 05-rede.txt 'echo CORREDOR_DNS_ZONA=sim > /etc/corredor/corredor.conf; bash /opt/corredor/repo/infra/01-rede.sh'
grava 05-rede.txt 'bash /opt/corredor/repo/infra/01-rede.sh | tail -n 1'
grava 05-rede.txt 'dig +short @10.77.0.1 api.corredor.internal; dig +short @10.77.0.1 painel.corredor.internal; dig +short @10.77.0.1 exemplo.com.br; echo "(a última consulta, fora do domínio interno, não tem resposta)"'

echo "== 6. teste de falha"
# Os limites padrão (memória 85%, disco 80%) ocupariam gigabytes da VM do Docker, que outros
# contêineres usam. O ensaio põe os limites alguns pontos acima do uso de agora. A ocupação é real.
docker exec "$NOME" bash -c '
  mem=$(awk "/^MemTotal:/ {t=\$2} /^MemAvailable:/ {d=\$2} END {printf \"%d\", (t-d)*100/t}" /proc/meminfo)
  disco=$(df -P / | awk "NR==2 {sub(\"%\",\"\",\$5); print \$5}")
  printf "MONITOR_LIMITE_MEM=%s\nMONITOR_LIMITE_DISCO=%s\n" $((mem + 4)) $((disco + 2)) > /etc/corredor/monitor.env
  echo "TESTE_AUTORIZACAO=\"Bearer dev-acs-demo-01-so-no-notebook\"" > /etc/corredor/teste-de-falha.env
  chmod 640 /etc/corredor/teste-de-falha.env'
cabecalho 06-teste-de-falha.txt "sudo corredor teste-de-falha (seis cenários) e o cenário travado (watchdog)"
grava 06-teste-de-falha.txt 'cat /etc/corredor/monitor.env; bash /opt/corredor/repo/infra/03-monitor.sh | grep -E "monitor|mudança"'
grava 06-teste-de-falha.txt 'time corredor teste-de-falha; echo "código de saída: $?"'
grava 06-teste-de-falha.txt 'corredor teste-de-falha travado; echo "código de saída: $?"'

echo "== 7. alertas do monitor"
cabecalho 07-monitor-e-alertas.txt "Métricas e alertas de CPU, memória e disco gravados pelo monitor"
grava 07-monitor-e-alertas.txt 'corredor logs alertas'
grava 07-monitor-e-alertas.txt 'tail -n 8 /var/log/corredor/monitor/monitor.log'
grava 07-monitor-e-alertas.txt 'journalctl -u corredor-monitor -p warning -o short-iso --no-pager | tail -n 15'
grava 07-monitor-e-alertas.txt 'bash /usr/local/lib/corredor/monitor.sh --uma-vez'

echo "== 8. monitor morto e religado pelo systemd"
cabecalho 08-monitor-religado.txt "corredor-monitor morto com kill -9 e congelado com kill -STOP: o systemd religa"
grava 08-monitor-religado.txt 'systemctl show corredor-monitor -p Restart -p RestartUSec -p WatchdogUSec -p NRestarts'
grava 08-monitor-religado.txt 'journalctl -u corredor-monitor -o short-iso --no-pager | grep -E "Main process exited|Watchdog timeout|Scheduled restart|Started|monitor iniciado" | tail -n 12'
grava 08-monitor-religado.txt 'antes=$(systemctl show -p MainPID --value corredor-monitor); kill -9 $antes; sleep 6; echo "PID antes $antes, depois $(systemctl show -p MainPID --value corredor-monitor)"; systemctl status corredor-monitor --no-pager --lines=0'

echo "== 9. auditoria e logs de execução"
cabecalho 09-auditoria.txt "Log de auditoria, logs de execução de cada script e registro de deploy"
grava 09-auditoria.txt 'corredor logs auditoria | tail -n 25'
grava 09-auditoria.txt 'ls -l /var/log/corredor/execucao'
grava 09-auditoria.txt 'head -n 12 "$(ls -tr /var/log/corredor/execucao/*-01-rede.log | head -n 1)"'
grava 09-auditoria.txt 'journalctl -t corredor-auditoria -o short-iso --no-pager | tail -n 5'
grava 09-auditoria.txt 'corredor logs deploy'

if grep -rIlF -e "$RAIZ" -e "$HOME" "$SAIDA" >/dev/null; then
  echo "há caminho do notebook nas evidências:" >&2
  grep -rIlF -e "$RAIZ" -e "$HOME" "$SAIDA" >&2
  exit 1
fi
echo "== evidências em ${SAIDA#"$RAIZ"/}"
grep -h -E '\[(PASSOU|FALHOU|PULADO)\]' "$SAIDA/06-teste-de-falha.txt" | sort | uniq -c
