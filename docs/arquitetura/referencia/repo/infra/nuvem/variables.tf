variable "regiao" {
  description = "Região do Azure. A assinatura de estudante só aceita algumas. Veja infra/nuvem/preflight.sh."
  type        = string
}

variable "tamanho" {
  description = "Tamanho da VM. B2ls_v2 tem 2 vCPU e 4 GiB e sai do crédito de estudante."
  type        = string
  default     = "Standard_B2ls_v2"
}

variable "rotulo_dns" {
  description = "Rótulo do nome público: <rotulo>.<regiao>.cloudapp.azure.com. Precisa ser único na região."
  type        = string
}

variable "origem_ssh" {
  description = "De onde o SSH é aceito. O deploy pelo GitHub Actions exige \"*\"."
  type        = string
  default     = "*"
}
