# Implementation checklist

`[x]` done and verified · `[~]` implemented, live external check not possible · `[ ]` not done

1. **[x] Use Copilot to accelerate development and refactoring.**
   Copilot was used to split the original monolith into three services, to move the shared HTTP
   setup into `ECommerce.ServiceDefaults`, and to write the search, recommendation and chat
   features. The result is the `src/` layout described in the README.

2. **[x] Develop the rest of the UI pages.**
   `frontend/src/pages` contains the catalog, product detail, cart, checkout, orders, order
   details, login/register and not-found pages. Shared layout, formatting and the chat widget live
   in `frontend/src/components`.

3. **[x] Tests and documentation for the microservices.**
   31 unit tests and 17 integration tests cover the services, the gateway, authentication and
   failure paths. Documentation is in `docs/` and the commands are in the README.

4. **[x] Optimize the development workflow.**
   One solution, one shared service pipeline, one `docker-compose.yml`, three Dockerfiles and one
   Azure DevOps pipeline. `infra/` holds the Bicep modules. `dotnet test` and `npm run build` are
   the two local gates.

5. **[x] Build the customer service chatbot.**
   `ChatWidget.jsx` posts to `/api/chat`. `ChatbotService` handles the intent, retrieves store
   facts, looks up the signed-in customer's orders and calls the model if one is configured.

6. **[x] Chatbot with order tracking, FAQ and product recommendations.**
   Order tracking is limited to the signed-in customer's own orders. FAQ answers come from
   `StoreKnowledge`. Product matches are attached to the reply. The widget is part of the running
   storefront; it has not been deployed to Azure.

7. **[ ] Set up the Microsoft Foundry workspace and explore the model catalog.**
   Blocked. No Azure subscription, Foundry resource or model deployment was available. The client
   and settings are ready in `src/ECommerce.AI`, and `docs/ai.md` lists what to fill in.

8. **[x] AI agents for customer service and product recommendations.**
   `ChatbotService` is the support agent, with four tools (knowledge search, order lookup, catalog,
   model). `RecommendationService` is the recommendation agent. Both have deterministic paths, and
   both report whether the model was used. Live Foundry calls are not verified.

9. **[x] RAG over enterprise data.**
   `StoreKnowledge` is the approved policy data. `Search` retrieves the best facts for a question,
   the assistant puts them into the prompt, and the same facts answer the question when no model is
   configured. Live model augmentation is not verified.

10. **[x] Governance and responsible AI.**
    No secrets in source, own-orders-only prompts, no card data, safe fallbacks and `usedAi`
    transparency. The rules are written down in `docs/ai.md`. There was no formal governance
    review, which is the only part of this task that cannot be produced by the code.

11. **[x] Small order-management change with GitHub Spec Kit.**
    `PATCH /api/orders/{id}/status` lets an admin move an order to a new status, with validation and
    tests. The spec, plan and tasks are in `specs/001-marketlane-microservices/`. The `specify` CLI
    was not installed, so those files were written by hand in the same format.

## What still needs credentials

Task 7 is the only open item, and tasks 6, 8 and 9 have a live-call part that could not be checked:

1. Create a Foundry resource and a chat model deployment.
2. Set `Foundry__Endpoint`, `Foundry__ApiKey` and `Foundry__ChatDeployment` for the Order and
   Product services.
3. Restart those two services and ask the chatbot a policy question. `usedAi` should be `true`.

Until that is done, the code is honest about using the fallback.

## Verification run

| Check | Result |
| --- | --- |
| `dotnet build ECommerce.sln -c Release` | 0 warnings, 0 errors |
| `dotnet test ECommerce.sln -c Release` | 31 unit + 17 integration passed |
| `npm run build` in `frontend` | passed |
| Local services and storefront | started, catalog and chat answered |
| Docker image build | not run, Docker daemon unavailable |
| Azure deployment | not run, no subscription or CLI |
| Live Foundry call | not run, no key |
