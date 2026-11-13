# Deployment

## Local processes

The commands are in the [README](../README.md). Short version: start the four .NET hosts and
`npm run dev`, then open `http://localhost:5173`.

Development mode writes a shared JWT secret and internal API key to `%TEMP%\marketlane-dev\`.
Nothing secret is committed, and the same key is reused on the next run.

## Docker Compose

```powershell
Copy-Item .env.example .env
# set JWT_SECRET and INTERNAL_API_KEY to long random values
docker compose up --build
```

| URL | Component |
| --- | --- |
| `http://localhost:3000` | Storefront (nginx) |
| `http://localhost:5080` | Gateway |

nginx in the frontend container proxies `/api` and `/hubs` to the gateway, so the browser still
sees one origin. SQLite files are kept in named volumes.

Check the file without starting containers:

```powershell
$env:JWT_SECRET = "a-long-test-value-at-least-32-characters"
$env:INTERNAL_API_KEY = "another-long-test-value"
docker compose config --quiet
```

## Azure

The Bicep files are in `infra/`:

- `shared.bicep` creates the resource group resources: Container Apps environment, Azure SQL,
  container registry and Log Analytics.
- `apps.bicep` creates the Container Apps and wires the environment variables.
- `main.bicep` deploys both.

`azure-pipelines.yml` builds and tests, pushes five images to ACR and updates the Container Apps.
The pipeline expects a service connection (`azureServiceConnection`), a resource group
(`resourceGroup`), an environment name and a variable group called `marketlane-secrets` holding
the JWT secret, internal key, SQL password and Foundry settings.

Manual deployment is possible but not scripted here:

```powershell
az deployment group create -g <resource-group> -f infra/main.bicep -p environmentName=demo
```

## Configuration

| Setting | Notes |
| --- | --- |
| `Jwt__Secret` | At least 32 characters. Required outside Development. |
| `Jwt__Issuer`, `Jwt__Audience` | Must be the same in the gateway and all services. |
| `Internal__ApiKey` | Shared by the Order and Product services. |
| `Database__Provider` | `Sqlite` or `SqlServer`. |
| `ConnectionStrings__*` | One per service database. |
| `Cors__AllowedOrigins__0` | The storefront origin. |
| `Services__ProductServiceUrl` | Internal address of the Product Service. |
| `Foundry__Endpoint`, `Foundry__ApiKey`, `Foundry__ChatDeployment` | Optional AI. Leave empty to use fallbacks. |

## What was actually run

- Solution build, unit tests, integration tests and the frontend production build all pass.
- `docker compose config` passes with test secrets.
- The Bicep files compile with the Bicep CLI.
- No Azure subscription, container registry or Foundry resource was available, so the pipeline
  has not been run and nothing has been deployed. Docker image builds were not run either because
  the Docker daemon is not available on this machine.
