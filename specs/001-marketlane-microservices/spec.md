# Feature Specification: Market Lane microservices

**Feature Branch**: `001-marketlane-microservices`
**Created**: 2026-11-05
**Status**: Implemented
**Input**: Split the working Market Lane e-commerce monolith into Product, User and Order services
behind a YARP gateway, package the result with Docker, deploy it to Azure through Bicep and Azure
DevOps, and add Microsoft Foundry recommendations, chat and semantic search without breaking the
existing storefront.

## User Scenarios & Testing

### User Story 1 - Browse and search the catalog (Priority: P1)

A shopper opens the storefront, browses products by category and searches with natural language.
The catalog remains available when the AI provider is not configured.

**Why this priority**: Catalog browsing is the entry point for every other flow.

**Independent Test**: Start the Product Service and gateway, request `/api/products`, then post a
search query and a suggestion query. Both return seeded data without authentication.

**Acceptance Scenarios**:

1. **Given** the seeded catalog, **when** the shopper loads the home page, **then** products and
   categories are shown through the gateway.
2. **Given** no AI credentials, **when** the shopper searches "lightweight running shoes", **then**
   the result contains relevant products and reports `usedAi: false`.
3. **Given** a partial product name, **when** the shopper types it, **then** suggestions return
   matching product names.

### User Story 2 - Register, sign in and manage a profile (Priority: P1)

A customer registers, signs in, reads their profile and updates their name. Administrators can
list accounts.

**Independent Test**: Register a customer through the gateway, use the returned token on
`/api/auth/me`, update the profile and verify the change.

**Acceptance Scenarios**:

1. **Given** a new email, **when** the customer registers, **then** the User Service returns a
   token and `201 Created`.
2. **Given** the same email, **when** registration is repeated, **then** the response is
   `409 Conflict`.
3. **Given** a customer token, **when** the customer updates `/api/users/me`, **then** the next
   `/api/auth/me` read reflects the new name.
4. **Given** a customer token, **when** the customer calls an admin-only endpoint, **then** the
   response is `403 Forbidden`.

### User Story 3 - Cart and checkout (Priority: P1)

A customer adds products to a cart, checks out with a shipping address, receives an order and a
simulated payment, and sees stock decrease exactly once.

**Why this priority**: Checkout is the revenue path and exercises cross-service communication.

**Independent Test**: Add two units of a seeded product, check out through the gateway, then verify
the order, payment, cart and stock.

**Acceptance Scenarios**:

1. **Given** an authenticated customer and available stock, **when** checkout succeeds, **then**
   the order is `Confirmed`, payment is `Successful`, the cart is empty and stock is reduced.
2. **Given** stock changed after the cart was filled, **when** checkout runs, **then** the response
   is `409 Conflict` and no order is created.
3. **Given** a failed step after stock reservation, **when** checkout aborts, **then** reserved
   stock is released.
4. **Given** another customer's order id, **when** a customer requests it, **then** the response is
   `404 Not Found`.

### User Story 4 - Order notifications and administration (Priority: P2)

Customers see live status changes. Administrators move orders to Processing and manage the
catalog and inventory.

**Independent Test**: Create an order, change its status as an administrator, and verify the
customer read and the SignalR notification contract.

**Acceptance Scenarios**:

1. **Given** a customer order, **when** an administrator patches the status, **then** the customer
   sees the new status.
2. **Given** a customer token, **when** the customer tries to change status, **then** the response
   is `403 Forbidden`.
3. **Given** an anonymous request to `/hubs`, **when** the gateway evaluates it, **then** the
   request is rejected with `401`.

### User Story 5 - Recommendations and support chat (Priority: P2)

A shopper sees home and related recommendations and can ask a support question. Both features work
without an AI provider and report whether AI ranked or answered.

**Independent Test**: Request `/api/recommendations/home`, `/api/recommendations/related/{id}` and
post a shipping question to `/api/chat` without Foundry settings.

**Acceptance Scenarios**:

1. **Given** no AI credentials, **when** recommendations are requested, **then** non-empty items
   are returned with `aiRanked: false`.
2. **Given** a shipping question, **when** chat runs without AI, **then** a deterministic answer is
   returned with `usedAi: false`.
3. **Given** a configured provider that fails, **when** any AI feature runs, **then** the request
   still succeeds through the fallback.

### User Story 6 - Deploy with containers and Azure (Priority: P3)

An operator builds five images, starts the stack locally and deploys the same configuration to
Azure through a pipeline.

**Independent Test**: Validate Compose configuration with test secrets; build the solution and the
frontend. Azure deployment requires subscription credentials.

**Acceptance Scenarios**:

1. **Given** test secrets, **when** `docker compose config` runs, **then** it exits successfully.
2. **Given** a service connection and variable group, **when** the pipeline runs, **then** it
   deploys shared resources, pushes images and deploys the apps.
3. **Given** missing Azure credentials, **when** deployment is attempted, **then** the limitation is
   documented and no success is claimed.

### Edge Cases

- Missing JWT or internal API key in Production fails fast instead of using a weak default.
- An unreachable downstream service returns `502` problem+json at the gateway.
- A malformed search query shorter than two characters returns `400`.
- Duplicate SKU or invalid category on product creation returns a validation error.
- A deleted or inactive product cannot be added to the cart.
- Empty carts cannot be checked out.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST expose Product, User and Order services with separate databases.
- **FR-002**: The gateway MUST preserve `/api` and `/hubs` paths used by the storefront.
- **FR-003**: Services MUST validate JWTs independently and enforce role and ownership rules.
- **FR-004**: Cross-service catalog calls MUST use an internal key and MUST NOT expose internal
  endpoints through the gateway.
- **FR-005**: Checkout MUST reserve stock before creating an order and compensate on failure.
- **FR-006**: Cart items and orders MUST store snapshots rather than cross-service foreign keys.
- **FR-007**: The frontend MUST keep its existing design and use relative API paths.
- **FR-008**: Semantic search, suggestions, recommendations and chat MUST work without AI.
- **FR-009**: AI responses MUST report whether AI was used.
- **FR-010**: Docker Compose MUST configure every service with environment variables and volumes.
- **FR-011**: Azure infrastructure MUST be defined in Bicep with no committed secrets.
- **FR-012**: The pipeline MUST build, test, push images and deploy without embedding secrets.
- **FR-013**: OpenAPI documents MUST be generated for each HTTP service.
- **FR-014**: Documentation MUST distinguish completed, partial, pending and blocked work.

### Key Entities

- **Product**: id, name, description, price, SKU, category, image, active flag, stock quantity.
- **Category**: id, name, description, active flag.
- **User**: id, first/last name, normalized email, password hash, role.
- **Cart / CartItem**: user id, product snapshot, quantity, unit price.
- **Order / OrderItem**: user id, customer snapshots, status, totals, timestamps, line snapshots.
- **Address**: order-owned shipping address.
- **Payment**: order id, status, amount, transaction reference.
- **RecommendationResponse / ChatResponse / SemanticSearchResponse**: AI metadata plus fallback
  results.

## Success Criteria

### Measurable Outcomes

- **SC-001**: `dotnet test ECommerce.sln` passes with zero failures.
- **SC-002**: The integration suite covers catalog, auth, roles, checkout, stock conflicts,
  recommendations, chat fallback and gateway failures.
- **SC-003**: `npm run build` produces a production bundle.
- **SC-004**: `docker compose config` validates with test secrets.
- **SC-005**: OpenAPI JSON exists for Product, User, Order and Gateway.
- **SC-006**: A reviewer can identify every externally blocked item without reading source code.

## Assumptions

- SQLite is acceptable for local and container runs; Azure SQL uses `EnsureCreated` until SQL
  Server migrations are generated.
- The demo payment provider remains simulated.
- Microsoft Foundry is optional and may be unavailable during verification.
- Azure deployment is performed by an operator with subscription permissions; the preparation
  environment has no Azure credentials.
