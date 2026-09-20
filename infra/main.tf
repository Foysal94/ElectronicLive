locals {
  resource_suffix = "${var.app_name}-${var.environment}"
}

# 1. Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "rg-${local.resource_suffix}"
  location = var.location

  tags = {
    Environment = var.environment
    Application = var.app_name
    ManagedBy   = "Terraform"
  }
}

# 2. Log Analytics Workspace (stdout/stderr log retention)
resource "azurerm_log_analytics_workspace" "law" {
  name                = "law-${local.resource_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = azurerm_resource_group.rg.tags
}

# 3. Application Insights (APM, distributed tracing, error tracking)
resource "azurerm_application_insights" "appi" {
  name                = "appi-${local.resource_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  workspace_id        = azurerm_log_analytics_workspace.law.id
  application_type    = "web"

  tags = azurerm_resource_group.rg.tags
}

# 4. Azure Container App Environment (Managed runtime host)
resource "azurerm_container_app_environment" "cae" {
  name                       = "cae-${local.resource_suffix}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id

  tags = azurerm_resource_group.rg.tags
}

# 5. Azure Container App (API Service)
resource "azurerm_container_app" "api" {
  name                         = "${var.app_name}-api"
  container_app_environment_id = azurerm_container_app_environment.cae.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  secret {
    name  = "appinsights-connection-string"
    value = azurerm_application_insights.appi.connection_string
  }

  secret {
    name  = "skiddle-api-key"
    value = var.skiddle_api_key != "" ? var.skiddle_api_key : "none"
  }

  secret {
    name  = "ticketmaster-api-key"
    value = var.ticketmaster_api_key != "" ? var.ticketmaster_api_key : "none"
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "${var.app_name}-api"
      image  = var.container_image
      cpu    = var.cpu
      memory = var.memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "appinsights-connection-string"
      }

      env {
        name        = "EventProviders__Skiddle__ApiKey"
        secret_name = "skiddle-api-key"
      }

      env {
        name        = "EventProviders__Ticketmaster__ApiKey"
        secret_name = "ticketmaster-api-key"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = azurerm_resource_group.rg.tags
}
