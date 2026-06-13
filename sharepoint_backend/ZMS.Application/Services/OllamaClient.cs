using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ZMS.Core.Security;

namespace ZMS.Application.Services;

public interface IOllamaClient
{
    string Model { get; }
    Task<(bool IsAvailable, string? Answer, string? Warning)> GenerateAsync(string systemPrompt, string userPrompt, object context, CancellationToken cancellationToken);
}

public class OllamaClient : IOllamaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public OllamaClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["OLLAMA_BASE_URL"] ?? configuration["Ollama:BaseUrl"] ?? "http://localhost:11434");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        Model = configuration["OLLAMA_MODEL"] ?? configuration["Ollama:Model"] ?? "llama3.1";
    }

    public string Model { get; }

    public async Task<(bool IsAvailable, string? Answer, string? Warning)> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        object context,
        CancellationToken cancellationToken)
    {
        var sanitizedSystemPrompt = SecretRedactor.Redact(systemPrompt);
        var sanitizedUserPrompt = SecretRedactor.Redact(userPrompt);
        var sanitizedContextJson = SecretRedactor.Redact(JsonSerializer.Serialize(SecretRedactor.RedactObject(context), JsonOptions));
        var prompt = $"""
        {sanitizedSystemPrompt}

        User question:
        {sanitizedUserPrompt}

        Platform context JSON:
        {sanitizedContextJson}
        """;

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model = Model,
                    prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.2
                    }
                },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, $"Ollama returned HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, cancellationToken);
            return string.IsNullOrWhiteSpace(payload?.Response)
                ? (false, null, "Ollama returned an empty response.")
                : (true, payload.Response.Trim(), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return (false, null, "Ollama is unavailable. Deterministic fallback guidance was returned.");
        }
    }

    private sealed class OllamaGenerateResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}
