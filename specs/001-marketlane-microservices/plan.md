# Implementation Plan: IBM ecommerce microservices

**Spec**: [spec.md](spec.md)
**Constitution**: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)

## Technical context

| Area | Choice |
| --- | --- |
| Runtime | .NET 10 / ASP.NET Core |
| Data | EF Core 10, SQLite locally and in Docker, Azure SQL in Azure |
| Gateway | YARP reverse proxy |
| Auth | JWT bearer, PBKDF2 password hashing, roles |
| Real-time | SignalR |
| AI | Microsoft Foundry / Azure OpenAI compatible chat completions behind `IAiChatClient` |
| Frontend | React 19, Vite 8, React Router 7, plain CSS |
| Containers | Multi-stage Dockerfiles, nginx for the frontend |
| Infrastructure | Bicep, Azure Container Apps, Azure SQL, ACR, Log Analytics |
| CI/CD | Azure DevOps multi-stage YAML |
| Tests | xUnit unit tests plus `WebApplicationFactory` integration tests |

## Architecture decisions

1. **Per-service DbContext and database.** Product, User and Order each own a context and a
   connection string. Repositories remain in `ECommerce.Infrastructure` so the existing business
   services are reused.
2. **HTTP catalog client.** The Order Service reads products and reserves stock through
   `IProductCatalogClient`. A failed call becomes a `503` problem response and checkout stops.
3. **Snapshots over joins.** Cart lines and order lines copy the product name, image and price.
   Orders copy the customer name and email. This avoids cross-database joins.
4. **Gateway at the edge.** YARP routes by path, validates JWTs for protected prefixes and maps
   forwarding failures to `502`/`504` problem+json.
5. **Shared service defaults.** `ECommerce.ServiceDefaults` owns controllers, validation errors,
   JWT, Swagger, CORS, health checks and the internal-key middleware.
6. **AI fallbacks.** `ECommerce.AI` wraps Foundry. Application services use deterministic
   heuristics when the client is not configured or fails, and set `UsedAi`/`AiRanked` accordingly.
7. **Configuration-driven provider.** `Database:Provider` selects SQLite or SQL Server.
   `Database:ApplyMigrations=false` uses `EnsureCreated` for tests. Azure SQL uses
   `EnsureCreated` because SQLite migrations are not T-SQL.
8. **Secrets at the edge of configuration.** Development secrets are generated in the temp
   directory. Production requires environment variables or Key Vault; no defaults are shipped.
9. **Same-origin frontend.** nginx substitutes `GATEWAY_UPSTREAM` from an environment variable so
   Docker Compose and Azure use the same image and the browser keeps relative paths.

## Project structure

```text
src/
  ECommerce.Domain/            entities and enums
  ECommerce.Application/       DTOs, interfaces, business services, AI seams
  ECommerce.Infrastructure/    DbContexts, repositories, clients, JWT, payments, seeders
  ECommerce.ServiceDefaults/   shared HTTP pipeline and secret resolution
  ECommerce.AI/                Foundry client and options
  ECommerce.ProductService/    catalog, categories, inventory, search
  ECommerce.UserService/       auth, profiles, roles
  ECommerce.OrderService/      cart, checkout, orders, payments, chat, recommendations, hub
  ECommerce.Gateway/           YARP routes, edge auth, proxy error mapping
tests/
  ECommerce.UnitTests/         service and validation tests
  ECommerce.IntegrationTests/  in-process multi-service and gateway tests
frontend/                      React storefront, nginx template
infra/                         shared.bicep, apps.bicep, main.bicep
docs/                          architecture, deployment, Foundry, security, review, verification
specs/                         Spec Kit-compatible feature artifacts
openapi/                       generated service contracts
```

## Phases

1. **Audit and design.** Record service ownership, data boundaries and compatibility constraints.
2. **Application seams.** Move ownership-specific logic into the existing application services and
   add snapshot-based cart/order models.
3. **Service split.** Create three hosts, separate persistence, seeders and migrations.
4. **Gateway.** Add YARP routes, edge authorization and failure mapping.
5. **Tests.** Rework unit tests and build an in-process multi-service integration fixture.
6. **Frontend.** Add recommendations, semantic search, suggestions and the chat widget without
   changing the visual language.
7. **Containers.** Add Dockerfiles, nginx template, Compose and environment examples.
8. **Azure.** Add Bicep modules and the Azure DevOps pipeline with secret variables.
9. **Contracts and specs.** Generate OpenAPI and add Spec Kit artifacts.
10. **Review and verification.** Fix findings, update documentation, run all available checks.

## Testing strategy

- Unit tests use temp SQLite databases and cover service rules, validation and fallbacks.
- Integration tests start four in-process hosts (Product, User, Order, Gateway). The gateway's YARP
  forwarder is replaced with a router that sends requests to the matching `TestServer`, so routing,
  JWT validation and internal HTTP calls are exercised without network sockets.
- A dedicated gateway factory points at a closed port to verify the `502` problem+json contract.
- The fixture sets `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` and `Internal__ApiKey` as process
  environment variables before hosts start, because `ServiceSecrets` resolves them early.
- Frontend is verified with a production Vite build.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Cross-service data leaks | Separate contexts, no shared tables, internal key, ownership checks |
| Stock over-selling | Reserve before order creation, release on failure, conflict on changed stock |
| AI provider outage | Deterministic fallbacks and explicit `UsedAi` flags |
| Secret leakage | Environment variables, pipeline secret variables, no deployment outputs |
| SQLite/Azure SQL drift | Documented `EnsureCreated` limitation and SQL Server migration follow-up |
| Compose image failure | `docker compose config` check plus documented Docker daemon blocker |
| Frontend regression | Preserve CSS and relative API paths; production build check |

## Complexity tracking

No additional service, database or technology is introduced beyond the requested split. The
gateway, shared defaults and AI library are small, focused projects that remove duplication rather
than add it. The legacy monolith source has been removed from the repository.
