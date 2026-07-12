terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.80.0"
    }
  }
  
  # Uncomment and configure for remote state
  # backend "azurerm" {
  #   resource_group_name  = "terraform-state"
  #   storage_account_name = "tfstate"
  #   container_name       = "tfstate"
  #   key                  = "pokepedia-dev.tfstate"
  # }
}

provider "azurerm" {
  features {}
  
  # Configure with environment variables:
  # ARM_SUBSCRIPTION_ID
  # ARM_CLIENT_ID
  # ARM_CLIENT_SECRET
  # ARM_TENANT_ID
}

variable "environment" {
  type        = string
  description = "Environment name (dev, staging, prod)"
  default     = "dev"
}

variable "location" {
  type        = string
  description = "Azure region for resources"
  default     = "eastus2"
}

variable "name_prefix" {
  type        = string
  description = "Prefix for all resource names"
  default     = "pokepedia"
}

variable "container_registry_url" {
  type        = string
  description = "Container registry URL"
}

variable "container_registry_username" {
  type        = string
  description = "Container registry username"
  sensitive   = true
}

variable "container_registry_password" {
  type        = string
  description = "Container registry password"
  sensitive   = true
}

variable "api_image_tag" {
  type        = string
  description = "API service image tag"
  default     = "latest"
}

variable "web_image_tag" {
  type        = string
  description = "Web service image tag"
  default     = "latest"
}

variable "enable_managed_identity" {
  type        = bool
  description = "Enable managed identity for container apps"
  default     = true
}

variable "redis_capacity" {
  type        = number
  description = "Redis cache capacity (0-6)"
  default     = 0
}

variable "api_cpu" {
  type        = string
  description = "API service CPU allocation"
  default     = "0.5"
}

variable "api_memory" {
  type        = string
  description = "API service memory allocation"
  default     = "1Gi"
}

variable "web_cpu" {
  type        = string
  description = "Web service CPU allocation"
  default     = "0.5"
}

variable "web_memory" {
  type        = string
  description = "Web service memory allocation"
  default     = "1Gi"
}

variable "api_min_replicas" {
  type        = number
  description = "API service minimum replicas"
  default     = 1
}

variable "api_max_replicas" {
  type        = number
  description = "API service maximum replicas"
  default     = 3
}

variable "web_min_replicas" {
  type        = number
  description = "Web service minimum replicas"
  default     = 1
}

variable "web_max_replicas" {
  type        = number
  description = "Web service maximum replicas"
  default     = 3
}

variable "tags" {
  type        = map(string)
  description = "Common tags for all resources"
  default = {
    application = "pokemonencyclopedia"
    managed_by  = "terraform"
  }
}

locals {
  resource_name_prefix = "${var.name_prefix}-${var.environment}"
  tags = merge(
    var.tags,
    {
      environment = var.environment
    }
  )
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "rg" {
  name     = "${local.resource_name_prefix}-rg"
  location = var.location
  tags     = local.tags
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "logs" {
  name                = "${local.resource_name_prefix}-logs"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}

# Redis Cache
resource "azurerm_redis_cache" "redis" {
  name                = "${local.resource_name_prefix}-redis"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  capacity            = var.redis_capacity
  family              = "C"
  sku_name            = "Standard"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  
  tags = local.tags
}

# Container App Environment
resource "azurerm_container_app_environment" "cae" {
  name                           = "${local.resource_name_prefix}-cae"
  location                       = azurerm_resource_group.rg.location
  resource_group_name            = azurerm_resource_group.rg.name
  log_analytics_workspace_id     = azurerm_log_analytics_workspace.logs.id
  
  tags = local.tags
}

# API Service Container App
resource "azurerm_container_app" "api" {
  name                         = "${local.resource_name_prefix}-api"
  container_app_environment_id = azurerm_container_app_environment.cae.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  identity {
    type = var.enable_managed_identity ? "SystemAssigned" : null
  }

  registry {
    server               = var.container_registry_url
    username             = var.container_registry_username
    password_secret_name = "container-registry-password"
  }

  secret {
    name  = "container-registry-password"
    value = var.container_registry_password
  }

  secret {
    name  = "redis-connection"
    value = "redisHost=${azurerm_redis_cache.redis.hostname},redisPort=${azurerm_redis_cache.redis.port},ssl=true,password=${azurerm_redis_cache.redis.primary_access_key}"
  }

  ingress {
    external_enabled = false
    target_port      = 8080
    transport        = "auto"
  }

  template {
    container {
      name   = "api-service"
      image  = "${var.container_registry_url}/pokemonencyclopedia-api:${var.api_image_tag}"
      cpu    = var.api_cpu
      memory = var.api_memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "REDIS_ENDPOINT"
        value = "${azurerm_redis_cache.redis.hostname}:${azurerm_redis_cache.redis.port}"
      }

      env {
        name        = "REDIS_PASSWORD"
        secret_name = "redis-connection"
      }

      liveness_probe {
        http_get {
          path = "/health"
          port = 8080
        }
        initial_delay = 10
        period_seconds = 10
      }
    }

    min_replicas = var.api_min_replicas
    max_replicas = var.api_max_replicas
  }

  tags = merge(local.tags, { service = "api" })
}

# Web Service Container App
resource "azurerm_container_app" "web" {
  name                         = "${local.resource_name_prefix}-web"
  container_app_environment_id = azurerm_container_app_environment.cae.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  identity {
    type = var.enable_managed_identity ? "SystemAssigned" : null
  }

  registry {
    server               = var.container_registry_url
    username             = var.container_registry_username
    password_secret_name = "container-registry-password"
  }

  secret {
    name  = "container-registry-password"
    value = var.container_registry_password
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"
    allow_insecure   = false
    traffic_weight {
      latest_revision = true
      percent         = 100
    }
  }

  template {
    container {
      name   = "web-service"
      image  = "${var.container_registry_url}/pokemonencyclopedia-web:${var.web_image_tag}"
      cpu    = var.web_cpu
      memory = var.web_memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name  = "APISERVICE_ENDPOINT"
        value = "http://${azurerm_container_app.api.ingress[0].fqdn}"
      }

      liveness_probe {
        http_get {
          path = "/"
          port = 8080
        }
        initial_delay = 10
        period_seconds = 10
      }
    }

    min_replicas = var.web_min_replicas
    max_replicas = var.web_max_replicas
  }

  tags = merge(local.tags, { service = "web" })

  depends_on = [azurerm_container_app.api]
}

output "web_service_url" {
  description = "Web service public URL"
  value       = "https://${azurerm_container_app.web.ingress[0].fqdn}"
}

output "api_service_fqdn" {
  description = "API service FQDN"
  value       = azurerm_container_app.api.ingress[0].fqdn
}

output "redis_hostname" {
  description = "Redis cache hostname"
  value       = azurerm_redis_cache.redis.hostname
}

output "redis_port" {
  description = "Redis cache port"
  value       = azurerm_redis_cache.redis.port
}

output "redis_access_key" {
  description = "Redis primary access key"
  value       = azurerm_redis_cache.redis.primary_access_key
  sensitive   = true
}

output "container_app_environment_id" {
  description = "Container App Environment ID"
  value       = azurerm_container_app_environment.cae.id
}

output "log_analytics_workspace_id" {
  description = "Log Analytics Workspace ID"
  value       = azurerm_log_analytics_workspace.logs.workspace_id
}
