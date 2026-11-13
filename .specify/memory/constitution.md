# Market Lane Constitution

## Core principles

1. **Preserve working behaviour.** The storefront design, checkout rules, authentication and
   order lifecycle remain compatible with the original monolith unless a documented change is
   required by the service split.
2. **One owner per data set.** Products, categories and inventory belong to the Product Service;
   accounts and roles belong to the User Service; carts, orders and payments belong to the Order
   Service. No service reads another service's tables.
3. **Contract-first integration.** Cross-service calls use explicit HTTP contracts, typed clients
   and a shared internal API key. Failures degrade predictably instead of returning inconsistent
   data.
4. **Secrets are external.** No secret is committed to source, baked into an image or written to a
   deployment output. Local development may generate temporary secrets outside the repository.
5. **AI is optional.** Every AI feature has a deterministic fallback and reports whether AI was
   actually used. A missing provider must not break browsing, checkout or support.
6. **Verification before claims.** A feature is only reported as working when it was built,
   tested or executed. Blocked external dependencies are documented explicitly.

## Delivery constraints

- Keep .NET 10 and the existing React/Vite frontend.
- Use SQLite for local development and Docker; support Azure SQL through configuration.
- Keep the public API paths `/api` and `/hubs` unchanged so the frontend needs no rewrite.
- Prefer simple, well-understood components over new infrastructure.
- Every new behaviour needs a unit or integration test, or a documented reason why it cannot be
  tested in this environment.

## Quality gates

- `dotnet build ECommerce.sln` succeeds.
- `dotnet test ECommerce.sln` passes.
- `npm run build` succeeds in `frontend/`.
- `docker compose config` validates with test secrets.
- No committed secrets and no unverified claims of deployment or AI success.
