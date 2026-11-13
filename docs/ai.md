# AI and security

## Where AI is used

Four features can call a model, and all four still work when no model is configured.

| Feature | Entry point | Without a model |
| --- | --- | --- |
| Support chat | `ChatbotService` | Reads the retrieved store facts and the customer's own orders |
| Recommendations | `RecommendationService` | Sorts by past purchases, popularity and stock |
| Semantic search | `ProductService` | Parses the phrase into keywords, category and price range |
| Suggestions | `ProductService` | Matches product names as the shopper types |

Responses carry `usedAi` or `aiRanked`, so the UI and the tests can tell which path answered.

## Configuration

| Setting | Default | Meaning |
| --- | --- | --- |
| `Foundry__Endpoint` | empty | Resource endpoint, for example `https://name.services.ai.azure.com` |
| `Foundry__ApiKey` | empty | Key for that resource |
| `Foundry__ChatDeployment` | empty | Deployment or model name |
| `Foundry__Mode` | `auto` | `auto`, `foundry` or `azure-openai` request shape |
| `Foundry__ApiVersion` | `2024-10-21` | API version used in the URL |
| `Foundry__ChatPath` | empty | Overrides the computed path |
| `Foundry__TimeoutSeconds` | `20` | HTTP timeout |

`FoundryOptions.IsConfigured` only checks endpoint and key. If either is empty, the AI client is
never called and the deterministic path runs.

For local runs, put the values in a file that is ignored by git and load them before starting the
Order and Product services. Do not commit keys.

## The Foundry client

`FoundryChatClient` builds a chat-completions request from the options above and posts it with
`api-key` and `Bearer` headers. If the request fails, times out or returns an empty body, it logs
the failure and returns `null`. The caller then uses its fallback. This keeps a missing or broken
model from breaking the storefront.

## Retrieval before generation

`ECommerce.Application/Knowledge/StoreKnowledge.cs` holds the policy facts the assistant is
allowed to use: shipping, delivery, returns, refunds, exchanges, cancellation, warranty, GST,
support hours, bulk orders and stock.

When a question arrives, `StoreKnowledge.Search`:

1. lowercases the question and removes stop words;
2. scores each fact by how many question words appear in its topic and text;
3. returns the best three facts.

Those facts are put into the prompt under `RETRIEVED STORE POLICIES`, and the prompt tells the
model to answer only from them. When no model is configured, the same retrieved text is returned
directly, so the answer is still grounded in approved policy rather than invented.

Keyword retrieval was chosen over embeddings because the fact list is small and the behaviour is
easy to test. A real deployment would move the facts into a search index or a vector store and
keep the same retrieve-then-answer shape.

## The two agent flows

**Customer support agent** (`ChatbotService`) has four tools it can call:

- `StoreKnowledge.Search`: approved policy text.
- `IOrderRepository.GetByUserIdAsync`: the signed-in customer's orders only.
- `IProductCatalogClient`: the catalog, for product questions.
- `IAiChatClient`: the model, when configured.

It checks the order intent first, then retrieved policies, then product matches, then a greeting.
`UsedAi` shows whether the model or the fallback answered.

**Recommendation agent** (`RecommendationService`) looks at what the customer bought before, the
categories they prefer, how popular each product is and how much stock is left. If a model is
configured it re-ranks the shortlist; otherwise the rule-based order stands. Results are cached
for five minutes per user.

## Responsible AI and security notes

- The assistant only receives the signed-in customer's own orders. Order lookups filter by user
  id, and a question about someone else's order number gets "not on this account".
- The system prompt forbids inventing statuses, prices or stock, and limits answers to store
  topics.
- No API keys are committed. Development keys are generated per machine; deployment keys come
  from pipeline variables or the environment.
- Card numbers are never collected. `MockPaymentProvider` returns a fake transaction id.
- Model output is treated as text only. It is never used to write orders, prices or stock.
- Every AI path has a deterministic fallback, and the response says which path ran.

## What is not verified

No Foundry resource, key or deployment was available, so no live model call has been made and no
workspace has been created. The client, configuration and fallbacks are in place; the checklist
marks the live steps as pending. The retrieval step, the fallbacks and the agent flows run and are
covered by the integration tests.
