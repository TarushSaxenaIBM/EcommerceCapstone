# IBM ecommerce

A small e-commerce store used as a capstone project. Shoppers can browse the catalog, search,
keep a cart, check out, follow their orders and ask a support assistant. Admins can manage
products, categories, stock and order status.

The backend is split into three services behind a gateway, and the storefront is React. Each
service owns its own SQLite database. Nothing else in the project is hidden: if a feature is not
finished, the checklist says so.

## What is in the box

- Product Service: catalog, categories, stock, search and suggestions.
- User Service: registration, login, JWT, profiles and roles.
- Order Service: cart, checkout, orders, payments, recommendations, chat and order notifications.
- Gateway: routes `/api` and `/hubs` to the right service and checks the JWT at the edge.
- Storefront: React + Vite, talks only to the gateway.
- Tests: 31 unit tests and 17 integration tests.

## How it fits together

```text
Browser
  |
  v
React storefront (Vite on 5173, nginx in Docker)
  |  /api and /hubs
  v
YARP gateway (5080)
  |------------------|-------------------|
  v                  v                   v
Product Service      User Service        Order Service
(5101)               (5102)              (5103)
  |                  |                   |
product.db           user.db             order.db
```

Only the gateway is public. The Order Service calls the Product Service over HTTP with a shared
internal key when it needs stock or product details, so no service reads another service's tables.

## Requirements

- .NET SDK 10
- Node.js 20 or newer
- Optional: Docker Desktop, for the container setup
- Optional: Azure CLI and Bicep, for deployment

## Run it locally

Open four terminals in the repository root and start the services:

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

Open `http://localhost:5173`. Use `localhost`, not `127.0.0.1`, because the Vite dev server binds
to the IPv6 loopback on Windows.

In Development the services create a shared JWT secret and internal API key under
`%TEMP%\marketlane-dev\`, so all four hosts accept the same tokens without a committed secret.

| URL | What it is |
| --- | --- |
| `http://localhost:5173` | Storefront |
| `http://localhost:5080` | Gateway |
| `http://localhost:5101/swagger` | Product Service API |
| `http://localhost:5102/swagger` | User Service API |
| `http://localhost:5103/swagger` | Order Service API |

Register an account from the storefront. The first account is a customer; to create an
administrator, set both seed values before starting the User Service:

```powershell
$env:Seed__AdminEmail = "admin@example.com"
$env:Seed__AdminPassword = "ChangeMe123!"
dotnet run --project src/ECommerce.UserService
```

The product database is seeded with eight products and four categories on first start. To reseed
it, stop the Product Service and delete `src/ECommerce.ProductService/data/product.db`.

## Configuration

Settings are environment variables with `__` between sections. Each service reads only what it
needs.

| Setting | Default | Purpose |
| --- | --- | --- |
| `ConnectionStrings__ProductDb` | `Data Source=data/product.db` | Product database |
| `ConnectionStrings__UserDb` | `Data Source=data/user.db` | User database |
| `ConnectionStrings__OrderDb` | `Data Source=data/order.db` | Order database |
| `Database__Provider` | `Sqlite` | Also accepts `SqlServer` |
| `Jwt__Secret` | generated in Development | Signing key, at least 32 characters |
| `Internal__ApiKey` | generated in Development | Service-to-service key |
| `Services__ProductServiceUrl` | `http://localhost:5101` | Where the Order Service finds the catalog |
| `Foundry__Endpoint`, `Foundry__ApiKey`, `Foundry__ChatDeployment` | empty | Optional AI setup |

Outside Development the services refuse to start without a JWT secret and an internal API key.
No secret is committed.

## Tests

```powershell
dotnet test ECommerce.sln
```

```powershell
cd frontend
npm run build
```

The integration tests start the Product, User, Order and Gateway hosts in the same process, so
routing and internal HTTP calls are exercised without opening ports. One test points the gateway
at a closed port and checks the `502` response.

## Docker Compose

```powershell
Copy-Item .env.example .env
# replace JWT_SECRET and INTERNAL_API_KEY with long random values
docker compose up --build
```

The storefront is then on `http://localhost:3000` and the gateway on `http://localhost:5080`.
SQLite files live in named volumes and each service applies its migrations on start.

## AI features

Search, suggestions, recommendations and the support chat all work without an AI model. When
`Foundry__Endpoint` and `Foundry__ApiKey` are set, the same code calls Microsoft Foundry and the
response reports `usedAi: true` or `aiRanked: true`. If the call fails, the deterministic path
answers instead.

The support assistant also retrieves approved store facts before answering. See
[docs/ai.md](docs/ai.md) for the model settings, the prompts and the retrieval step.

## Documentation

- [Architecture](docs/architecture.md)
- [Deployment](docs/deployment.md)
- [AI and security](docs/ai.md)
- [Implementation checklist](IMPLEMENTATION-CHECKLIST.md)
- [Spec Kit spec](specs/001-marketlane-microservices/spec.md)

## Known limits

- Payments are simulated. No card data is stored and no money moves.
- The JWT is kept in `localStorage`, which is fine for a demo but not for production.
- Azure SQL uses `EnsureCreated` because the SQLite migrations are not valid T-SQL. SQL Server
  migrations are a follow-up.
- The Foundry workspace, live model calls and cloud deployment need credentials that were not
  available while this was built. The checklist keeps those items marked as unverified.
