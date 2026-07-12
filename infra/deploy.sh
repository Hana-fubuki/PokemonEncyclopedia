#!/bin/bash

# Pokemon Encyclopedia - Azure Deployment Script
# This script deploys the application to Azure Container Apps using Bicep

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENVIRONMENT="${1:-dev}"
LOCATION="${2:-eastus2}"
ACR_URL="${3:-}"
ACR_USERNAME="${4:-}"
ACR_PASSWORD="${5:-}"

# Validate inputs
if [ -z "$ACR_URL" ]; then
    echo "❌ Error: Container registry URL required"
    echo "Usage: $0 <environment> <location> <acr-url> <acr-username> <acr-password>"
    echo "Example: $0 dev eastus2 myregistry.azurecr.io username password"
    exit 1
fi

# Derived values
RESOURCE_GROUP="pokepedia-${ENVIRONMENT}-rg"
RESOURCE_PREFIX="pokepedia-${ENVIRONMENT}"

echo "🚀 Deploying Pokemon Encyclopedia to Azure"
echo "=================================================="
echo "Environment:     $ENVIRONMENT"
echo "Location:        $LOCATION"
echo "Resource Group:  $RESOURCE_GROUP"
echo "Resource Prefix: $RESOURCE_PREFIX"
echo "Registry:        $ACR_URL"
echo "=================================================="

# Azure Login Check
if ! az account show >/dev/null 2>&1; then
    echo "📝 Logging in to Azure..."
    az login
fi

# Get subscription info
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "📌 Using subscription: $SUBSCRIPTION_ID"

# Create Resource Group
echo "📦 Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --tags environment="$ENVIRONMENT" application="pokemonencyclopedia" \
    --output none
echo "✅ Resource group created/updated"

# Update parameters file
echo "⚙️  Updating parameters..."
cat > "$SCRIPT_DIR/bicep/parameters.json" <<EOF
{
  "\$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment": {
      "value": "$ENVIRONMENT"
    },
    "location": {
      "value": "$LOCATION"
    },
    "namePrefix": {
      "value": "pokepedia"
    },
    "containerRegistryUrl": {
      "value": "$ACR_URL"
    },
    "containerRegistryUsername": {
      "value": "$ACR_USERNAME"
    },
    "containerRegistryPassword": {
      "value": "$ACR_PASSWORD"
    },
    "apiImageTag": {
      "value": "latest"
    },
    "webImageTag": {
      "value": "latest"
    },
    "enableManagedIdentity": {
      "value": true
    }
  }
}
EOF
echo "✅ Parameters updated"

# Deploy Bicep template
DEPLOYMENT_NAME="pokepedia-${ENVIRONMENT}-$(date +%s)"
echo "🔨 Deploying Bicep template..."
echo "Deployment Name: $DEPLOYMENT_NAME"

az deployment group create \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$SCRIPT_DIR/bicep/main.bicep" \
    --parameters "$SCRIPT_DIR/bicep/parameters.json" \
    --output json > "$SCRIPT_DIR/deployment-output.json"

echo "✅ Deployment completed successfully"

# Extract outputs
echo ""
echo "📊 Deployment Outputs"
echo "=================================================="

WEB_URL=$(az deployment group show \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query properties.outputs.webServiceUrl.value -o tsv)

API_FQDN=$(az deployment group show \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query properties.outputs.apiServiceFqdn.value -o tsv)

REDIS_HOST=$(az deployment group show \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query properties.outputs.redisCacheHostname.value -o tsv)

echo "🌐 Web Service URL:  $WEB_URL"
echo "🔌 API Service FQDN: $API_FQDN"
echo "💾 Redis Host:       $REDIS_HOST"
echo "=================================================="

# Save deployment info
cat > "$SCRIPT_DIR/deployment-info.txt" <<EOF
Pokemon Encyclopedia Deployment Info
=====================================
Deployment Time: $(date)
Environment:     $ENVIRONMENT
Location:        $LOCATION
Resource Group:  $RESOURCE_GROUP
Subscription:    $SUBSCRIPTION_ID

Services:
- Web Service:  $WEB_URL
- API Service:  $API_FQDN
- Redis Cache:  $REDIS_HOST

To view logs:
  az container app logs show --resource-group $RESOURCE_GROUP --name ${RESOURCE_PREFIX}-api
  az container app logs show --resource-group $RESOURCE_GROUP --name ${RESOURCE_PREFIX}-web

To view deployment details:
  az deployment group show --name $DEPLOYMENT_NAME --resource-group $RESOURCE_GROUP

To delete resources:
  az group delete --name $RESOURCE_GROUP
EOF

echo ""
echo "📄 Deployment info saved to: deployment-info.txt"
echo ""
echo "✨ Deployment successful!"
echo ""
echo "Next steps:"
echo "1. Wait 2-3 minutes for services to start"
echo "2. Visit: $WEB_URL"
echo "3. Check logs: az container app logs show --resource-group $RESOURCE_GROUP --name ${RESOURCE_PREFIX}-web"
echo ""
