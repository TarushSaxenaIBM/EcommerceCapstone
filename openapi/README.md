# OpenAPI documents

Generated from the running services in Development mode with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/export-openapi.ps1
```

| File | Service | Notes |
| --- | --- | --- |
| [product-service.json](product-service.json) | Product Service | Catalog, categories, inventory, search, suggestions and internal endpoints |
| [user-service.json](user-service.json) | User Service | Registration, login, profile, user listing and role changes |
| [order-service.json](order-service.json) | Order Service | Cart, checkout, orders, payments, recommendations and chat |
| [gateway.json](gateway.json) | Gateway | Gateway-owned `/health` only |

The gateway proxies YARP routes configured in code rather than controllers, so its proxied paths
do not appear in the Swagger document. Route ownership is documented in
[the contract summary](../specs/001-marketlane-microservices/contracts/README.md).

The `/api/internal` endpoints appear in the Product Service document for completeness. They are not
exposed by the gateway and require the internal API key.
