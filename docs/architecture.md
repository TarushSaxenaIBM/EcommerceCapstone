# Architecture

Three services, one gateway, one React app. Each service has its own database and its own
`Program.cs`; there is no shared runtime between them.

| Service | Port | Owns | Database |
| --- | --- | --- | --- |
| Product Service | 5101 | Products, categories, inventory, search, suggestions | `product.db` |
| User Service | 5102 | Accounts, password hashes, JWT issuing, profiles, roles | `user.db` |
| Order Service | 5103 | Carts, checkout, orders, payments, addresses, recommendations, chat | `order.db` |
| Gateway | 5080 | Path routing, edge authentication, proxy error mapping | - |
| Storefront | 5173 | UI only | - |

## Projects

```text
src/
  ECommerce.Domain            entities and enums, no dependencies
  ECommerce.Application       DTOs, interfaces, business services, exceptions, knowledge base
  ECommerce.Infrastructure    EF Core, repositories, JWT, hashing, HTTP clients, seeders
  ECommerce.ServiceDefaults   shared HTTP pipeline (errors, auth, swagger, CORS, health)
  ECommerce.AI                Foundry options and chat client
  ECommerce.ProductService    host + controllers
  ECommerce.UserService       host + controllers
  ECommerce.OrderService      host + controllers + background worker + SignalR hub
  ECommerce.Gateway           YARP configuration and edge auth
```

Dependencies point inward: services depend on Application and Infrastructure, and Application
depends only on Domain. The AI client is hidden behind `IAiChatClient`, so Application never
references Azure.

## One request, start to finish

Take `GET /api/products?page=1`:

1. `frontend/src/api/client.js` calls the relative URL `/api/products`.
2. Vite proxies `/api` to the gateway on port 5080. nginx does the same in Docker.
3. `ECommerce.Gateway/Program.cs` matches the path and forwards it to port 5101.
4. `ProductsController` calls `ProductService.GetPagedAsync`.
5. `ProductRepository` reads SQLite through EF Core.
6. The service maps entities to `ProductSummaryResponse` and returns JSON.

Every feature follows the same path: controller, application service, repository, database.
Once this one is clear, the rest is repetition.

## Checkout

1. `CheckoutController` calls `CheckoutService.CheckoutAsync`.
2. The service loads the cart and asks the Product Service for current prices and stock.
3. It writes the address, order and pending payment inside one database transaction.
4. It asks the Product Service to reserve stock, then runs the simulated payment.
5. On success it marks the order confirmed, clears the cart and sends a SignalR message.
6. On failure it releases any stock it reserved and records the failed payment.

The cart stores a price snapshot for display, but checkout always uses the current catalog price
and re-checks stock, so a stale cart cannot oversell an item.

The reservation is a plain HTTP call inside the transaction, with a release call as compensation.
That is enough for a demo, but it is not a distributed transaction: a crash between reserving and
committing can leave stock reserved. An outbox or a reservation timeout would be the next step.

## Authentication

1. `POST /api/auth/register` or `/login` reaches the User Service through the gateway.
2. `PasswordHasher` stores a PBKDF2 hash, never the password.
3. `JwtTokenService` issues a token with the user id, email and role.
4. The storefront stores the token in `localStorage` and sends `Authorization: Bearer ...`.
5. The gateway rejects anonymous calls to protected prefixes. Each service also validates the
   token and checks the role or the owner of the record.

Passwords, card data and internal keys never leave the service that owns them. Card data is never
collected at all because payments are simulated.

## Service-to-service calls

The Order Service needs catalog data for carts, checkout, recommendations and chat. It calls the
Product Service over HTTP using `IProductCatalogClient`, with the shared `X-Internal-Api-Key`
header. Those endpoints live under `/api/internal` and are not routed by the gateway, so a browser
cannot reach them.

The Product Service is the only writer of stock. The Order Service can only ask it to reserve or
release units.

## Notifications

`OrderHub` is a SignalR hub at `/hubs/orders`. Messages are sent to `user:{userId}` groups, so a
customer only receives updates for their own orders. `OrderProcessingWorker` moves confirmed
orders to processing after a short delay to make the status change visible in the UI.

## Data choices

- SQLite keeps local setup to one file per service. SQL Server is supported by changing
  `Database__Provider`, but it uses `EnsureCreated` rather than migrations.
- Money is stored as a numeric column and handled as `decimal`. The UI formats everything as INR
  with `en-IN`.
- Roles are an enum (`Customer`, `Admin`) instead of a role table, which is enough for two roles.
- Order lines keep product name and price snapshots so an old order still reads correctly after a
  product is edited or deleted.
