# Deployment

## Prerequisites

| Tool | Version | Used for |
| --- | --- | --- |
| .NET SDK | 10.x | Build, tests, migrations |
| Node.js | 20+ | Frontend build |
| Docker Desktop / Docker Engine | Current | Local containers (optional) |
| Azure CLI + Bicep | Current | Azure deployment (optional) |
| Azure DevOps | - | CI/CD pipeline |

The repository does not contain secrets. Every secret is supplied with environment variables,
pipeline variables or (recommended for production) Azure Key Vault.

## Local processes

Run each service in its own terminal. Development mode generates shared JWT and internal API
keys under `%TEMP%\marketlane-dev\` so tokens minted by the User Service are accepted by the
other services.

```powershell
dotnet run --project src/ECommerce.ProductService
dotnet run --project src/ECommerce.UserService
dotnet run --project src/ECommerce.OrderService
dotnet run --project src/ECommerce.Gateway
```

Then start the storefront:

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The gateway is on `http://localhost:5080`, and Swagger is available
at `/swagger` on each service while it runs in Development.

Optional administrator seed:

```powershell
$env:Seed__AdminEmail = "admin@example.com"
$env:Seed__AdminPassword = "ChangeMe123!"
dotnet run --project src/ECommerce.UserService
```

The administrator is created only when both values are non-empty. No default account exists.

## Local Docker Compose

1. Copy `.env.example` to `.env`.
2. Replace `JWT_SECRET` and `INTERNAL_API_KEY` with long random values.
3. Optionally set `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD`.
4. Optionally set the `FOUNDRY_*` values; leave them blank to use the deterministic fallbacks.
5. Start the stack:

```powershell
docker compose up --build
```

| URL | Component |
| --- | --- |
| `http://localhost:3000` | Storefront |
| `http://localhost:5080` | Gateway |

The frontend container proxies `/api` and `/hubs` to the gateway on the Compose network. The
services use named volumes for their SQLite databases. Each service applies its own migrations on
startup.

Validate the Compose file without starting containers:

```powershell
$env:JWT_SECRET = "a-long-test-value-at-least-32-characters"
$env:INTERNAL_API_KEY = "another-long-test-value"
docker compose config --quiet
```

> The Docker daemon was not running in the environment where this repository was prepared, so the
> Compose file was validated with `docker compose config` but images were not built or started.
> See [Final verification](final-verification.md).

## Azure resources

`infra/shared.bicep` creates:

- Log Analytics workspace
- Azure Container Registry (Basic, admin account enabled for pipeline pushes)
- Azure SQL server and three databases (`productdb`, `userdb`, `orderdb`)
- SQL firewall rule allowing Azure services
- Azure Container Apps environment

`infra/apps.bicep` creates five Container Apps:

| App | Ingress | Image |
| --- | --- | --- |
| `product-<env>` | Internal | `product-service` |
| `user-<env>` | Internal | `user-service` |
| `order-<env>` | Internal | `order-service` |
| `gateway-<env>` | External | `gateway` |
| `frontend-<env>` | External | `frontend` |

`infra/main.bicep` composes both modules for a one-shot deployment after the images exist. The
pipeline uses `shared.bicep` and `apps.bicep` separately because the registry must exist before
images can be pushed.

### Pipeline setup

Create a variable group named `marketlane-secrets` and add:

| Variable | Secret | Purpose |
| --- | --- | --- |
| `sqlAdminLogin` | No | Azure SQL administrator login |
| `sqlAdminPassword` | Yes | Azure SQL password |
| `jwtSecret` | Yes | Shared JWT signing key, at least 32 characters |
| `internalApiKey` | Yes | Service-to-service key |
| `foundryEndpoint` | No | Foundry resource endpoint, or blank |
| `foundryApiKey` | Yes | Foundry key, or blank |
| `foundryChatDeployment` | No | Chat deployment name, or blank |
| `adminEmail` | No | Optional administrator email |
| `adminPassword` | Yes | Optional administrator password |

Create a pipeline variable or use the defaults for:

| Variable | Default | Purpose |
| --- | --- | --- |
| `azureServiceConnection` | - | ARM service connection |
| `resourceGroup` | - | Existing target resource group |
| `location` | - | Azure region, for example `eastus` |
| `environmentName` | - | Short name, for example `dev` |
| `imageTag` | `$(Build.BuildId)` | Image tag built and deployed by the pipeline |

The pipeline in `azure-pipelines.yml`:

1. Restores, builds and tests the .NET solution.
2. Installs Node.js, runs `npm ci` and builds the frontend.
3. Deploys `infra/shared.bicep`.
4. Builds and pushes five images to the ACR created by step 3.
5. Deploys `infra/apps.bicep` with the ACR, SQL and application secrets.

> **Secret constraints:** `sqlAdminPassword` is embedded in an ADO.NET connection string by
> `apps.bicep`, so avoid semicolons or braces in that password. A semicolon would truncate the
> connection string. Use Key Vault references if arbitrary password characters are required.
> Empty `foundryApiKey` and `adminPassword` values are omitted from the Container Apps secrets
> instead of being passed as empty secrets.

The pipeline requires an ARM service connection with permission to create resources and to list
ACR credentials. Secrets are mapped to environment variables only inside the deployment tasks and
are never written to the repository or to deployment outputs.

### Manual deployment

```powershell
az group create --name marketlane-dev --location eastus

az deployment group create `
  --resource-group marketlane-dev `
  --template-file infra/shared.bicep `
  --parameters environmentName=dev sqlAdminLogin=marketlane sqlAdminPassword="<secret>"

# Build and push images to the registry name returned by the deployment.
az acr login --name <registryName>
docker build -f src/ECommerce.ProductService/Dockerfile -t <registryLoginServer>/product-service:v1 .
# Repeat for user-service, order-service, gateway and frontend, then push.

az deployment group create `
  --resource-group marketlane-dev `
  --template-file infra/apps.bicep `
  --parameters `
    environmentName=dev `
    containerEnvironmentId="<containerEnvironmentId>" `
    registryName="<registryName>" `
    registryLoginServer="<registryLoginServer>" `
    sqlServerFqdn="<sqlServerFqdn>" `
    sqlAdminLogin=marketlane `
    sqlAdminPassword="<secret>" `
    jwtSecret="<secret>" `
    internalApiKey="<secret>" `
    frontendOrigin="https://frontend-dev.<environmentDomain>" `
    imageTag=v1
```

### Azure SQL schema

The application uses SQLite migrations for local development. SQLite migration SQL is not valid
T-SQL, so the `SqlServer` provider calls `EnsureCreatedAsync` instead of applying those migrations.
This keeps the Azure deployment runnable, but schema evolution for Azure SQL requires SQL Server
migrations or a migration bundle in a follow-up. Do not point the SQLite migrations at Azure SQL.

### Production follow-ups

- Replace the ACR admin account with managed identity and `AcrPull`.
- Store SQL, JWT, internal and Foundry secrets in Key Vault and reference them with Container Apps
  secret references or managed identity.
- Restrict the SQL firewall to the Container Apps environment instead of allowing all Azure
  services.
- Use a custom domain and managed certificates for the frontend and gateway.
- Generate SQL Server migrations before relying on schema changes in production.

## Actual deployment status

No Azure credentials or `az` CLI were available in the preparation environment, so no resource
was deployed and no live URL exists. All three Bicep templates compile cleanly with Bicep CLI
0.49.2, but no `what-if` or deployment was run. The deployment step remains **BLOCKED** until the
templates are run in a subscription with the required permissions.
