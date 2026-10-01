output "resource_group_name" {
  description = "The name of the created resource group."
  value       = azurerm_resource_group.rg.name
}

output "container_app_name" {
  description = "The name of the deployed Azure Container App."
  value       = azurerm_container_app.api.name
}

output "container_app_job_name" {
  description = "The name of the deployed Azure Container App Job."
  value       = azurerm_container_app_job.scanner.name
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

output "static_web_app_name" {
  description = "The name of the deployed Azure Static Web App."
  value       = azurerm_static_web_app.client.name
}

output "static_web_app_default_host_name" {
  description = "The default host name (public URL) of the frontend Static Web App."
  value       = azurerm_static_web_app.client.default_host_name
}

output "static_web_app_api_key" {
  description = "The deployment token for Azure Static Web App."
  value       = azurerm_static_web_app.client.api_key
  sensitive   = true
}

