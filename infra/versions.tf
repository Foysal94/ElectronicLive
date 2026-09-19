terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.100"
    }
  }

  # Uncomment and configure for remote state in Azure Blob Storage
  # backend "azurerm" {
  #   resource_group_name  = "rg-electroniclive-tfstate"
  #   storage_account_name = "<unique_storage_account_name>"
  #   container_name       = "tfstate"
  #   key                  = "electroniclive.terraform.tfstate"
  # }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}
