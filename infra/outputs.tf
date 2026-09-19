output "resource_group_name" {
  description = "The name of the created resource group."
  value       = azurerm_resource_group.rg.name
}

output "container_app_name" {
  description = "The name of the deployed Azure Container App."
  value       = azurerm_container_app.api.name
}

output "container_app_fqdn" {
  description = "The fully qualified domain name (public URL) of the deployed API."
  value       = azurerm_container_app.api.latest_revision_fqdn
}

output "container_app_environment_name" {
  description = "The name of the Container App Environment."
  value       = azurerm_container_app_environment.cae.name
}

output "application_insights_connection_string" {
  description = "Connection string for Application Insights telemetry."
  value       = azurerm_application_insights.appi.connection_string
  sensitive   = true
}
