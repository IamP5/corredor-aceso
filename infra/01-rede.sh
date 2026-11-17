#!/usr/bin/env bash
# Rubrica: script de provisionamento de infra (rede, DNS, IP). 2 pontos.
# Configura hostname, fuso, IP fixo de serviço, nomes, DNS, firewall e chaves SSH.
# Pode rodar quantas vezes for preciso: cada função confere antes de mudar.
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

configurar_hostname() {
  if [[ "$(hostname)" == "$CORREDOR_HOSTNAME" ]]; then
    feito "hostname $CORREDOR_HOSTNAME"
  else
    hostnamectl set-hostname "$CORREDOR_HOSTNAME"
    mudou "hostname definido como $CORREDOR_HOSTNAME"
  fi
  # Sem isto o cloud-init reescreve o hostname e o /etc/hosts a cada boot.
  if [[ -d /etc/cloud/cloud.cfg.d ]]; then
    instalar_texto /etc/cloud/cloud.cfg.d/99-corredor.cfg 644 root:root <<'CFG'
preserve_hostname: true
manage_etc_hosts: false
CFG
  fi
}

configurar_relogio() {
  instalar_pacotes tzdata
  if [[ "$(timedatectl show -p Timezone --value)" == "$CORREDOR_FUSO" ]]; then
    feito "fuso $CORREDOR_FUSO"
  else
    timedatectl set-timezone "$CORREDOR_FUSO"
    mudou "fuso definido como $CORREDOR_FUSO"
  fi
}

# IP fixo em uma interface própria (corredor0). A placa principal continua com
# o DHCP da nuvem ou do Multipass, então este passo não derruba o SSH.
configurar_ip_de_servico() {
  local precisa=0
  systemctl is-active --quiet systemd-networkd || erro "systemd-networkd não está ativo nesta VM"
  instalar_texto /etc/systemd/network/10-corredor0.netdev 644 root:root <<'NETDEV'
[NetDev]
Name=corredor0
Kind=dummy
NETDEV
  if ((ALTEROU)); then precisa=1; fi
  instalar_texto /etc/systemd/network/10-corredor0.network 644 root:root < <(
    printf '[Match]\nName=corredor0\n\n[Network]\nAddress=%s/%s\n' "$CORREDOR_IP_SERVICO" "$CORREDOR_IP_PREFIXO"
    # Com a zona DNS ligada, o systemd-resolved pergunta ao dnsmasq só por *.corredor.internal.
    if [[ $CORREDOR_DNS_ZONA == sim ]]; then
      printf 'DNS=%s\nDomains=~%s\nDNSDefaultRoute=false\n' "$CORREDOR_IP_SERVICO" "$CORREDOR_DOMINIO_INTERNO"
    fi
  )
  if ((ALTEROU)); then precisa=1; fi
  ip -4 -o addr show dev corredor0 2>/dev/null | grep -q " $CORREDOR_IP_SERVICO/" || precisa=1
  if ((precisa)); then
    networkctl reload
    for _ in $(seq 1 20); do
      ip -4 -o addr show dev corredor0 2>/dev/null | grep -q " $CORREDOR_IP_SERVICO/" && break
      sleep 0.5
    done
    ip -4 -o addr show dev corredor0 | grep -q " $CORREDOR_IP_SERVICO/" ||
      erro "a interface corredor0 não recebeu $CORREDOR_IP_SERVICO"
    mudou "IP fixo $CORREDOR_IP_SERVICO/$CORREDOR_IP_PREFIXO em corredor0"
  else
    feito "IP fixo $CORREDOR_IP_SERVICO/$CORREDOR_IP_PREFIXO em corredor0"
  fi
}

configurar_nomes() {
  bloco_gerenciado /etc/hosts "127.0.1.1 $CORREDOR_HOSTNAME
$CORREDOR_IP_SERVICO api.$CORREDOR_DOMINIO_INTERNO painel.$CORREDOR_DOMINIO_INTERNO"
  if systemctl is-active --quiet systemd-resolved; then
    instalar_texto /etc/systemd/resolved.conf.d/corredor.conf 644 root:root <<RESOLVED
[Resolve]
FallbackDNS=$CORREDOR_DNS_RESERVA
RESOLVED
    if ((ALTEROU)); then systemctl restart systemd-resolved; fi
  else
    info "systemd-resolved inativo: DNS de reserva não aplicado"
  fi
}

# Opcional (CORREDOR_DNS_ZONA=sim): um servidor DNS de verdade para corredor.internal,
# o dnsmasq, ouvindo só no IP de serviço. O arquivo entra antes do pacote, então o
# dnsmasq já sobe sem disputar a porta 53 do systemd-resolved. O bloco do /etc/hosts
# continua valendo, com ou sem a zona.
configurar_zona_dns() {
  if [[ $CORREDOR_DNS_ZONA != sim ]]; then
    info "zona DNS do dnsmasq desligada: os nomes vêm do /etc/hosts"
    return 0
  fi
  instalar_texto /etc/dnsmasq.d/corredor.conf 644 root:root <<DNSMASQ
# Gerado por infra/01-rede.sh. Responde só no IP de serviço e só pelo domínio interno.
listen-address=$CORREDOR_IP_SERVICO
bind-dynamic
no-resolv
no-hosts
local=/$CORREDOR_DOMINIO_INTERNO/
address=/$CORREDOR_DOMINIO_INTERNO/$CORREDOR_IP_SERVICO
DNSMASQ
  local reiniciar=$ALTEROU
  instalar_pacotes dnsmasq bind9-dnsutils
  if ((reiniciar)) || ! systemctl is-active --quiet dnsmasq; then
    systemctl enable dnsmasq >/dev/null 2>&1
    systemctl restart dnsmasq
    mudou "dnsmasq responde por $CORREDOR_DOMINIO_INTERNO em $CORREDOR_IP_SERVICO"
  else
    feito "dnsmasq responde por $CORREDOR_DOMINIO_INTERNO em $CORREDOR_IP_SERVICO"
  fi
}

configurar_firewall() {
  local regra
  instalar_pacotes ufw
  if grep -q '^DEFAULT_INPUT_POLICY="DROP"' /etc/default/ufw; then
    feito "firewall: entrada negada por padrão"
  else
    ufw default deny incoming >/dev/null
    mudou "firewall: entrada negada por padrão"
  fi
  for regra in 22/tcp 80/tcp 443/tcp; do
    if ufw show added | grep -qx "ufw allow $regra"; then
      feito "firewall: porta $regra liberada"
    else
      ufw allow "$regra" >/dev/null
      mudou "firewall: porta $regra liberada"
    fi
  done
  if ufw status | grep -q '^Status: active'; then
    feito "firewall ativo"
  else
    ufw --force enable >/dev/null
    mudou "firewall ativado"
  fi
}

# Chaves públicas da equipe, versionadas em infra/chaves/equipe/*.pub.
# Apagar o arquivo de uma pessoa e rodar de novo tira o acesso dela.
garantir_chaves_ssh() {
  local admin casa chaves
  admin="$(id -un 1000 2>/dev/null)" || {
    info "sem usuário de uid 1000: chaves SSH não instaladas"
    return 0
  }
  casa="$(getent passwd "$admin" | cut -d: -f6)"
  chaves="$(cat "$INFRA_DIR"/chaves/equipe/*.pub 2>/dev/null || true)"
  garantir_dir "$casa/.ssh" 700 "$admin:$admin"
  [[ -f $casa/.ssh/authorized_keys ]] || install -m 0600 -o "$admin" -g "$admin" /dev/null "$casa/.ssh/authorized_keys"
  bloco_gerenciado "$casa/.ssh/authorized_keys" "$chaves"
}

echo "== 01-rede: ambiente $CORREDOR_AMBIENTE =="
configurar_hostname
configurar_relogio
configurar_ip_de_servico
configurar_nomes
configurar_zona_dns
configurar_firewall
garantir_chaves_ssh
echo "== 01-rede: $MUDANCAS mudança(s) =="
