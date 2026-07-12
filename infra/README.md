# Pokemon Encyclopedia - Infrastructure as Code

This directory contains the infrastructure definitions for deploying the Pokemon Encyclopedia application to Azure Container Apps using either Bicep or Terraform.

## Architecture

- **Azure Container Apps**: Hosts both the API and Web services with auto-scaling
- **Redis Cache**: Provides caching for API responses and data
- **Log Analytics**: Monitors container apps and collects logs
- **Managed Identity**: System-assigned identities for secure resource communication

## Prerequisites

### Required
- Azure subscription
- Azure Container Registry (ACR) with container images pushed
- Azure CLI (`az` command)

### For Bicep Deployment
- Azure CLI with Bicep CLI (included in recent versions)

### For Terraform Deployment
- Terraform >= 1.5.0
- Azure Provider >= 3.80.0

## Container Images

Before deploying, you need to build and push container images to your Azure Container Registry:

```bash
# Build images
dotnet publish PokemonEncyclopedia.ApiService -o ./publish/api
dotnet publish PokemonEncyclopedia.Web -o ./publish/web

# Build container images
docker build -t myregistry.azurecr.io/pokemonencyclopedia-api:latest -f PokemonEncyclopedia.ApiService/Dockerfile ./publish/api
docker build -t myregistry.azurecr.io/pokemonencyclopedia-web:latest -f PokemonEncyclopedia.Web/Dockerfile ./publish/web

# Push to ACR
docker push myregistry.azurecr.io/pokemonencyclopedia-api:latest
docker push myregistry.azurecr.io/pokemonencyclopedia-web:latest
```

## Bicep Deployment

### 1. Update Parameters

Edit `bicep/parameters.dev.json`:

```json
{
  "parameters": {
    "containerRegistryUrl": {
      "value": "myregistry.azurecr.io"
    },
    "containerRegistryUsername": {
      "value": "YOUR_USERNAME"
    },
    "containerRegistryPassword": {
      "value": "YOUR_PASSWORD"
    }
  }
}
```

### 2. Deploy

```bash
cd infra/bicep

# Create resource group
az group create --name pokepedia-dev-rg --location eastus2

# Deploy Bicep template
az deployment group create \
  --name pokepedia-dev-deployment \
  --resource-group pokepedia-dev-rg \
  --template-file main.bicep \
  --parameters parameters.dev.json
```

### 3. Get Outputs

```bash
az deployment group show \
  --name pokepedia-dev-deployment \
  --resource-group pokepedia-dev-rg \
  --query properties.outputs
```

## Terraform Deployment

### 1. Initialize Terraform

```bash
cd infra/terraform

terraform init
```

### 2. Configure Variables

Edit `terraform.dev.tfvars`:

```hcl
container_registry_url      = "myregistry.azurecr.io"
container_registry_username = "YOUR_USERNAME"
container_registry_password = "YOUR_PASSWORD"
```

### 3. Set Azure Credentials

```bash
export ARM_SUBSCRIPTION_ID="your-subscription-id"
export ARM_CLIENT_ID="your-client-id"
export ARM_CLIENT_SECRET="your-client-secret"
export ARM_TENANT_ID="your-tenant-id"
```

Or use Azure CLI authentication:
```bash
az login
```

### 4. Plan Deployment

```bash
terraform plan -var-file=terraform.dev.tfvars -out=tfplan
```

### 5. Apply Deployment

```bash
terraform apply tfplan
```

### 6. Get Outputs

```bash
terraform output
```

## Configuration

### Environment Variables

Both services accept the following environment variables:

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `ASPNETCORE_URLS`: Set to `http://+:8080`
- `REDIS_ENDPOINT`: Redis cache hostname and port
- `REDIS_PASSWORD`: Redis cache access key
- `APISERVICE_ENDPOINT`: (Web service only) API service endpoint URL

### Resource Sizing

Default resource allocations:
- **API Service**: 0.5 CPU, 1GB RAM, 1-3 replicas
- **Web Service**: 0.5 CPU, 1GB RAM, 1-3 replicas
- **Redis Cache**: Standard tier, 250MB

Modify these in the template parameters or Terraform variables.

## Monitoring

### View Container Logs

```bash
# Using Azure CLI
az container app logs show \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-api

# Using Azure Portal
# Navigate to Container Apps > Logs
```

### Application Insights

Logs are automatically sent to Log Analytics. Query using KQL:

```kusto
ContainerAppConsoleLogs
| where ContainerAppName == "pokepedia-dev-api"
| order by TimeGenerated desc
```

## Scaling

### Manual Scaling

```bash
# Using Azure CLI
az container app update \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-api \
  --min-replicas 2 \
  --max-replicas 5
```

### Auto-scaling Rules

Currently uses CPU-based auto-scaling within the defined min/max replicas.

## Updating Deployments

### Update Images

```bash
# Build and push new image
docker build -t myregistry.azurecr.io/pokemonencyclopedia-api:v1.1 .
docker push myregistry.azurecr.io/pokemonencyclopedia-api:v1.1

# Update deployment (Bicep)
az deployment group create \
  --name pokepedia-dev-deployment \
  --resource-group pokepedia-dev-rg \
  --template-file main.bicep \
  --parameters parameters.dev.json apiImageTag=v1.1

# Update deployment (Terraform)
terraform apply -var-file=terraform.dev.tfvars -var="api_image_tag=v1.1"
```

## Clean Up

### Bicep

```bash
az group delete --name pokepedia-dev-rg
```

### Terraform

```bash
cd infra/terraform
terraform destroy -var-file=terraform.dev.tfvars
```

## Troubleshooting

### Container Apps Not Starting

```bash
# Check revision status
az container app revision list \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-api

# View detailed logs
az container app logs show \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-api \
  --tail 100
```

### Redis Connection Issues

```bash
# Test Redis connectivity
az redis show \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-redis

# Get connection string
az redis list-keys \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-redis
```

## Security Considerations

- Store container registry credentials in Azure Key Vault
- Use managed identities instead of connection strings when possible
- Enable Azure Policy for compliance
- Configure network policies for inter-service communication
- Regularly update base images and dependencies

## Further Reading

- [Azure Container Apps Documentation](https://docs.microsoft.com/en-us/azure/container-apps/)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest)
