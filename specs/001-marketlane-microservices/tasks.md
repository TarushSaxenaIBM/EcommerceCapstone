# Tasks: Market Lane microservices

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

Status legend: `[x]` completed and verified, `[~]` completed but external verification blocked,
`[ ]` not done.

## Phase 1 - Audit and design

- [x] T001 Audit the monolith, tests, frontend and dependencies.
- [x] T002 Record service ownership and data boundaries.
- [x] T003 Record compatibility constraints for `/api`, `/hubs` and the frontend.
- [x] T004 Create the implementation checklist.

## Phase 2 - Domain and application refactor

- [x] T005 Add order-owned shipping addresses and customer snapshots.
- [x] T006 Add product snapshots to cart and order lines.
- [x] T007 Add profile and role management services.
- [x] T008 Add recommendation, chat and semantic search services with AI seams.
- [x] T009 Add validation for new requests and conflict paths.

## Phase 3 - Service split

- [x] T010 Create `ECommerce.ServiceDefaults`.
- [x] T011 Create `ECommerce.AI`.
- [x] T012 Add Product, User and Order DbContexts and repositories.
- [x] T013 Add per-service seeders and database initialization.
- [x] T014 Generate SQLite migrations for the three contexts.
- [x] T015 Create the Product Service host.
- [x] T016 Create the User Service host.
- [x] T017 Create the Order Service host.
- [x] T018 Add the internal product catalog client with compensation.
- [x] T019 Remove the legacy API project from the solution.

## Phase 4 - Gateway

- [x] T020 Add YARP routes for catalog, auth, cart, orders, payments, AI and SignalR.
- [x] T021 Add edge authorization rules.
- [x] T022 Map forwarding failures to `502`/`504` problem+json.
- [x] T023 Verify gateway authentication and failure contracts.

## Phase 5 - Tests

- [x] T024 Rewrite unit tests for the new service boundaries.
- [x] T025 Create an in-process multi-service integration fixture.
- [x] T026 Test checkout, stock conflict, ownership and role enforcement.
- [x] T027 Test recommendations, chat fallback and semantic search.
- [x] T028 Test gateway `401` and `502` responses.
- [x] T029 Run the full solution test suite (45 tests).

## Phase 6 - Frontend

- [x] T030 Add home and related recommendations.
- [x] T031 Add semantic search and autocomplete suggestions.
- [x] T032 Add the support chat widget.
- [x] T033 Preserve the existing design language and responsive layout.
- [x] T034 Run the production frontend build.

## Phase 7 - Containers

- [x] T035 Add Dockerfiles for the three services and gateway.
- [x] T036 Add the multi-stage frontend image with an nginx template.
- [x] T037 Add `docker-compose.yml`, `.env.example` and `.dockerignore`.
- [~] T038 Validate Compose configuration (`docker compose config` passed).
- [ ] T039 Build and start the Compose stack (Docker daemon unavailable).

## Phase 8 - Azure DevOps

- [x] T040 Add build, test and frontend stages.
- [~] T041 Add the shared Bicep deployment stage (defined; not executed without Azure access).
- [~] T042 Add the ACR image build and push stage (defined; not executed without Azure access).
- [~] T043 Add the Container Apps deployment stage with secret-backed configuration (defined; not executed).
- [x] T044 Document service connection and variable group setup.

## Phase 9 - Azure infrastructure

- [x] T045 Add `infra/shared.bicep`.
- [x] T046 Add `infra/apps.bicep`.
- [x] T047 Add `infra/main.bicep`.
- [x] T048 Validate Bicep syntax with the Bicep CLI 0.49.2 (all three templates compile).
- [ ] T049 Deploy to an Azure subscription (no credentials or `az` CLI).

## Phase 10 - AI

- [x] T050 Add Foundry options and chat client.
- [x] T051 Add semantic search interpretation with keyword fallback.
- [x] T052 Add recommendation re-ranking with heuristic fallback.
- [x] T053 Add support chat with FAQ/order/catalog fallback.
- [x] T054 Document Foundry configuration and security.
- [ ] T055 Call a live Foundry deployment (no endpoint or key).

## Phase 11 - Specs and contracts

- [x] T056 Add Spec Kit constitution, spec, plan and tasks.
- [x] T057 Add an OpenAPI export script.
- [x] T058 Generate OpenAPI documents for Product, User, Order and Gateway.
- [~] T059 Run the Spec Kit CLI (not installed; artifacts are compatible by convention).

## Phase 12 - Review, documentation and verification

- [x] T060 Review code for ownership, secret and failure-handling issues.
- [x] T061 Rewrite the README for the microservice architecture.
- [x] T062 Add architecture, deployment, Foundry and security docs.
- [x] T063 Add the code review and final verification reports.
- [x] T064 Pin a patched `System.Security.Cryptography.Pkcs` version.
- [x] T065 Re-run solution build, tests and frontend build.
- [x] T066 Record Docker, Azure, Foundry and Spec Kit blockers precisely.
