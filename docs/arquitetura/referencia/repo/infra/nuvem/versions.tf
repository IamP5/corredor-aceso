terraform {
  required_version = ">= 1.6"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

# A assinatura vem da variável de ambiente ARM_SUBSCRIPTION_ID.
# O login vem do `az login`. Nenhuma credencial fica neste repositório.
provider "azurerm" {
  features {}
  resource_provider_registrations = "core"
}
