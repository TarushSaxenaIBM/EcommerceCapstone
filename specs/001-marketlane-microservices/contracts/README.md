# API contracts

The generated OpenAPI documents in [`openapi/`](../../../openapi) are the source of truth for
request and response shapes. This file records the stable route ownership used by the gateway.

## Public routes through the gateway

| Method | Route | Auth | Owner |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Anonymous | User |
| POST | `/api/auth/login` | Anonymous | User |
| GET | `/api/auth/me` | Bearer | User |
| PUT | `/api/users/me` | Bearer | User |
| GET | `/api/users` | Admin | User |
| PATCH | `/api/users/{id}/role` | Admin | User |
| GET | `/api/products` | Anonymous | Product |
| GET | `/api/products/{id}` | Anonymous | Product |
| POST | `/api/products/search` | Anonymous | Product |
| GET | `/api/products/suggestions` | Anonymous | Product |
| POST/PUT/DELETE | `/api/products`, `/api/products/{id}` | Admin | Product |
| GET | `/api/categories`, `/api/categories/{id}` | Anonymous | Product |
| POST/PUT/DELETE | `/api/categories`, `/api/categories/{id}` | Admin | Product |
| GET/PUT | `/api/inventory/{productId}` | Admin | Product |
| POST | `/api/inventory/{productId}/adjust` | Admin | Product |
| GET | `/api/cart` | Bearer | Order |
| POST | `/api/cart/items` | Bearer | Order |
| PUT/DELETE | `/api/cart/items/{itemId}` | Bearer | Order |
| DELETE | `/api/cart` | Bearer | Order |
| POST | `/api/checkout` | Bearer | Order |
| GET | `/api/orders` | Bearer | Order |
| GET | `/api/orders/{id}` | Bearer | Order |
| GET | `/api/orders/all` | Admin | Order |
| PATCH | `/api/orders/{id}/status` | Admin | Order |
| GET | `/api/payments/{orderId}` | Bearer | Order |
| GET | `/api/recommendations/home` | Anonymous | Order |
| GET | `/api/recommendations/related/{productId}` | Anonymous | Order |
| POST | `/api/chat` | Anonymous | Order |
| WebSocket | `/hubs/orders` | Bearer | Order |

## Internal routes

These routes are reachable only inside the service network and require
`X-Internal-Api-Key`. The gateway does not route `/api/internal`.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/internal/products/{id}` | Read one product |
| GET | `/api/internal/products?ids=1,2` | Read several products |
| GET | `/api/internal/products/featured?take=8` | Read featured products |
| POST | `/api/internal/inventory/{id}/reserve` | Reserve stock |
| POST | `/api/internal/inventory/{id}/release` | Release stock |

## Error contract

All handled errors use `application/problem+json` with `title`, `status` and `detail`:

- `400` validation failure or invalid search query
- `401` missing or invalid JWT, or invalid internal key
- `403` authenticated but not allowed
- `404` missing resource or another customer's order
- `409` duplicate email, SKU conflict or stock conflict
- `502` unreachable downstream service at the gateway
- `503` internal service dependency failure
- `504` downstream timeout at the gateway

## Health

Every service exposes `GET /health` and `GET /health/ready`. The Container Apps probes use these
paths.
