# Code walkthrough

This document is the short path for understanding and presenting the project. Read it top to
bottom once; it takes about five minutes.

## 1. The shape of the system

```text
Browser (React)
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

Three small services, one gateway, one React app. Each service owns its database and no service
reads another service's tables.

## 2. The six backend projects

| Project | One-sentence job |
| --- | --- |
| `ECommerce.Domain` | Plain entities and enums (`Product`, `Order`, `User`, `OrderStatus`). No dependencies. |
| `ECommerce.Application` | Business rules: services, DTOs, interfaces, exceptions. No database code. |
| `ECommerce.Infrastructure` | EF Core `DbContext`s, repositories, JWT, password hashing, HTTP client, seeders. |
| `ECommerce.ServiceDefaults` | One shared HTTP pipeline: validation errors, JWT, Swagger, CORS, health checks. |
| `ECommerce.AI` | Optional Foundry/Azure OpenAI chat client. Returns `null` instead of throwing. |
| `ECommerce.*Service` | Thin hosts: `Program.cs` + controllers. No business logic. |

Dependencies point inward: Service → Application/Infrastructure → Domain. `ECommerce.AI`
implements an interface declared in Application, so the business code never depends on Azure.

## 3. Follow one request

Example: `GET /api/products?page=1`.

1. `frontend/src/api/client.js` calls the relative URL `/api/products`.
2. Vite (development) or nginx (containers) proxies `/api` to the gateway on port 5080.
3. `ECommerce.Gateway/Program.cs` matches the path and forwards to `http://localhost:5101`.
4. `ECommerce.ProductService/Controllers/ProductsController.cs` receives the request.
5. `ECommerce.Application/Services/ProductService.cs` applies paging, filtering and caching.
6. `ECommerce.Infrastructure/Persistence/Repositories/ProductRepository.cs` reads SQLite through
   EF Core.
7. The service maps entities to `ProductSummaryResponse`/`ProductResponse` DTOs.
8. The JSON travels back through the gateway to React.

That chain — controller → application service → repository → database — repeats for every feature.
Once you can explain this one request, you can explain all of them.

## 4. Follow a checkout

1. `CheckoutController` calls `CheckoutService.CheckoutAsync`.
2. The Order Service asks the Product Service to reserve stock through
   `IProductCatalogClient` (internal HTTP call with `X-Internal-Api-Key`).
3. It creates the order and a simulated payment, clears the cart, and publishes a SignalR
   notification.
4. If a later step fails, it calls the release endpoint to put the stock back.
5. `OrderProcessingWorker` later moves `Confirmed` orders to `Processing`.

The important sentence for a review: **the cart stores price snapshots, but checkout reserves
current stock, so the system cannot silently oversell.**

## 5. Follow login

1. `AuthController` calls `AuthService.RegisterAsync` or `LoginAsync`.
2. `PasswordHasher` hashes/verifies with PBKDF2.
3. `JwtTokenService` issues a token containing the user id and role.
4. React stores the token in `localStorage` and sends `Authorization: Bearer ...`.
5. The gateway rejects anonymous calls to protected prefixes; each service also validates the JWT
   and checks roles/ownership.

## 6. Follow the chatbot

1. `frontend/src/components/ChatWidget.jsx` posts `{ message, history }` to `/api/chat`.
2. `ECommerce.OrderService/Controllers/ChatController.cs` passes the signed-in user id (or `null`).
3. `ECommerce.Application/Services/ChatbotService.cs`:
   - detects shipping/returns/order/product intents,
   - loads the customer's own orders (never another customer's),
   - matches catalog products,
   - asks `IAiChatClient` if Foundry is configured,
   - otherwise returns a deterministic fallback.
4. `ECommerce.AI/FoundryChatClient.cs` calls the Foundry chat-completions endpoint. On any failure
   it logs and returns `null`, which triggers the fallback.
5. The response carries `UsedAi`, so the UI and tests can prove which path ran.

## 7. Task-to-code map

| # | Checklist task | Where to look | What to say |
| --- | --- | --- | --- |
| 1 | Copilot-assisted development | Whole repo | Copilot helped split the monolith into services and refactor search, recommendations and validation. |
| 2 | Rest of the UI pages | `frontend/src/pages`, `components` | Home, product, cart, checkout, orders, order details, auth, 404. |
| 3 | Tests and documentation | `tests/`, `docs/` | 28 unit + 17 integration tests, architecture/deployment/security/Foundry docs. |
| 4 | Workflow optimization | `ECommerce.sln`, `docker-compose.yml`, `azure-pipelines.yml` | Shared service defaults, one solution, one pipeline, local dev secrets. |
| 5 | Chatbot build | `ChatWidget.jsx`, `ChatController.cs`, `ChatbotService.cs` | Floating widget → `/api/chat` → intent handling → optional AI. |
| 6 | Chatbot features | `ChatbotService.cs` | FAQ answers, own-order tracking, product matches. |
| 7 | Foundry workspace and model catalog | `ECommerce.AI/FoundryOptions.cs`, `FoundryChatClient.cs`, `docs/foundry.md` | Configuration-driven endpoint/model; live workspace needs your credentials. |
| 8 | AI agents | `ChatbotService.cs`, `RecommendationService.cs` | Two AI-backed flows: support agent and recommendation agent, both with fallbacks. |
| 9 | RAG with enterprise data | To be added with Foundry credentials | Retrieve approved store facts, then let the model answer only from them. |
| 10 | Governance and responsible AI | `docs/security.md`, `docs/foundry.md` | No committed secrets, own-data-only prompts, safe fallbacks, `UsedAi` transparency. |
| 11 | Spec Kit order change | `OrdersController.UpdateStatus`, `UpdateOrderStatusRequest` | Admin `PATCH /api/orders/{id}/status`; Spec Kit artifacts depend on the CLI. |

## 8. Five-minute explanation script

> The app is a React storefront in front of three .NET services. A YARP gateway routes `/api` by
> path, so the browser only knows one origin. Each service owns its own SQLite database: Product
> owns catalog and stock, User owns accounts and JWTs, Order owns carts, orders, payments, chat and
> recommendations.
>
> Most code is layered the same way: controller, application service, repository, database. Shared
> HTTP behaviour — validation errors, JWT, Swagger, CORS, health checks — lives in
> `ECommerce.ServiceDefaults` so the three services do not repeat it.
>
> Checkout is the only cross-service write: the Order Service reserves stock over an internal HTTP
> call and releases it if a later step fails. That is why the services stay independent.
>
> AI is optional. `IAiChatClient` has a Foundry implementation, but every feature has a deterministic
> fallback and reports whether AI was used. The chatbot is the clearest example: it can answer FAQs
> and look up only the signed-in customer's orders with or without a model.

## 9. Rules we follow to keep it simple

- Controllers stay thin; business logic lives in Application services.
- One DTO per request/response shape; no entities leave the service.
- New shared HTTP behaviour goes in `ServiceDefaults`, not copied into each service.
- New AI features must have a fallback and must report `UsedAi`/`AiRanked`.
- No secrets in source; development secrets are generated per machine.
- A feature is "done" only when build, tests and the running UI prove it.
