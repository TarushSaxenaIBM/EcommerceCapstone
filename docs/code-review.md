# Code review

Review date: 2026-11-05

Scope: service boundaries, data ownership, authentication, checkout consistency, AI fallbacks,
secrets, containers, Bicep and tests.

## Summary

The split keeps the original storefront contract and moves functionality into Product, User and
Order services without cross-database access. The main correctness risks are checkout consistency
and secret handling; both have explicit safeguards and tests. Remaining items are documented
production follow-ups rather than hidden defects.

## Findings and resolutions

### High: cross-service stock consistency

**Finding:** Checkout could reserve stock and then fail while creating the order, payment or cart
update, leaving stock permanently reduced.

**Resolution:** `CheckoutService` tracks each successful reservation and releases them in a
`catch` block after any failure. The Order Service calls the Product Service's internal reserve and
release endpoints through `IProductCatalogClient`. Integration tests cover successful checkout and
the changed-stock conflict.

**Residual risk:** Release is best-effort. If the Product Service is unreachable during
compensation, the failure is logged and stock remains reserved. A production system should add an
outbox or reconciliation job. This is documented, not silently ignored.

### High: service-to-service endpoints exposed publicly

**Finding:** Internal catalog and inventory endpoints must not be callable by browsers.

**Resolution:** The gateway has no `/api/internal` route. `InternalApiKeyMiddleware` protects the
prefix and compares the key in constant time. The key is a separate secret from the JWT key.

### High: missing production secrets

**Finding:** A service could start with an empty JWT or internal key.

**Resolution:** `ServiceSecrets` throws in non-Development environments when either value is
missing. The JWT secret must be at least 32 characters. Development generates temporary files under
`%TEMP%\marketlane-dev\`; nothing is committed.

### Medium: duplicate-data drift

**Finding:** Carts and orders cannot join to the User or Product databases.

**Resolution:** Cart and order lines store product snapshots, and orders store customer name/email
snapshots. Checkout re-reads current products for price and stock, so stale cart prices do not
become the charged price.

**Residual risk:** A product renamed after checkout keeps its historical name in the order. That is
intentional for order history.

### Medium: AI provider failure could break user flows

**Finding:** A slow or failing Foundry endpoint must not make search, recommendations or chat fail.

**Resolution:** `FoundryChatClient` never throws for provider failures; it logs and returns `null`.
Each caller has a deterministic fallback. `SemanticSearchResponse.UsedAi`,
`RecommendationResponse.AiRanked` and `ChatResponse.UsedAi` expose which path ran. The integration
suite asserts the fallback path when Foundry is unconfigured.

### Medium: gateway routing and auth drift

**Finding:** Adding a protected API later could accidentally bypass the edge check.

**Resolution:** The gateway lists protected prefixes explicitly and requires authentication for
catalog/category writes. Every service still validates the JWT and ownership, so the edge check is
defence in depth. Gateway `401` and unreachable-upstream `502` contracts are tested.

### Medium: test hosts initially disagreed on secrets

**Finding:** Integration hosts generated different development secrets because
`ServiceSecrets` resolves configuration before `WebApplicationFactory` adds its in-memory settings.
The Order Service then received `401` from the Product Service.

**Resolution:** `StoreFixture` sets `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` and
`Internal__ApiKey` as process environment variables before creating the hosts, then clears them on
disposal. This matches how production configuration is supplied and keeps the test suite
deterministic.

### Medium: frontend container used a fixed gateway URL

**Finding:** A hard-coded `proxy_pass http://gateway:8080` would break in Azure, where the gateway
has a different host name and ingress port.

**Resolution:** The image uses an nginx template. `GATEWAY_UPSTREAM` is substituted at container
start: `http://gateway:8080` in Compose and `http://gateway-<env>` in Container Apps. The browser
continues to use relative `/api` and `/hubs` paths.

### Medium: vulnerable transitive package

**Finding:** `System.Security.Cryptography.Pkcs` 9.0.11 produced NU1902 and NU1903 warnings
(GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx).

**Resolution:** A direct reference to the patched 10.0.14 package is pinned in
`ECommerce.Infrastructure.csproj`. The solution build no longer reports the advisory.

### Resolved: retired monolith source removed

**Finding:** `src/ECommerce.API` was no longer in the solution but its source and local database
files were still present, which made the repository harder to navigate.

**Resolution:** The retired monolith was deleted. The three services, gateway and frontend are the
only application projects in the repository.

### Low: demo token storage

**Finding:** The frontend keeps the JWT in `localStorage`.

**Resolution:** Documented as a demo trade-off. A hardened deployment should use http-only cookies
or refresh-token rotation. Changing it now would broaden the scope beyond the requested split.

## Checks performed

- No hard-coded JWT, internal, SQL, ACR or Foundry secrets in source.
- No `Console.WriteLine` or logging of token values.
- Every service has its own connection string and context.
- Gateway does not route `/api/internal`.
- Internal key comparison is constant time.
- Checkout compensates reservations on failure.
- Order ownership checks return `404` instead of leaking existence.
- AI fallbacks are explicit and tested.
- The four .NET images switch to the non-root `APP_UID` user and own `/app` plus `/app/data`;
  the nginx frontend keeps the upstream image's default worker model. No privileged mode is used.
- Bicep secrets use `@secure()` parameters or runtime credential lookup, not outputs.
- Pipeline secrets are mapped through `env:` blocks rather than interpolated into log lines.

## Recommended follow-ups

1. Replace the ACR admin account with managed identity and `AcrPull`.
2. Move SQL, JWT, internal and Foundry secrets to Key Vault with managed identity.
3. Add SQL Server migrations or a migration bundle before production schema changes.
4. Add an outbox/reconciliation job for reservation compensation.
5. Add rate limiting to `/api/chat` and `/api/products/search`.
6. Add OpenTelemetry tracing across the gateway and services.
