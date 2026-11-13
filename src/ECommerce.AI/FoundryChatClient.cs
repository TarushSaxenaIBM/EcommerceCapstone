using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.AI;

/// <summary>
/// Calls a Microsoft Foundry or Azure OpenAI chat completions endpoint. The client
/// never throws to callers: when the provider is unavailable it returns
/// <c>null</c> so the application can use its deterministic fallback.
/// </summary>
public sealed class FoundryChatClient : IAiChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly FoundryOptions _options;
    private readonly ILogger<FoundryChatClient> _logger;

    public FoundryChatClient(
        HttpClient httpClient,
        IOptions<FoundryOptions> options,
        ILogger<FoundryChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 120));
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri())
            {
                Content = JsonContent.Create(new
                {
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.2,
                    max_tokens = 600
                })
            };

            request.Headers.TryAddWithoutValidation("api-key", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Foundry request failed with {StatusCode}. Body: {Body}",
                    (int)response.StatusCode,
                    body.Length > 500 ? body[..500] : body);

                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, ct);

            return payload?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Foundry chat completion failed; the fallback will be used.");
            return null;
        }
    }

    private Uri BuildRequestUri()
    {
        var baseUri = _options.Endpoint.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(_options.ChatPath))
        {
            var path = _options.ChatPath.TrimStart('/');
            return new Uri($"{baseUri}/{path}{QuerySeparator(path)}api-version={Uri.EscapeDataString(_options.ApiVersion)}");
        }

        var mode = _options.Mode.Trim().ToLowerInvariant();
        var useModelsPath = mode == "models" ||
                            (mode == "auto" && baseUri.Contains("services.ai.azure.com", StringComparison.OrdinalIgnoreCase));

        var requestPath = useModelsPath
            ? "models/chat/completions"
            : $"openai/deployments/{Uri.EscapeDataString(_options.ChatDeployment)}/chat/completions";

        return new Uri($"{baseUri}/{requestPath}?api-version={Uri.EscapeDataString(_options.ApiVersion)}");
    }

    private static string QuerySeparator(string path) => path.Contains('?') ? "&" : "?";

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
