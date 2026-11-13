# Architecture

IBM ecommerce is split into three independently deployable business services behind a YARP
gateway. The React storefront keeps the same public `/api` and `/hubs` paths it used when the
backend was a monolith.

```text
Browser
  |
  v
React storefront (nginx, port 3000 in Docker)
  |  same-origin /api and /hubs
  v
YARP gateway (port 5080)
  |------------------|-------------------|
  v                  v                   v
Product Service      User Service        Order Service
(5101)               (5102)              (5103)
  |                  |                   |
product.db           user.db             order.db
```

## Service ownership

| Service | Owns | Does not own |
| --- | --- | --- |
| Product Service | Products, categories, inventory, semantic search and product suggestions | Users, carts, orders, payments |
| User Service | Accounts, password hashes, JWT issuing, profiles, roles | Product, cart or order data |
| Order Service | Carts, checkout, orders, payments, addresses, recommendations and support chat | Product or user tables |

Each service has its own EF Core `DbContext` and its own database. No service reads another
service's tables. Data that crosses a boundary is copied as a snapshot:

- A cart item keeps the product name, image and unit price at the time it was added.
- An order keeps the customer name and email, because the User Service owns the live account.
- Product and inventory reads go through `IProductCatalogClient`, a typed HTTP client that sends
  `X-Internal-Api-Key` to the Product Service.

## Internal contract

These endpoints are available only inside the service network. The gateway does not route
`/api/internal`.

| Endpoint | Purpose |
| --- | --- |
| `GET /api/internal/products/{id}` | Read one product snapshot |
| `GET /api/internal/products?ids=1,2` | Read several product snapshots |
| `GET /api/internal/products/featured?take=8` | Read featured products for recommendations |
| `POST /api/internal/inventory/{id}/reserve` | Reserve stock for checkout |
| `POST /api/internal/inventory/{id}/release` | Compensate a failed checkout |

`InternalApiKeyMiddleware` rejects requests without a matching key with a `401 problem+json`
response. The key is a separate secret from the JWT signing key.

## Checkout flow

1. The Order Service validates the cart and requests a stock reservation for each item from the
   Product Service.
2. If every reservation succeeds, the Order Service creates the order and payment, clears the
   cart and publishes a SignalR notification.
3. If any later step fails, reserved stock is released with the compensating endpoint.
4. A background worker moves `Confirmed` orders to `Processing` after the configured delay.

The cart stores price snapshots, but checkout reserves current stock. If stock changed between
adding to the cart and checkout, the request fails with `409 Conflict` rather than silently
over-selling.

## Gateway

YARP routes requests by path:

| Path | Destination |
| --- | --- |
| `/api/products`, `/api/categories`, `/api/inventory` | Product Service |
| `/api/auth`, `/api/users` | User Service |
| `/api/cart`, `/api/orders`, `/api/checkout`, `/api/payments`, `/api/recommendations`, `/api/chat` | Order Service |
| `/hubs/{**catch-all}` | Order Service (WebSocket passthrough) |

`GatewayAuthorizationMiddleware` rejects anonymous requests to protected prefixes before they
reach a downstream service. Every service still validates the JWT and enforces its own roles and
ownership rules, so the gateway is an optimization rather than the only line of defence.

Unhandled proxy failures become `502` or `504` problem+json responses. The integration suite
verifies the gateway contract with a deliberately unreachable upstream.

## Frontend

The React app keeps a single API client and talks to relative URLs. In development Vite proxies
`/api` and `/hubs` to the gateway. In Docker and Azure, nginx serves the built assets and proxies
the same paths to the gateway, so the browser never needs CORS or a cross-origin API URL.

## AI boundaries

AI is optional and isolated behind `IAiChatClient`:

- Product Service uses Foundry for semantic query interpretation, then falls back to keyword
  ranking when the provider is not configured or fails.
- Order Service uses Foundry to rank recommendations and to answer support questions, with
  deterministic heuristics and FAQ answers as fallbacks.

The response models expose `UsedAi` or `AiRanked`, so the UI can show when a fallback was used.
The application never reports a successful AI call when the provider was unavailable.

## Ports

| Component | Local | Container | Azure |
| --- | --- | --- | --- |
| Product Service | 5101 | 8080 | Internal ingress |
| User Service | 5102 | 8080 | Internal ingress |
| Order Service | 5103 | 8080 | Internal ingress |
| Gateway | 5080 | 8080 | External ingress |
| Frontend | 5173 (Vite) | 80 | External ingress |

## Related documents

- [Code walkthrough](code-walkthrough.md)
- [Deployment](deployment.md)
- [Microsoft Foundry](foundry.md)
- [Security](security.md)
- [Code review](code-review.md)
- [Final verification](final-verification.md)
