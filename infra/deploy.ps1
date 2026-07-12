param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$true)]
    [string]$AcrUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$AcrUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$AcrPassword
)

$ErrorActionPreference = "Stop"

# Configuration
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$resourceGroup = "pokepedia-${Environment}-rg"
$resourcePrefix = "pokepedia-${Environment}"
$deploymentName = "pokepedia-${Environment}-$(Get-Date -Format 'yyyyMMddHHmmss')"

Write-Host "🚀 Deploying Pokemon Encyclopedia to Azure" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Gray
Write-Host "Environment:     $Environment" -ForegroundColor Green
Write-Host "Location:        $Location" -ForegroundColor Green
Write-Host "Resource Group:  $resourceGroup" -ForegroundColor Green
Write-Host "Resource Prefix: $resourcePrefix" -ForegroundColor Green
Write-Host "Registry:        $AcrUrl" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Gray

# Check Azure CLI
try {
    $accountInfo = az account show --query "id" --output tsv
    $subscriptionId = $accountInfo
    Write-Host "📌 Using subscription: $subscriptionId" -ForegroundColor Green
}
catch {
    Write-Host "❌ Not logged in to Azure" -ForegroundColor Red
    Write-Host "Please run: az login" -ForegroundColor Yellow
    exit 1
}

# Create Resource Group
Write-Host "📦 Creating resource group..." -ForegroundColor Cyan
az group create `
    --name $resourceGroup `
    --location $Location `
    --tags environment=$Environment application=pokemonencyclopedia `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Resource group created/updated" -ForegroundColor Green
}
else {
    Write-Host "❌ Failed to create resource group" -ForegroundColor Red
    exit 1
}

# Create parameters JSON
Write-Host "⚙️  Creating parameters file..." -ForegroundColor Cyan
$parametersContent = @{
    "`$schema" = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"
    "contentVersion" = "1.0.0.0"
    "parameters" = @{
        "environment" = @{ "value" = $Environment }
        "location" = @{ "value" = $Location }
        "namePrefix" = @{ "value" = "pokepedia" }
        "containerRegistryUrl" = @{ "value" = $AcrUrl }
        "containerRegistryUsername" = @{ "value" = $AcrUsername }
        "containerRegistryPassword" = @{ "value" = $AcrPassword }
        "apiImageTag" = @{ "value" = "latest" }
        "webImageTag" = @{ "value" = "latest" }
        "enableManagedIdentity" = @{ "value" = $true }
    }
} | ConvertTo-Json -Depth 10

$parametersFile = Join-Path $scriptDir "bicep\parameters.json"
$parametersContent | Out-File -FilePath $parametersFile -Encoding UTF8
Write-Host "✅ Parameters file created" -ForegroundColor Green

# Deploy Bicep template
Write-Host "🔨 Deploying Bicep template..." -ForegroundColor Cyan
Write-Host "Deployment Name: $deploymentName" -ForegroundColor Gray

$bicepFile = Join-Path $scriptDir "bicep\main.bicep"

az deployment group create `
    --name $deploymentName `
    --resource-group $resourceGroup `
    --template-file $bicepFile `
    --parameters $parametersFile `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Deployment completed successfully" -ForegroundColor Green
}
else {
    Write-Host "❌ Deployment failed" -ForegroundColor Red
    exit 1
}

# Extract outputs
Write-Host ""
Write-Host "📊 Deployment Outputs" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Gray

$webUrl = az deployment group show `
    --name $deploymentName `
    --resource-group $resourceGroup `
    --query "properties.outputs.webServiceUrl.value" `
    --output tsv

$apiFqdn = az deployment group show `
    --name $deploymentName `
    --resource-group $resourceGroup `
    --query "properties.outputs.apiServiceFqdn.value" `
    --output tsv

$redisHost = az deployment group show `
    --name $deploymentName `
    --resource-group $resourceGroup `
    --query "properties.outputs.redisCacheHostname.value" `
    --output tsv

Write-Host "🌐 Web Service URL:  $webUrl" -ForegroundColor Green
Write-Host "🔌 API Service FQDN: $apiFqdn" -ForegroundColor Green
Write-Host "💾 Redis Host:       $redisHost" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Gray

# Save deployment info
$deploymentInfo = @"
Pokemon Encyclopedia Deployment Info
=====================================
Deployment Time: $(Get-Date)
Environment:     $Environment
Location:        $Location
Resource Group:  $resourceGroup
Subscription:    $subscriptionId

Services:
- Web Service:  $webUrl
- API Service:  $apiFqdn
- Redis Cache:  $redisHost

To view logs:
  az container app logs show --resource-group $resourceGroup --name ${resourcePrefix}-api
  az container app logs show --resource-group $resourceGroup --name ${resourcePrefix}-web

To view deployment details:
  az deployment group show --name $deploymentName --resource-group $resourceGroup

To delete resources:
  az group delete --name $resourceGroup
"@

$infoFile = Join-Path $scriptDir "deployment-info.txt"
$deploymentInfo | Out-File -FilePath $infoFile -Encoding UTF8

Write-Host ""
Write-Host "📄 Deployment info saved to: deployment-info.txt" -ForegroundColor Yellow
Write-Host ""
Write-Host "✨ Deployment successful!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Wait 2-3 minutes for services to start"
Write-Host "2. Visit: $webUrl"
Write-Host "3. Check logs: az container app logs show --resource-group $resourceGroup --name ${resourcePrefix}-web"
Write-Host ""
