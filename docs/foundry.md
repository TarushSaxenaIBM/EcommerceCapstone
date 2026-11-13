# Microsoft Foundry integration

AI features are optional. Every AI call goes through `IAiChatClient` in `ECommerce.AI`. When the
provider is not configured, returns an error or times out, the caller uses a deterministic
fallback and reports that AI was not used. No AI result is fabricated.

## Configuration

| Setting | Environment variable | Purpose |
| --- | --- | --- |
| `Foundry:Endpoint` | `Foundry__Endpoint` | Resource endpoint, for example `https://my-foundry.services.ai.azure.com` |
| `Foundry:ApiKey` | `Foundry__ApiKey` | API key (use Key Vault or a Container Apps secret in Azure) |
| `Foundry:ChatDeployment` | `Foundry__ChatDeployment` | Chat deployment or model name |
| `Foundry:Mode` | `Foundry__Mode` | `auto`, `deployments` or `models` |
| `Foundry:ApiVersion` | `Foundry__ApiVersion` | API version, default `2024-10-21` |
| `Foundry:ChatPath` | `Foundry__ChatPath` | Optional explicit request path |
| `Foundry:TimeoutSeconds` | `Foundry__TimeoutSeconds` | Request timeout, clamped to 5-120 seconds |

`Mode=auto` uses the `models/chat/completions` path for `services.ai.azure.com` endpoints and the
`openai/deployments/{deployment}/chat/completions` path otherwise. Set `Foundry__ChatPath` when a
resource uses a different route.

The client sends an `api-key` header and a standard chat-completions body. A non-success response
is logged and converted to `null`; callers then use their fallback.

## Where AI is used

### Semantic search (Product Service)

`POST /api/products/search` asks Foundry to interpret a natural-language query into keywords,
an optional category and a price range. The interpreted query is applied to the local catalog.
If AI is unavailable, or if the AI interpretation returns no matches, the endpoint falls back to
tokenized keyword matching. `UsedAi` in the response is `true` only when an AI interpretation was
applied successfully.

Example fallback response:

```json
{
  "items": [ { "id": 5, "name": "Trailhead Running Shoes" } ],
  "interpretedQuery": "lightweight, running, shoes",
  "usedAi": false
}
```

### Recommendations (Order Service)

`GET /api/recommendations/home` and `GET /api/recommendations/related/{productId}` rank featured
products with local signals first: previous purchases, preferred categories, popularity and
stock. When Foundry is configured, it is asked to re-rank the candidate IDs. If the response is
missing, malformed or fails, the local order is kept. `AiRanked` reports which path was used.
Results are cached for five minutes.

### Support chat (Order Service)

`POST /api/chat` first detects FAQ intents (shipping, returns), order-status questions and product
lookups. Authorized order facts are added to the prompt only for the signed-in user. Foundry
receives the message, conversation history and permitted facts. If it fails, the chatbot uses
rule-based FAQ answers, order-status data and catalog matches. `UsedAi` is `false` in that case.

The chat endpoint does not expose another customer's order. The fallback and the AI prompt both
use only the current user's order data.

## Local verification without credentials

Leave `FOUNDRY_*` blank (the default). Start the services and call:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/chat `
  -ContentType 'application/json' `
  -Body '{"message":"What is your shipping policy?"}'
```

The response has `usedAi: false` and a deterministic shipping answer. The integration suite
asserts this fallback path. Similarly:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/products/search `
  -ContentType 'application/json' `
  -Body '{"query":"lightweight running shoes"}'
```

returns local matches with `usedAi: false`.

These checks prove that the fallbacks work. They do **not** prove that a Foundry call succeeds.

## Azure configuration

The pipeline passes `Foundry__Endpoint`, `Foundry__ChatDeployment` and `Foundry__Mode` as plain
environment variables and `Foundry__ApiKey` as a Container Apps secret. For production, keep the
key in Key Vault and grant the Container App's managed identity access instead of copying the key
into a pipeline variable.

## Actual AI verification status

No Microsoft Foundry endpoint, key or deployment was available in the preparation environment.
No live Foundry request was made. The client, configuration surface, fallbacks and integration
tests are complete; live AI calls remain **BLOCKED** on credentials.
