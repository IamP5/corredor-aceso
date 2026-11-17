#!/usr/bin/env bash
# Dá à VM uma URL HTTPS temporária, para o celular e os vídeos usarem HTTPS sem
# certificado próprio. Instala o cloudflared, liga corredor-tunel.service e mostra a URL.
# A URL muda cada vez que o serviço reinicia. Não faz parte do provisionamento.
# Uso, dentro da VM: sudo corredor tunel        (liga e mostra a URL)
#                    sudo corredor tunel parar  (desliga)
set -Eeuo pipefail
INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/comum.sh
source "$INFRA_DIR/lib/comum.sh"
# shellcheck source=lib/auditoria.sh
source "$INFRA_DIR/lib/auditoria.sh"
exigir_root
carregar_config
auditoria_iniciar "$@"

url_do_tunel() {
  journalctl -u corredor-tunel --since "$1" -o cat --no-pager 2>/dev/null |
    grep -o 'https://[a-z0-9-]*\.trycloudflare\.com' | tail -n 1
}

if [[ ${1:-} == parar ]]; then
  systemctl disable --now corredor-tunel >/dev/null 2>&1 || true
  mudou "túnel desligado"
  exit 0
fi

if ! command -v cloudflared >/dev/null 2>&1; then
  pacote="cloudflared-linux-$(dpkg --print-architecture).deb"
  curl -fsSL --retry 3 -o "/tmp/$pacote" "https://github.com/cloudflare/cloudflared/releases/latest/download/$pacote"
  dpkg -i "/tmp/$pacote" >/dev/null
  rm -f "/tmp/$pacote"
  mudou "cloudflared instalado"
fi
instalar_arquivo "$INFRA_DIR/systemd/corredor-tunel.service" /etc/systemd/system/corredor-tunel.service 644 root:root
if ((ALTEROU)); then systemctl daemon-reload; fi

inicio="$(date '+%Y-%m-%d %H:%M:%S')"
systemctl enable corredor-tunel >/dev/null 2>&1
systemctl restart corredor-tunel
for _ in $(seq 1 30); do
  url="$(url_do_tunel "$inicio")"
  [[ -n $url ]] && break
  sleep 1
done
[[ -n ${url:-} ]] || erro "o túnel não mostrou URL em 30 s. Veja: journalctl -u corredor-tunel"
echo
echo "URL HTTPS da API: $url"
echo "No app, ponha EXPO_PUBLIC_API_URL=$url em app/.env.local e reinicie o Expo."
echo "Painel: $url/painel/"
