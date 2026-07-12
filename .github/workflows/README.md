# GitHub Actions Deployment Workflows

This directory contains GitHub Actions workflows for deploying the Pokemon Encyclopedia application to Azure Container Apps using either Bicep (recommended) or Terraform.

## Workflows Overview

### 1. Deploy to Azure (Bicep) - DEFAULT ✅
**File:** `.github/workflows/deploy-bicep.yml`

**Trigger:** Automatic on push to `main` branch
- Changes to `PokemonEncyclopedia.ApiService/**`
- Changes to `PokemonEncyclopedia.Web/**`
- Changes to `infra/bicep/**`

**Manual Trigger:** `workflow_dispatch` with environment selection (dev/staging/prod)

**Steps:**
1. Build container images for API and Web services
2. Push to Azure Container Registry
3. Deploy infrastructure with Bicep template
4. Outputs deployment URLs

**Recommended for:** Production deployments, quick iterations, Microsoft Azure best practices

---

### 2. Deploy to Azure (Terraform)
**File:** `.github/workflows/deploy-terraform.yml`

**Trigger:** Manual only (`workflow_dispatch`)

**Options:**
- `environment`: dev, staging, or prod
- `build_images`: Whether to build images before deploying (default: true)

**Steps:**
1. (Optional) Build and push container images
2. Terraform format check
3. Terraform init/validate/plan
4. Terraform apply
5. Outputs deployment URLs

**Recommended for:** Infrastructure-as-code testing, multi-cloud strategies, advanced customization

---

## Configuration

### Required GitHub Secrets

Both workflows require these secrets in your repository settings:

```
AZURE_CONTAINER_REGISTRY_URL          # e.g., myregistry.azurecr.io
AZURE_CONTAINER_REGISTRY_USERNAME     # ACR username
AZURE_CONTAINER_REGISTRY_PASSWORD     # ACR password or access key
AZURE_SUBSCRIPTION_ID                 # Azure subscription ID
AZURE_TENANT_ID                       # Azure AD tenant ID
AZURE_CLIENT_ID                       # Service principal client ID
AZURE_CLIENT_SECRET                   # Service principal secret
```

### Setup Instructions

1. **Create Service Principal:**
   ```bash
   az ad sp create-for-rbac \
     --name "github-actions-pokepedia" \
     --role Contributor \
     --scopes /subscriptions/{subscription-id} \
     --json-auth
   ```

2. **Add GitHub Secrets:**
   - Go to Settings → Secrets and variables → Actions
   - Add each secret from the output above
   - Add ACR credentials

3. **Create ACR Access Key (if not using admin credentials):**
   ```bash
   az acr credential show --resource-group mygroup --name myregistry
   ```

---

## Usage

### Using Bicep (Default)

**Automatic deployment on push:**
- Commit to `main` with changes to code or infra/bicep/
- Workflow runs automatically
- Check Actions tab for progress

**Manual deployment:**
1. Go to Actions → Deploy to Azure (Bicep) - DEFAULT
2. Click "Run workflow"
3. Select environment (dev/staging/prod)
4. Click "Run workflow"

### Using Terraform

**Manual deployment only:**
1. Go to Actions → Deploy to Azure (Terraform)
2. Click "Run workflow"
3. Select options:
   - **Environment:** dev, staging, or prod
   - **Build Images:** true (build and push) or false (use existing)
4. Click "Run workflow"

---

## Workflow Outputs

### Bicep Deployment
- Container images pushed to ACR
- Azure resources created/updated
- Web Service URL in workflow summary

### Terraform Deployment
- Terraform plan output
- Container images pushed to ACR (if enabled)
- Terraform state tracked
- Web Service URL in workflow summary

---

## Environment Promotion

### Dev → Staging → Prod Pipeline

```bash
# 1. Push to main (triggers Bicep deployment to dev)
git push origin main

# 2. Manual Terraform deployment to staging
# Actions → Deploy to Azure (Terraform) → Run workflow → staging

# 3. Manual Terraform deployment to prod
# Actions → Deploy to Azure (Terraform) → Run workflow → prod
```

---

## Monitoring Deployments

### In GitHub
1. Go to Actions tab
2. Select the workflow run
3. View job logs in real-time
4. Check deployment summary

### On Azure
```bash
# View deployment status
az deployment group show \
  --name pokepedia-dev-deployment \
  --resource-group pokepedia-dev-rg

# View container app logs
az container app logs show \
  --resource-group pokepedia-dev-rg \
  --name pokepedia-dev-web

# View Terraform state (if using Terraform)
terraform show
```

---

## Troubleshooting

### Images not pushing to ACR
- Check ACR credentials in GitHub secrets
- Verify ACR URL format (e.g., myregistry.azurecr.io)
- Ensure service principal has AcrPush role

### Bicep deployment fails
- Check Bicep syntax: `az bicep validate infra/bicep/main.bicep`
- Verify Azure credentials
- Check resource group exists

### Terraform deployment fails
- Run locally first: `terraform plan -var-file=terraform.dev.tfvars`
- Check Terraform version matches (1.5.7)
- Verify Azure provider authentication

---

## Switching Between Workflows

### To use only Bicep (recommended)
- Bicep workflow already runs on every push to main
- Manual Terraform workflows can still be triggered
- No action needed - this is the default setup

### To use only Terraform
1. Disable Bicep workflow: Remove/rename `deploy-bicep.yml`
2. Enable Terraform auto-trigger: Uncomment `on.push` in `deploy-terraform.yml`
3. Customize workflow as needed

### To use both
- Current setup: Both available
- Bicep runs automatically on push
- Terraform available for manual deployments
- No conflicts - can use either independently

---

## Best Practices

1. **Bicep First:** Use Bicep for standard deployments (it's the default for a reason)
2. **Terraform for Testing:** Use Terraform workflow to test infrastructure changes before committing
3. **Environment Isolation:** Always specify correct environment in manual workflows
4. **Review Logs:** Check workflow logs for any warnings or errors
5. **Secrets Rotation:** Regularly rotate Azure credentials and ACR passwords
6. **Version Tags:** Use semantic versioning for image tags (e.g., v1.2.3)
7. **Documentation:** Keep deployment parameters documented in your wiki

---

## Advanced Configuration

### Custom Docker Registries
Edit the registry steps in either workflow to push to different registries

### Multi-Region Deployments
Create additional workflows or modify existing ones to deploy to multiple regions

### Approval Gates
Add manual approval step between build and deploy:
```yaml
environment:
  name: production
  deployment_branch_policy:
    protected_branches: true
```

### Notifications
Add Slack/Teams notifications in workflow summary section

---

## Maintenance

- **Monitor workflow execution times** - Adjust resource allocation if needed
- **Review error logs regularly** - Fix any intermittent failures
- **Update secrets annually** - Rotate credentials for security
- **Test disaster recovery** - Periodically test redeployment from scratch
- **Keep templates updated** - Regularly review and update Bicep/Terraform templates

---

## Support

For issues or questions:
1. Check workflow logs for detailed error messages
2. Review Azure documentation
3. Consult Bicep/Terraform documentation
4. Check repository issues for known problems
