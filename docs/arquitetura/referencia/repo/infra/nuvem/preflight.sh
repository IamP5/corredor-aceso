#!/usr/bin/env bash
# Antes do primeiro `terraform apply`: mostra o que a assinatura de estudante
# aceita, para escolher regiao e tamanho em producao.auto.tfvars.
# Uso: infra/nuvem/preflight.sh [regiao]
set -euo pipefail
REGIAO="${1:-eastus}"

az account show --query '{assinatura:name, id:id}' -o table
echo
echo "Regiões permitidas pela política da assinatura (vazio quer dizer sem restrição):"
az policy assignment list --query "[?parameters.listOfAllowedLocations].parameters.listOfAllowedLocations.value[]" -o tsv || true
echo
echo "Tamanhos da série B disponíveis sem restrição em $REGIAO:"
az vm list-skus --location "$REGIAO" --resource-type virtualMachines --size Standard_B2 \
  --query "[?length(restrictions)==\`0\`].name" -o tsv
echo
echo "Imagem Ubuntu 24.04 em $REGIAO:"
az vm image list --location "$REGIAO" --publisher Canonical --offer ubuntu-24_04-lts --sku server --all \
  --query "[-1].urn" -o tsv
echo
echo "Exporte a assinatura antes do Terraform:"
echo "  export ARM_SUBSCRIPTION_ID=\$(az account show --query id -o tsv)"
