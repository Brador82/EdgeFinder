using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EdgeFinder.Claude;

public class ClaudeClient
{
    private readonly HttpClient _http;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-20250514";

    public ClaudeClient(HttpClient http, string apiKey)
    {
        _http = http;
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> SendAsync(string systemPrompt, string userMessage, int maxTokens = 1024, CancellationToken ct = default)
    {
        var request = new
        {
            model = Model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(ApiUrl, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Claude API error {(int)response.StatusCode}: {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var textBlock = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return textBlock ?? "";
    }

    public async Task<string> SendWithImageAsync(string systemPrompt, string userMessage, byte[] imageData, string mediaType = "image/png", int maxTokens = 1024, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(imageData);

        var request = new
        {
            model = Model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mediaType,
                                data = base64
                            }
                        },
                        new
                        {
                            type = "text",
                            text = userMessage
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(ApiUrl, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Claude API error {(int)response.StatusCode}: {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var textBlock = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return textBlock ?? "";
    }
}
