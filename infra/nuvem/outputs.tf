output "ip_publico" {
  value = azurerm_public_ip.pip.ip_address
}

output "fqdn" {
  description = "Copie para CORREDOR_DOMINIO_PUBLICO em infra/config/ambientes/producao.conf e para a variável VM_HOST do GitHub."
  value       = azurerm_public_ip.pip.fqdn
}

output "ssh" {
  value = "ssh ${local.usuario}@${azurerm_public_ip.pip.fqdn}"
}

output "acompanhar_primeiro_boot" {
  value = "ssh ${local.usuario}@${azurerm_public_ip.pip.fqdn} 'cloud-init status --wait --long; sudo corredor status'"
}
