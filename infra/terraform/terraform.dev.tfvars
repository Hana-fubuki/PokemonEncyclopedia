# Terraform variables for development environment

environment = "dev"
location    = "eastus2"
name_prefix = "pokepedia"

# Container Registry - Update with your registry values
container_registry_url      = "myregistry.azurecr.io"
container_registry_username = "USERNAME"
container_registry_password = "PASSWORD"

# Image tags
api_image_tag = "latest"
web_image_tag = "latest"

# Resource allocation
api_cpu    = "0.5"
api_memory = "1Gi"
web_cpu    = "0.5"
web_memory = "1Gi"

# Scaling
api_min_replicas = 1
api_max_replicas = 3
web_min_replicas = 1
web_max_replicas = 3

# Redis configuration
redis_capacity = 0 # 0 = 250MB, 1 = 1GB, etc.

# Security
enable_managed_identity = true

# Tags
tags = {
  application = "pokemonencyclopedia"
  managed_by  = "terraform"
  cost_center = "engineering"
}
