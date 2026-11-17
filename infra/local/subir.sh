#!/usr/bin/env bash
# Cria a VM local no Multipass com o mesmo cloud-init da nuvem.
# Serve para desenvolver os scripts e para gravar o vídeo sem depender do Azure.
# Uso: infra/local/subir.sh [--recriar]
set -euo pipefail
AQUI="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NOME="corredor-ubs"

command -v multipass >/dev/null 2>&1 || {
  echo "Instale o Multipass: https://canonical.com/multipass/install" >&2
  exit 1
}
if [[ ${1:-} == --recriar ]]; then
  multipass delete --purge "$NOME" 2>/dev/null || true
fi
if multipass info "$NOME" >/dev/null 2>&1; then
  echo "A VM $NOME já existe. Use --recriar para começar do zero."
  multipass start "$NOME"
else
  # O prazo padrão do launch é curto para o provisionamento inteiro.
  multipass launch 24.04 --name "$NOME" --cpus 2 --memory 4G --disk 20G --timeout 1800 \
    --cloud-init "$AQUI/../cloud-init/corredor.yaml"
fi
multipass exec "$NOME" -- cloud-init status --wait --long || true
multipass exec "$NOME" -- sudo corredor status || true
echo
echo "IP da VM: $(multipass info "$NOME" | awk '/IPv4/ {print $2; exit}')"
echo "Entrar:   multipass shell $NOME"
