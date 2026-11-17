# Cria só o que a nuvem precisa dar: rede, firewall de borda, IP público e a VM.
# Nada aqui instala pacote ou configura serviço. A VM recebe o cloud-init, que
# clona o repositório e chama infra/provisionar.sh.

locals {
  nome         = "corredor-ubs"
  usuario      = "ubuntu"
  pasta_chaves = "${path.module}/../chaves/equipe"
  chaves       = fileset(local.pasta_chaves, "*.pub")
  tags         = { projeto = "corredor-aceso", origem = "terraform" }
  portas = {
    ssh   = { prioridade = 100, porta = "22", origem = var.origem_ssh }
    http  = { prioridade = 110, porta = "80", origem = "*" }
    https = { prioridade = 120, porta = "443", origem = "*" }
  }
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-corredor-aceso"
  location = var.regiao
  tags     = local.tags
}

resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-corredor"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  address_space       = ["10.20.0.0/16"]
  tags                = local.tags
}

resource "azurerm_subnet" "subnet" {
  name                 = "snet-corredor"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.20.1.0/24"]
}

resource "azurerm_network_security_group" "nsg" {
  name                = "nsg-corredor"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = local.tags

  dynamic "security_rule" {
    for_each = local.portas
    content {
      name                       = "permitir-${security_rule.key}"
      priority                   = security_rule.value.prioridade
      direction                  = "Inbound"
      access                     = "Allow"
      protocol                   = "Tcp"
      source_port_range          = "*"
      destination_port_range     = security_rule.value.porta
      source_address_prefix      = security_rule.value.origem
      destination_address_prefix = "*"
    }
  }
}

resource "azurerm_subnet_network_security_group_association" "nsg" {
  subnet_id                 = azurerm_subnet.subnet.id
  network_security_group_id = azurerm_network_security_group.nsg.id
}

resource "azurerm_public_ip" "pip" {
  name                = "pip-corredor"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  allocation_method   = "Static"
  sku                 = "Standard"
  domain_name_label   = var.rotulo_dns
  tags                = local.tags
}

resource "azurerm_network_interface" "nic" {
  name                = "nic-corredor"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = local.tags

  ip_configuration {
    name                          = "principal"
    subnet_id                     = azurerm_subnet.subnet.id
    private_ip_address_allocation = "Static"
    private_ip_address            = "10.20.1.10"
    public_ip_address_id          = azurerm_public_ip.pip.id
  }
}

resource "azurerm_linux_virtual_machine" "vm" {
  name                            = local.nome
  resource_group_name             = azurerm_resource_group.rg.name
  location                        = azurerm_resource_group.rg.location
  size                            = var.tamanho
  admin_username                  = local.usuario
  disable_password_authentication = true
  network_interface_ids           = [azurerm_network_interface.nic.id]
  custom_data                     = filebase64("${path.module}/../cloud-init/corredor.yaml")
  tags                            = local.tags

  dynamic "admin_ssh_key" {
    for_each = local.chaves
    content {
      username   = local.usuario
      public_key = trimspace(file("${local.pasta_chaves}/${admin_ssh_key.value}"))
    }
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "StandardSSD_LRS"
    disk_size_gb         = 30
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "ubuntu-24_04-lts"
    sku       = "server"
    version   = "latest"
  }

  lifecycle {
    # Mudar o cloud-init ou as chaves recriaria a VM e apagaria o banco.
    # Depois do primeiro boot, quem aplica mudança é infra/provisionar.sh.
    # Para recriar de propósito: terraform apply -replace=azurerm_linux_virtual_machine.vm
    ignore_changes = [custom_data, admin_ssh_key]

    precondition {
      condition     = length(local.chaves) > 0
      error_message = "Ponha ao menos uma chave pública em infra/chaves/equipe/<nome>.pub."
    }
  }
}
