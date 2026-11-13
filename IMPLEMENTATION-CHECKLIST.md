# Implementation Checklist — Microservices, Azure DevOps & AI

Source baseline: monolith (`ECommerce.API` + `Application` + `Infrastructure` + `Domain`), React storefront,
37 passing tests (25 unit + 12 integration), SQLite, JWT, SignalR.

Status legend: `[x]` completed and verified, `[~]` implemented but external verification blocked,
`[ ]` not done (blocked or follow-up).

## Phase 1 — Microservices
- [x] Audit existing code and dependencies
- [x] Split data model ownership (Product, User, Order)
- [x] Product Service (catalog, categories, inventory, search)
- [x] User Service (register, login, JWT, profile, roles)
- [x] Order Service (cart, checkout, orders, payments, notifications)
- [x] REST service-to-service communication with graceful failure
- [x] Per-service databases and migrations

## Phase 2 — Gateway
- [x] YARP API gateway on port 5080 (frontend-compatible)
- [x] JWT validation at the gateway
- [x] Downstream failure handling (502/503/504 problem+json)
- [x] SignalR WebSocket passthrough

## Phase 3 — Docker
- [x] Multi-stage Dockerfiles for 3 services + gateway + frontend
- [x] docker-compose with health checks, volumes, env configuration
- [~] Build and run verification — `docker compose config` passes; daemon unavailable, so images were not built or started

## Phase 4 — Azure DevOps
- [x] Pipeline YAML covering services, gateway and frontend (one multi-stage pipeline instead of duplicated files)
- [x] Build, test, Docker push and deployment stages
- [x] Service connections/variables documented (no secrets committed)

## Phase 5 — Azure
- [x] Bicep infrastructure (ACR, Container Apps, Azure SQL, Log Analytics; secrets via Container Apps secrets)
- [x] Deployment guide and environment configuration
- [~] Actual deployment: BLOCKED (no Azure credentials/az CLI; Bicep compile verified only)

Key Vault and dedicated SQL Server migrations remain documented production follow-ups rather than
part of this deployment; secrets are supplied as `@secure()` parameters and Container Apps secrets.

## Phase 6 — Copilot improvements
- [x] Added unit tests for negative paths
- [x] Improved product search + suggestions
- [x] Validation and consistent errors
- [x] Documentation review

## Phase 7 — Recommendations
- [x] `/api/recommendations/home` (personalized) and `/api/recommendations/related/{productId}`
- [x] Foundry ranking with heuristic fallback + cache
- [x] Homepage and product page UI

## Phase 8 — Chatbot
- [x] `/api/chat` with authorized order lookup
- [x] Foundry chat with FAQ/heuristic fallback
- [x] Global chat widget

## Phase 9 — Semantic search
- [x] Query interpretation through Foundry, keyword fallback
- [x] Suggestions endpoint and UI
- [x] Empty/error handling

## Phase 10 — Spec Kit / OpenAPI
- [~] Spec Kit-compatible specs (CLI not installed; artifacts created and documented)
- [x] OpenAPI for Product, User, Order and Gateway
- [x] Contract checks in integration tests

## Phase 11 — Review / docs
- [x] Code review fixes
- [x] README and supporting docs

## Phase 12 — Verification
- [x] dotnet build / test — 0 warnings, 0 errors; 28 unit + 17 integration tests pass
- [x] frontend build — production bundle produced
- [~] docker compose build / up / smoke tests — configuration validated, runtime blocked without a Docker daemon
- [x] Bicep compile — `shared.bicep`, `apps.bicep`, `main.bicep` compile with Bicep CLI 0.49.2

See [docs/final-verification.md](docs/final-verification.md) for commands, results and blockers.
