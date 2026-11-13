# IBM ecommerce

IBM ecommerce is a complete e-commerce demo built as three .NET microservices behind a YARP gateway,
with a React storefront. Shoppers can browse and search the catalog, manage a cart, check out, see
orders and notifications, and ask a support assistant. Administrators can manage products,
categories, inventory and order status.

The storefront design, public `/api` and `/hubs` paths, JWT model and checkout rules are unchanged
from the original monolith. The backend is now split by business ownership.

## Architecture

```text
Browser
  |
  v
React storefront (Vite locally, nginx in containers)
  |  same-origin /api and /hubs
  v
YARP gateway (5080)
  |-------------------|-------------------|
  v                   v                   v
Product Service       User Service        Order Service
(5101)                (5102)              (5103)
  |                   |                   |
product.db            user.db             order.db
```

| Service | Responsibilities | Database |
| --- | --- | --- |
| Product Service | Products, categories, inventory, semantic search, suggestions | `product.db` |
| User Service | Registration, login, JWT, profiles, roles | `user.db` |
| Order Service | Cart, checkout, orders, payments, addresses, recommendations, chat, SignalR | `order.db` |
| Gateway | Path routing, edge authentication, proxy error mapping | - |
| Frontend | Storefront, recommendations, search suggestions, chat widget | - |

Each service owns its data. Cross-service catalog reads and stock reservations use an internal
HTTP client with a shared `X-Internal-Api-Key`; no service reads another service's tables. Cart
and order lines store product snapshots, and orders store customer name/email snapshots.

See [Architecture](docs/architecture.md) for request flows, the internal contract and ports.

## Technology

| Layer | Stack |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, EF Core 10, SQLite (SQL Server supported by configuration) |
| Gateway | YARP reverse proxy |
| Auth | JWT bearer tokens, PBKDF2 password hashing, role-based authorization |
| Real-time | SignalR user-scoped order notifications |
| AI | Microsoft Foundry / Azure OpenAI compatible client with deterministic fallbacks |
| Frontend | React 19, Vite 8, React Router 7, plain CSS |
| Containers | Multi-stage Dockerfiles, nginx |
| Infrastructure | Bicep, Azure Container Apps, Azure SQL, ACR, Log Analytics |
| CI/CD | Azure DevOps YAML |
| Tests | xUnit, `Microsoft.AspNetCore.Mvc.Testing`, in-process multi-service hosts |

## Structure

```text
ECommerce.sln
src/
  ECommerce.Domain/            entities and enums
  ECommerce.Application/       DTOs, interfaces, business services
  ECommerce.Infrastructure/    DbContexts, repositories, HTTP clients, JWT, payments, seeders
  ECommerce.ServiceDefaults/   shared HTTP pipeline and secret resolution
  ECommerce.AI/                Foundry options and chat client
  ECommerce.ProductService/    catalog, categories, inventory, search
  ECommerce.UserService/       auth, profiles, roles
  ECommerce.OrderService/      cart, checkout, orders, payments, AI, hub
  ECommerce.Gateway/           YARP routes and edge authorization
tests/
  ECommerce.UnitTests/         service and validation tests
  ECommerce.IntegrationTests/  multi-service and gateway tests
frontend/                      React storefront, nginx template
infra/                         shared.bicep, apps.bicep, main.bicep
docs/                          architecture, deployment, Foundry, security, review, verification
specs/                         Spec Kit-compatible spec, plan and tasks
openapi/                       generated service contracts
scripts/                       OpenAPI export script
```

Dependencies point inward: services depend on Application and Infrastructure, Application depends
on Domain.

## Prerequisites

- .NET SDK 10.x
- Node.js 20+ and npm
- Optional: Docker Desktop / Docker Engine for containers
- Optional: Azure CLI with Bicep, and an Azure DevOps organization for deployment
- Optional: `dotnet-ef` for new migrations

## Run locally

Development mode generates shared JWT and internal API keys under `%TEMP%\marketlane-dev\`, so all
four hosts accept the same tokens without committing secrets.

```powershell
dotnet run --project src/ECommerce.ProductService
dotnet run --project src/ECommerce.UserService
dotnet run --project src/ECommerce.OrderService
dotnet run --project src/ECommerce.Gateway
```

In another terminal:

```powershell
cd frontend
npm install
npm run dev
```

| URL | Component |
| --- | --- |
| `http://localhost:5173` | Storefront |
| `http://localhost:5080` | Gateway |
| `http://localhost:5101/swagger` | Product Service Swagger (Development) |
| `http://localhost:5102/swagger` | User Service Swagger (Development) |
| `http://localhost:5103/swagger` | Order Service Swagger (Development) |

The Vite dev server proxies `/api` and `/hubs` to `http://localhost:5080`, so no CORS setup is
needed for normal readiness.

### Seed an administrator

```powershell
$env:Seed__AdminEmail = "admin@example.com"
$env:Seed__AdminPassword = "ChangeMe123!"
dotnet run --project src/ECommerce.UserService
```

The account is created only when both values are non-empty. There is no default administrator.

## Configuration

Configuration uses environment variables with `__` as the separator. Each service reads only the
settings it owns.

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__ProductDb` | Product database |
| `ConnectionStrings__UserDb` | User database |
| `ConnectionStrings__OrderDb` | Order database |
| `Database__Provider` | `Sqlite` (default) or `SqlServer` |
| `Database__ApplyMigrations` | `true` applies SQLite migrations; `false` uses `EnsureCreated` |
| `Jwt__Secret` | JWT signing key, at least 32 characters. Required outside Development |
| `Jwt__Issuer`, `Jwt__Audience` | Must match across the gateway and services |
| `Internal__ApiKey` | Service-to-service key. Required outside Development |
| `Cors__AllowedOrigins__0` | Allowed browser origin |
| `Services__ProductServiceUrl` | Product Service address used by the Order Service and gateway |
| `Seed__AdminEmail`, `Seed__AdminPassword` | Optional administrator seed |
| `Foundry__Endpoint`, `Foundry__ApiKey`, `Foundry__ChatDeployment` | Optional AI configuration |
| `Foundry__Mode`, `Foundry__ApiVersion`, `Foundry__ChatPath` | Optional AI request routing |

No secrets are committed. Production refuses to start if the JWT secret or internal key is
missing. See [Security](docs/security.md) and [Microsoft Foundry](docs/foundry.md).

## Docker Compose

```powershell
Copy-Item .env.example .env
# Edit .env and replace JWT_SECRET and INTERNAL_API_KEY with long random values.
docker compose up --build
```

| URL | Component |
| --- | --- |
| `http://localhost:3000` | Storefront |
| `http://localhost:5080` | Gateway |

The frontend nginx container proxies `/api` and `/hubs` to the gateway, keeping the browser
same-origin. SQLite data lives in named volumes. Each service applies its own migrations on
startup.

Validate the Compose file without starting containers:

```powershell
$env:JWT_SECRET = "a-long-test-value-at-least-32-characters"
$env:INTERNAL_API_KEY = "another-long-test-value"
docker compose config --quiet
```

## API overview

| Area | Endpoints |
| --- | --- |
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` |
| Users | `PUT /api/users/me`, `GET /api/users` (admin) |
| Products | `GET /api/products`, `GET /api/products/{id}`, `POST /api/products/search`, `GET /api/products/suggestions`, admin `POST/PUT/DELETE` |
| Categories | `GET /api/categories`, `GET /api/categories/{id}`, admin `POST/PUT/DELETE` |
| Inventory | `GET/PUT /api/inventory/{productId}`, `POST /api/inventory/{productId}/adjust` (admin) |
| Cart | `GET /api/cart`, `POST /api/cart/items`, `PUT/DELETE /api/cart/items/{itemId}`, `DELETE /api/cart` |
| Checkout | `POST /api/checkout` |
| Orders | `GET /api/orders`, `GET /api/orders/{id}`, `GET /api/orders/all` (admin), `PATCH /api/orders/{id}/status` (admin) |
| Payments | `GET /api/payments/{orderId}` |
| Recommendations | `GET /api/recommendations/home`, `GET /api/recommendations/related/{productId}` |
| Chat | `POST /api/chat` |
| Notifications | SignalR hub at `/hubs/orders` |

Generated OpenAPI documents are in [openapi/](openapi). Regenerate them with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/export-openapi.ps1
```

The internal endpoints under `/api/internal` are not routed by the gateway. See the
[contract summary](specs/001-marketlane-microservices/contracts/README.md).

## AI features

- **Semantic search** interprets natural language into keywords, category and price range.
- **Autocomplete** returns product suggestions as the shopper types.
- **Recommendations** use purchases, preferred categories, popularity and stock, optionally
  re-ranked by Foundry.
- **Support chat** answers shipping, returns, order-status and product questions, using permitted
  order facts for the signed-in customer.

All four work without credentials. Responses report `usedAi` or `aiRanked` so fallbacks are
visible. Never assume a live AI call succeeded unless those flags and the Foundry configuration
say so.

## Tests

```powershell
dotnet test ECommerce.sln
```

The suite contains 28 unit tests and 17 integration tests. Integration tests start Product, User,
Order and Gateway hosts in process. The gateway's YARP forwarder routes to the matching `TestServer`,
so path routing, JWT validation and internal HTTP calls are exercised without network sockets. A
separate test points the gateway at a closed port and verifies the `502` problem+json contract.

Frontend production build:

```powershell
cd frontend
npm run build
```

## Azure deployment

Bicep modules and the Azure DevOps pipeline are in [infra/](infra) and
[azure-pipelines.yml](azure-pipelines.yml). The pipeline builds and tests, deploys shared
resources, pushes five images to ACR and deploys the Container Apps. See
[Deployment](docs/deployment.md) for the variable group, required secrets and manual commands.

## Documentation

- [Code walkthrough](docs/code-walkthrough.md)
- [Architecture](docs/architecture.md)
- [Deployment](docs/deployment.md)
- [Microsoft Foundry](docs/foundry.md)
- [Security](docs/security.md)
- [Code review](docs/code-review.md)
- [Final verification](docs/final-verification.md)
- [Spec Kit spec](specs/001-marketlane-microservices/spec.md), [plan](specs/001-marketlane-microservices/plan.md), [tasks](specs/001-marketlane-microservices/tasks.md)

## Verification status

| Check | Status |
| --- | --- |
| .NET solution build | Passed |
| Unit tests (28) | Passed |
| Integration tests (17) | Passed |
| Frontend production build | Passed |
| `docker compose config` | Passed with test secrets |
| Bicep compilation | Passed for `shared.bicep`, `apps.bicep` and `main.bicep` (Bicep CLI 0.49.2) |
| Docker image build and stack start | Blocked: Docker daemon not available |
| Bicep deployment | Blocked: no Azure credentials or `az` CLI |
| Live Foundry calls | Blocked: no endpoint, key or deployment |
| Spec Kit CLI | Blocked: `specify` not installed; compatible artifacts created manually |

Nothing in this repository claims that a blocked external step succeeded.

## Notes and assumptions

- SQLite keeps local setup simple. Azure SQL uses `EnsureCreated` because the SQLite migrations
  are not valid T-SQL; SQL Server migrations are a documented follow-up.
- Money is stored as a SQLite-compatible numeric column but handled as `decimal` in C#. The
  storefront displays all amounts in Indian rupees (`en-IN`, `INR`).
- Roles are a `UserRole` enum (Customer, Admin) rather than a separate role table.
- Payments are simulated by `MockPaymentProvider`. No card data or real payment credentials exist.
- Access tokens are stored in `localStorage` for the demo. A hardened storefront would use
  http-only cookies or refresh-token rotation.
