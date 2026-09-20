<!-- AGENT DIRECTIVE: DO NOT MODIFY, AUTO-STAGE, OR COMMIT THIS INVESTIGATION DOCUMENT UNLESS EXPLICITLY INSTRUCTED BY THE USER. -->

# ElectronicLive: Production Deployment Architecture & Investigation

This document details the architectural investigation, trade-off analysis, and deployment blueprint for hosting the **ElectronicLive** monorepo (.NET 10 Minimal API backend + React/Vite SPA client).

---

## 1. Executive Summary & Goals

- **Target Audience:** Personal portfolio and recruiter showcase for a senior software developer applying to mid-tier/enterprise roles.
- **Budget Goal:** 100% free tier hosting (or minimum viable cost with a custom domain).
- **Core Architecture:** 
  - **Backend:** Decoupled .NET 10 Web API (Aggregator/BFF) containerized via Docker.
  - **Frontend:** Pure Single Page Application (SPA) built with React, Vite, and Tailwind CSS.
  - **Custom Domain:** Single domain (e.g. `electroniclive.com`) routing frontend traffic to the edge CDN and API traffic to the container app.

---

## 2. Backend Hosting: Azure Container Apps (ACA)

### Why Azure Container Apps?
1. **.NET Ecosystem Standard:** Azure is the predominant enterprise cloud for .NET engineering teams. Deploying via GitHub Actions to Azure Container Apps provides strong CV/portfolio signal.
2. **Generous Always-Free Tier:**
   - **2,000,000 HTTP requests** / month free.
   - **180,000 vCPU-seconds** / month free.
   - **360,000 GiB-seconds** / month free.
3. **True Scale-to-Zero (`minReplicas = 0`):** Fast wake-up latency (~2–4 seconds), avoiding the 30–60 second cold-start delays seen in PaaS free tiers (e.g. Render).
4. **No Registry Fees:** Pulling container images from GitHub Container Registry (`ghcr.io`) avoids Azure Container Registry (ACR) basic tier fees (~£4/month).

### Comparative Provider Matrix

| Provider / Tier | Monthly Free Allowance | Cold Start Latency | Drawbacks / Notes |
| :--- | :--- | :--- | :--- |
| **Azure Container Apps (ACA)** | **2M requests, 180k vCPU-sec** | **2–4s** | Best-in-class for .NET; requires card verification. |
| **Google Cloud Run** | 2M requests, 180k vCPU-sec | 2–4s | Great free tier, but less common for .NET roles. |
| **Render (Free Tier)** | 750 instance hours | 30–60s | Ephemeral, spins down after 15m; severe cold start delay. |
| **Azure App Service (F1)** | 60 CPU min / day | 0s (capped) | Hard daily cap shuts app down for 24h if exceeded. |
| **AWS ECS / App Runner** | 12-Month Free Trial | N/A | Expires after 12 months; no permanent free tier. |
| **Fly.io / Railway / Heroku** | No permanent free tier | N/A | Shifted to paid/credit card models. |

### Configuration Rules for Zero Spend
- **CPU / Memory:** Allocate `0.25 vCPU` and `0.5 GiB RAM` per replica.
- **Scaling Limits:** Set `minReplicas: 0` and `maxReplicas: 1`.
- **Image Host:** `ghcr.io/<owner>/electroniclive-api:latest`.

### Authentication & Secrets Management
- **GitHub Actions Authentication:** Passwordless **Azure OIDC (Workload Identity Federation)** via Microsoft Entra ID App Registration. No expiring client secrets.
- **Provider API Keys:** Injected as secure environment variables (`SKIDDLE_API_KEY`, `TICKETMASTER_API_KEY`).

### Azure Identity & Billing Note
- **Monzo / Challenger Bank Caveat:** Microsoft Azure billing routinely fails automated 3D Secure verification with UK digital challenger banks (Monzo, Revolut, Starling). Use a high-street bank card (Barclays, HSBC, Lloyds) or PayPal to complete subscription activation.

---

## 3. Observability & Logging: Azure-Native Stack

Instead of third-party SaaS tools (Splunk, Logz.io, Datadog), leverage Azure's built-in observability:

- **Log Analytics Workspace (`law-electroniclive-prod`):**
  - Ingests all container `stdout`/`stderr` logs automatically.
  - Free ingestion allowance: **5 GB / month**.
- **Application Insights (`appi-electroniclive-prod`):**
  - Integrated via `Microsoft.ApplicationInsights.AspNetCore` in the .NET API.
  - Automatically captures unhandled exceptions, full stack traces, dependency latencies (outbound calls to Skiddle, Resident Advisor, Ticketmaster), and live request metrics.
  - Queryable using KQL (Kusto Query Language) and visualizable via Azure Monitor Workbooks.

---

## 4. Frontend Hosting: Azure Static Web Apps (SWA)

### Why Azure Static Web Apps (Free Tier)?
- **Unified Azure Footprint:** Manages the entire stack (Frontend SWA, Backend ACA, Log Analytics, App Insights) under a single Azure subscription and Terraform state file.
- **Generous Free Allowance:**
  - **100 GB** bandwidth / month free.
  - **2 custom domains** with free SSL certificate auto-renewal.
  - Global edge distribution across Microsoft's global network.
- **Native SPA Fallback:** Client-side routing (`/events`, `/watchlist`) works seamlessly without routing hacks.
- **Zero Compute Waste:** Pure CDN asset delivery at sub-50ms latency without spinning up node runtimes.

---

## 5. API Connectivity & CORS Configuration

### Cross-Origin Architecture
1. **Frontend Base URL Parameter:** The client resolves API requests using `import.meta.env.VITE_ELECTRONICLIVE_API_URL`.
   - In local dev: defaults to empty string `""` and proxies through Vite to `http://localhost:5275`.
   - In production: configured with the ACA fully qualified domain name (e.g., `https://electroniclive-api.politedune-xxxx.uksouth.azurecontainerapps.io` or custom domain `https://api.electroniclive.com`).
2. **Dynamic CORS Injection via Terraform:**
   - Terraform binds the Static Web App default host name (`https://${azurerm_static_web_app.client.default_host_name}`) directly into the Container App's environment via `Cors__AllowedOrigins__0`.
   - The .NET backend automatically binds this into `Cors:AllowedOrigins` for `app.UseCors("FrontendCorsPolicy")`.

---

## 6. Target CI/CD Pipeline Architecture (Decoupled Workflows)

```mermaid
flowchart TD
    subgraph Git["GitHub Repository (Monorepo)"]
        PushBE["Git Push (backend/**)"]
        PushFE["Git Push (client/**)"]
    end

    subgraph CI_BE["Backend Pipeline (.github/workflows/backend-ci-cd.yml)"]
        PushBE --> TestBE[Job 1: .NET Build & Unit Tests]
        TestBE --> BuildDocker[Job 2: Build & Push API Image to GHCR]
        BuildDocker --> DeployACA[Job 3: Deploy to Azure Container Apps via OIDC]
    end

    subgraph CI_FE["Frontend Pipeline (.github/workflows/client-ci-cd.yml)"]
        PushFE --> TestFE[Job 1: Lint, Typecheck & Vitest]
        TestFE --> DeploySWA[Job 2: Build & Deploy dist/ to Azure Static Web Apps]
    end

    subgraph Prod["Production Infrastructure (Azure)"]
        DeployACA --> Backend["Backend: Azure Container Apps (electroniclive-api)"]
        DeploySWA --> Frontend["Frontend: Azure Static Web Apps (swa-electroniclive-prod)"]
        Frontend -->|Direct HTTPS Query (CORS)| Backend
    end
```

---

## 7. Execution Checklist

### Phase 1: Azure Infrastructure Provisioning (Terraform)
- [x] Provision Resource Group: `rg-electroniclive-prod` (`uksouth`).
- [x] Provision Log Analytics Workspace & Application Insights.
- [x] Provision Container App Environment (`cae-electroniclive-prod`).
- [x] Provision Container App (`electroniclive-api`) with scale-to-zero (`minReplicas = 0`).
- [x] Provision Static Web App (`swa-electroniclive-prod`) in `westeurope` (Free tier).
- [x] Bind `Cors__AllowedOrigins__0` dynamically to SWA default host name.

### Phase 2: Client & Pipeline Configuration
- [x] Parameterize API client with `VITE_ELECTRONICLIVE_API_URL` ([client.ts](file:///Users/foysalahmed/Code/ElectronicLive/client/src/api/client.ts)).
- [x] Create decoupled backend CI/CD pipeline ([backend-ci-cd.yml](file:///Users/foysalahmed/Code/ElectronicLive/.github/workflows/backend-ci-cd.yml)).
- [x] Create decoupled frontend CI/CD pipeline ([client-ci-cd.yml](file:///Users/foysalahmed/Code/ElectronicLive/.github/workflows/client-ci-cd.yml)).

### Phase 3: GitHub Repository Secrets & Variables
- [ ] `AZURE_CLIENT_ID` (OIDC)
- [ ] `AZURE_TENANT_ID` (OIDC)
- [ ] `AZURE_SUBSCRIPTION_ID` (OIDC)
- [ ] `AZURE_STATIC_WEB_APPS_API_TOKEN` (From Terraform output `static_web_app_api_key`)
- [ ] `VITE_ELECTRONICLIVE_API_URL` (Repo variable / secret with ACA FQDN)
- [ ] `SKIDDLE_API_KEY`
- [ ] `TICKETMASTER_API_KEY`

### Phase 4: Custom Domain (Post-MVP)
- [ ] Purchase custom domain.
- [ ] Map apex domain to Azure Static Web App.
- [ ] Map `api.` subdomain to Azure Container App.

