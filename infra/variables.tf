variable "location" {
  description = "The Azure region where all resources will be created."
  type        = string
  default     = "uksouth"
}

variable "environment" {
  description = "The deployment environment name (e.g., prod, staging, dev)."
  type        = string
  default     = "prod"
}

variable "app_name" {
  description = "The application name prefix used across resource naming."
  type        = string
  default     = "electroniclive"
}

variable "container_image" {
  description = "The container image to deploy from GitHub Container Registry."
  type        = string
  default     = "ghcr.io/foysal94/electroniclive/electroniclive-api:latest"
}

variable "min_replicas" {
  description = "The minimum number of replicas. Set to 0 to enable scale-to-zero (zero idle cost)."
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "The maximum number of replicas to prevent unexpected overages."
  type        = number
  default     = 1
}

variable "cpu" {
  description = "The vCPU allocation for the container instance."
  type        = number
  default     = 0.25
}

variable "memory" {
  description = "The memory allocation for the container instance (e.g., 0.5Gi)."
  type        = string
  default     = "0.5Gi"
}

variable "skiddle_api_key" {
  description = "API key for the Skiddle Events API."
  type        = string
  default     = ""
  sensitive   = true
}

variable "ticketmaster_api_key" {
  description = "API key for the Ticketmaster Discovery API."
  type        = string
  default     = ""
  sensitive   = true
}
