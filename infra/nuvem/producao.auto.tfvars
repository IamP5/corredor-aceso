# Valores versionados do ambiente de produção. Aqui não entra segredo.
# Depois de mudar regiao ou rotulo_dns, atualize CORREDOR_DOMINIO_PUBLICO em
# infra/config/ambientes/producao.conf com o valor de `terraform output fqdn`.
regiao     = "eastus"
rotulo_dns = "corredor-aceso-gs26"
