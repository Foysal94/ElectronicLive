# ElectronicLive Infrastructure as Code (Terraform)

This directory contains the production Infrastructure as Code (IaC) for provisioning all cloud resources on Microsoft Azure.

---

## Resources Provisioned

1. **Resource Group (`rg-electroniclive-prod`):** Top-level container in region `uksouth`.
2. **Log Analytics Workspace (`law-electroniclive-prod`):** Ingests and stores container logs (30-day retention, free tier up to 5 GB/month).
3. **Application Insights (`appi-electroniclive-prod`):** Application Performance Monitoring (APM), error tracing, and live metrics.
4. **Container App Environment (`cae-electroniclive-prod`):** Serverless runtime environment linked to Log Analytics.
5. **Azure Container App (`electroniclive-api`):**
   - Ingress enabled on port `8080` (public HTTPS URL).
   - `min_replicas = 0` (scale-to-zero when idle for 100% free hosting).
   - `max_replicas = 1` (limits concurrency to avoid overages).
   - Sized at `0.25 vCPU` and `0.5 GiB RAM`.
   - Image pulled from GitHub Container Registry (`ghcr.io`).
   - Neon PostgreSQL connection string and provider API keys wired as container secrets.
6. **Azure Container App Job (`electroniclive-scanner-job`):**
   - Scheduled cron runner executing twice daily (`0 8,18 * * *`).
   - Sized at `0.25 vCPU` and `0.5 GiB RAM` with 180s replica timeout.
   - Command override: `["dotnet", "ElectronicLive.Api.dll", "--job", "scan-watchlist"]`.
   - Wired with Neon PostgreSQL database, Resend email dispatching, and EDM provider API secrets.
7. **Azure Static Web App (`swa-electroniclive-prod`):**
   - Global CDN static hosting for the React/Vite frontend client.


---

## Local Execution (Optional)

```bash
# Authenticate with Azure CLI
az login

# Initialize Terraform
terraform init

# Plan changes
terraform plan -var="environment=prod"

# Apply changes
terraform apply -var="environment=prod"
```

---

## Automated CI/CD Execution

This infrastructure is automatically planned and applied in GitHub Actions via `.github/workflows/infra.yml` whenever changes inside `infra/**` are merged to `main`.
