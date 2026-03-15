using System.Text.Json;
using EdgeFinder.Claude.Prompts;
using EdgeFinder.Core.Abstractions;

namespace EdgeFinder.Claude;

public class ClaudeQueryAnalyzer : IQueryAnalyzer
{
    private readonly ClaudeClient _client;

    public ClaudeQueryAnalyzer(ClaudeClient client)
    {
        _client = client;
    }

    public async Task<QueryIntent> AnalyzeAsync(string question, byte[]? imageData = null, CancellationToken ct = default)
    {
        string response;

        if (imageData != null)
        {
            response = await _client.SendWithImageAsync(
                QueryAnalysisPrompt.System,
                question,
                imageData,
                ct: ct);
        }
        else
        {
            response = await _client.SendAsync(
                QueryAnalysisPrompt.System,
                question,
                ct: ct);
        }

        return ParseResponse(response, question);
    }

    private static QueryIntent ParseResponse(string response, string originalQuestion)
    {
        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var domain = root.GetProperty("domain").GetString() ?? "general";
            var entities = root.GetProperty("entities").EnumerateArray()
                .Select(e => e.GetString() ?? "").ToArray();
            var timeFrame = root.TryGetProperty("timeFrame", out var tf)
                ? tf.GetString() : null;
            var dataTypes = root.GetProperty("requiredDataTypes").EnumerateArray()
                .Select(e => e.GetString() ?? "").ToArray();
            var normalized = root.GetProperty("normalizedQuestion").GetString()
                ?? originalQuestion;

            return new QueryIntent(domain, entities, timeFrame, dataTypes, normalized);
        }
        catch
        {
            // Fallback: treat as general sports question
            return new QueryIntent(
                "sports",
                [originalQuestion],
                null,
                ["odds", "h2h_odds"],
                originalQuestion);
        }
    }
}
