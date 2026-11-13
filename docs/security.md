# Security notes

## Secrets

| Secret | Local development | Container / Azure |
| --- | --- | --- |
| JWT signing key | Generated once in `%TEMP%\marketlane-dev\jwt-secret.dev` | `Jwt__Secret` from a pipeline secret or Key Vault |
| Internal API key | Generated once in `%TEMP%\marketlane-dev\internal-api-key.dev` | `Internal__ApiKey` from a pipeline secret or Key Vault |
| SQL password | Not needed (SQLite) | `sqlAdminPassword` pipeline secret, stored in a Container Apps secret |
| Foundry key | Blank | `Foundry__ApiKey` Container Apps secret or Key Vault |
| Admin seed password | Optional environment variable | `Seed__AdminPassword` Container Apps secret |

Production refuses to start when `Jwt:Secret` or `Internal:ApiKey` is missing. There are no
built-in default credentials. The generated development files are outside the repository.

The ACR admin account is enabled so the pipeline can push images. Its credentials are read with
`listCredentials()` during deployment and placed in Container Apps secrets; they are not committed
or written to deployment outputs. A production deployment should use a managed identity with the
`AcrPull` role instead.

## Authentication and authorization

- The User Service issues short-lived JWT access tokens. Passwords are hashed with PBKDF2.
- The gateway rejects anonymous requests to cart, checkout, orders, payments, inventory, users and
  `/hubs` before proxying. It also requires authentication for catalog writes.
- Each service validates the token independently and enforces roles and ownership. A customer
  cannot read another customer's order, and only administrators can mutate products, categories,
  inventory or order status.
- The SignalR hub accepts the token from `access_token` on `/hubs` paths because browser
  WebSocket clients cannot set an `Authorization` header.

## Service-to-service access

`/api/internal` endpoints require `X-Internal-Api-Key`. They are not routed by the gateway, so
they are not reachable through the public API. The key is compared in constant time.

## CORS

Services allow only the configured frontend origins. In Docker and Azure the browser calls the
frontend origin and nginx proxies `/api` and `/hubs` to the gateway, so CORS is normally not
involved. CORS remains configured for direct gateway use and local Vite development.

## Network exposure

Only the gateway and frontend Container Apps use external ingress. Product, User and Order
Services use internal ingress. The frontend reaches the gateway over the Container Apps
environment network.

## Containers

The four .NET images use the runtime image's non-root `APP_UID` and own `/app` and `/app/data`,
so the SQLite files and published binaries are writable without running as root. The nginx
frontend uses the upstream image defaults. Images were not built or started in the preparation
environment because no Docker daemon was available; this is a static Dockerfile review, not a
runtime assertion.

## Azure SQL

The shared deployment has `AllowAzureServices` enabled so the Container Apps environment can
connect. This is broader than necessary. Restrict the firewall to the environment's outbound
addresses, or use a private endpoint, before treating the deployment as production-ready.

## Payment data

`MockPaymentProvider` simulates payments. It accepts no card numbers and stores no payment
credentials. A real provider must be added behind `IPaymentProvider` with PCI-compliant hosting.

## Client token storage

The demo stores the JWT in `localStorage`. A hardened storefront should use an http-only,
same-site cookie or short-lived access tokens with refresh rotation. This is a known demo
trade-off, not a claim of production readiness.

## Logging

The services log provider failures and validation problems, but do not log JWT values, internal
API keys or Foundry keys. The Foundry client truncates error bodies to 500 characters before
logging. Review logs before adding request-body logging in production.
