#!/usr/bin/env bash
# Rubrica: monitoramento como serviço tolerante a falhas. 1 ponto.
# Instala monitor.sh como corredor-monitor.service, com usuário próprio,
# Restart=always e watchdog. Também instala a rotação dos logs.
# Pode rodar quantas vezes for preciso: só reinicia o monitor se algo mudou.
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

instalar_monitor() {
  local reiniciar=0
  instalar_pacotes curl logrotate
  garantir_usuario corredor-mon
  garantir_dir "$CORREDOR_LOG/monitor" 750 corredor-mon:corredor-mon
  instalar_arquivo "$INFRA_DIR/monitor.sh" /usr/local/lib/corredor/monitor.sh 755 root:root
  if ((ALTEROU)); then reiniciar=1; fi
  instalar_arquivo "$INFRA_DIR/systemd/corredor-monitor.service" /etc/systemd/system/corredor-monitor.service 644 root:root
  if ((ALTEROU)); then
    systemctl daemon-reload
    reiniciar=1
  fi
  # Alerta de falha definitiva, disparado pelo OnFailure= da API e do job de ML.
  instalar_arquivo "$INFRA_DIR/systemd/corredor-alerta@.service" /etc/systemd/system/corredor-alerta@.service 644 root:root
  if ((ALTEROU)); then systemctl daemon-reload; fi
  # Limites próprios desta VM, se existirem, ficam em /etc/corredor/monitor.env.
  if [[ -f $CORREDOR_ETC/monitor.env && $CORREDOR_ETC/monitor.env -nt /run/corredor-monitor.aplicado ]]; then
    reiniciar=1
  fi
  if ((reiniciar)) && systemctl is-active --quiet corredor-monitor; then
    systemctl restart corredor-monitor
    mudou "monitor reiniciado, porque o script, a unit ou os limites mudaram"
  fi
  ativar_servico corredor-monitor
  touch /run/corredor-monitor.aplicado
}

instalar_rotacao() {
  instalar_arquivo "$INFRA_DIR/config/logrotate-corredor" /etc/logrotate.d/corredor 644 root:root
}

echo "== 03-monitor: ambiente $CORREDOR_AMBIENTE =="
instalar_monitor
instalar_rotacao
echo "== 03-monitor: $MUDANCAS mudança(s) =="
