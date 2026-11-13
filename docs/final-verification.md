# Final verification

Date: 2026-11-12

Environment:

| Tool | Version / status |
| --- | --- |
| OS | Windows |
| .NET SDK | 10.0.403 |
| Node.js / npm | 26.13.0 / 11.20.0 |
| Docker client | 29.7.2 |
| Docker engine | Unavailable (`npipe:////./pipe/dockerDesktopLinuxEngine` not found) |
| Bicep CLI | 0.49.2 |
| Azure CLI (`az`) | Not installed |
| Spec Kit CLI (`specify`) | Not installed |
| Microsoft Foundry credentials | Not available |

Nothing in this report claims that a blocked step succeeded.

## Status summary

| Area | Status | Evidence |
| --- | --- | --- |
| Solution build | **COMPLETED** | 0 warnings, 0 errors |
| Unit tests | **COMPLETED** | 28 passed |
| Integration tests | **COMPLETED** | 17 passed |
| Frontend production build | **COMPLETED** | Vite build succeeded |
| Compose configuration | **COMPLETED** | `docker compose config --quiet` exit 0 |
| Docker image build / stack start | **BLOCKED** | Docker engine not running |
| Bicep compilation | **COMPLETED** | Three templates compile with Bicep CLI 0.49.2 |
| Azure deployment / `what-if` | **BLOCKED** | No Azure CLI or credentials |
| Live Foundry calls | **BLOCKED** | No endpoint, key or deployment |
| OpenAPI documents | **COMPLETED** | Four JSON documents generated |
| Spec Kit artifacts | **PARTIAL** | Compatible artifacts created; CLI unavailable |
| Secret scan | **COMPLETED** | No production secrets in source |

## Commands and results

### 1. Solution build

```powershell
dotnet build ECommerce.sln -v minimal
```

Result:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The earlier `NU1902`/`NU1903` advisories for `System.Security.Cryptography.Pkcs` 9.0.11 are resolved by
the direct 10.0.14 reference in `src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj`.

### 2. Tests

```powershell
dotnet test ECommerce.sln -v minimal --no-build
```

Result:

```text
Passed! - Failed: 0, Passed: 28, Skipped: 0, Total: 28 - ECommerce.UnitTests.dll
Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17 - ECommerce.IntegrationTests.dll
```

The integration suite starts Product, User, Order and Gateway hosts in process. It covers:

- full checkout through the gateway, including payment, cart clearing and stock reduction;
- stock conflict and reservation compensation paths;
- registration, login, profile updates and role enforcement;
- product, category, inventory and order administration;
- recommendations, semantic search and chat fallback without AI credentials;
- gateway `401` for protected anonymous requests and `502` problem+json for an unreachable upstream.

### 3. Frontend production build

```powershell
$env:Path = "C:\Program Files\nodejs;$env:Path"
npm run build
```

Run in `frontend/`. Result: Vite 8.3.5 transformed 73 modules and wrote `dist/index.html`,
`dist/assets/index-*.css` and `dist/assets/index-*.js`. Exit code 0.

### 4. Docker Compose configuration

```powershell
$env:JWT_SECRET = "a-long-test-value-at-least-32-characters"
$env:INTERNAL_API_KEY = "another-long-test-value"
docker compose config --quiet
```

Result: exit code 0. The file defines five services, named volumes for the three SQLite databases,
per-service environment variables and TCP health checks. `order-service` waits for the Product
Service, the gateway waits for all three services and the frontend waits for the gateway.

The four .NET Dockerfiles switch to the non-root `APP_UID` user and make `/app/data` writable.
This is a static review; the images were not built.

Docker image build and `docker compose up` were **not** executed:

```text
failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine
```

### 5. Bicep compilation

```powershell
& "$env:LOCALAPPDATA\Programs\Bicep CLI\bicep.exe" build infra/shared.bicep --stdout
& "$env:LOCALAPPDATA\Programs\Bicep CLI\bicep.exe" build infra/apps.bicep --stdout
& "$env:LOCALAPPDATA\Programs\Bicep CLI\bicep.exe" build infra/main.bicep --stdout
```

Result: all three exit 0 with no errors or warnings. `apps.bicep` has three documented
`#disable-next-line use-secure-value-for-secure-inputs` suppressions on SQL connection strings,
because the linter cannot infer that a `format()` result built from a `@secure()` password is
secret. The values are still passed as Container Apps secrets, never as outputs.

Compilation does not prove deployability. No `az deployment group what-if` or live deployment ran,
because `az` and Azure credentials are unavailable. Optional Foundry and administrator secrets are
omitted from the template output when their values are empty, so no empty Container Apps secret is
created. The pipeline YAML was reviewed but not queued in Azure DevOps.

### 5a. Pipeline review

`azure-pipelines.yml` was checked against the Bicep parameter and output names:

- Shared deployment outputs `registryName`, `registryLoginServer`, `sqlServerFqdn`,
  `containerEnvironmentId` and `containerEnvironmentDefaultDomain` are captured with
  `##vso[task.setvariable]` and consumed by the image and app stages.
- `apps.bicep` receives every required parameter, including `registryName`,
  `containerEnvironmentId` and `frontendOrigin`.
- Secret variables are passed through task `env:` blocks and referenced as shell variables, not
  interpolated into the script text.
- Path filters use `src/**`, `tests/**`, `frontend/**` and `infra/**`.

The YAML was not parsed by Azure DevOps and no pipeline run exists. YAML syntax risk is therefore
reduced by review, not eliminated by execution.

### 6. OpenAPI export

```powershell
powershell -ExecutionPolicy Bypass -File scripts/export-openapi.ps1
```

Result: the script started each service in Development mode and wrote:

| File | Paths |
| --- | --- |
| `openapi/product-service.json` | 14 |
| `openapi/user-service.json` | 7 |
| `openapi/order-service.json` | 13 |
| `openapi/gateway.json` | 1 (`/health`; YARP routes are code-configured) |

### 7. Secret and configuration review

- No `.env` file is present in the repository.
- `.gitignore` and `.dockerignore` exclude `.env`, `*.db` and local data.
- Application configuration contains no JWT secret, internal key, SQL password or Foundry key.
- The only password-like literals are test fixtures under `tests/`.
- Production fails fast when `Jwt:Secret` or `Internal:ApiKey` is missing.
- Bicep secrets use `@secure()` parameters or Container Apps secrets; ACR credentials are read with
  `listCredentials()` at deployment time instead of being passed as parameters or outputs.

## Requirements traceability

| Requirement | Evidence |
| --- | --- |
| Three services with separate databases | `src/ECommerce.{Product,User,Order}Service`, three DbContexts and migrations |
| Gateway keeps `/api` and `/hubs` | `src/ECommerce.Gateway/Program.cs`; integration tests through the gateway |
| Independent JWT validation and roles | `AddServiceDefaults`, service controllers; role/ownership tests |
| Internal service-to-service calls | `IProductCatalogClient` with `X-Internal-Api-Key`; no gateway `/api/internal` route |
| Stock reserve and compensation | `CheckoutService`; stock conflict and compensation tests |
| Snapshot data across boundaries | Cart/order product snapshots, order customer snapshots |
| Frontend design and relative API paths | Existing CSS retained; nginx and Vite proxy `/api` and `/hubs` |
| AI with deterministic fallbacks | `ECommerce.AI`, `IAiChatClient`; `UsedAi`/`AiRanked`; fallback tests |
| Docker packaging | Five Dockerfiles, nginx template, Compose configuration validated |
| Azure infrastructure as code | `infra/*.bicep`, all three compile |
| CI/CD without committed secrets | `azure-pipelines.yml`, variable group and secret `env:` mappings |
| OpenAPI | `openapi/*.json` generated from running services |
| Honest status reporting | This report |

## Blocked items and how to unblock

1. **Docker image build and runtime smoke test.** Start Docker Desktop (Linux engine), then run
   `docker compose up --build` after copying `.env.example` to `.env` and setting the two required
   secrets. Check `http://localhost:3000` and the `/health` endpoints.
2. **Azure deployment.** Install the Azure CLI, sign in to a subscription, create the resource
   group and run the pipeline with an ARM service connection, or run the manual commands in
   [deployment.md](deployment.md). A `what-if` is recommended before the first real deployment.
3. **Live Microsoft Foundry verification.** Set `Foundry__Endpoint`, `Foundry__ApiKey` and
   `Foundry__ChatDeployment` (or the pipeline variables) and confirm that search, recommendations and
   chat report `usedAi`/`aiRanked` as `true`.
4. **Spec Kit CLI.** Install `specify` and run it against
   `specs/001-marketlane-microservices`; the artifacts follow the expected spec/plan/tasks/contracts
   layout but were not generated by the CLI.

## Conclusion

All work that can be verified in this environment is complete: the solution builds without warnings,
all 45 tests pass, the frontend builds, Compose configuration and all Bicep templates validate, and
OpenAPI documents are generated. The remaining gaps are external dependencies that were genuinely
unavailable: Docker runtime, Azure subscription access, Foundry credentials and the Spec Kit CLI.
