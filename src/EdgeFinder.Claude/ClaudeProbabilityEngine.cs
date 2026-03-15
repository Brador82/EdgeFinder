using System.Text.Json;
using EdgeFinder.Claude.Prompts;
using EdgeFinder.Core.Abstractions;

namespace EdgeFinder.Claude;

public class ClaudeProbabilityEngine : IProbabilityEngine
{
    private readonly ClaudeClient _client;

    public ClaudeProbabilityEngine(ClaudeClient client)
    {
        _client = client;
    }

    public async Task<ProbabilityResult> CalculateAsync(
        QueryIntent intent,
        List<DataPayload> data,
        CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(intent, data);

        var response = await _client.SendAsync(
            ProbabilityInterpretationPrompt.System,
            userMessage,
            maxTokens: 2048,
            ct: ct);

        return ParseResponse(response, data);
    }

    private static string BuildUserMessage(QueryIntent intent, List<DataPayload> data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Question: {intent.NormalizedQuestion}");
        sb.AppendLine($"Domain: {intent.Domain}");
        sb.AppendLine($"Entities: {string.Join(", ", intent.Entities)}");

        if (intent.TimeFrame != null)
            sb.AppendLine($"Time frame: {intent.TimeFrame}");

        sb.AppendLine();
        sb.AppendLine("Available data:");

        foreach (var payload in data)
        {
            sb.AppendLine($"\n--- {payload.SourceName} ({payload.DataType}) ---");
            // Truncate very large payloads to stay within token limits
            var rawData = payload.RawData.Length > 8000
                ? payload.RawData[..8000] + "\n... (truncated)"
                : payload.RawData;
            sb.AppendLine(rawData);
        }

        if (data.Count == 0)
        {
            sb.AppendLine("No external data available. Use your knowledge to provide the best estimate.");
        }

        return sb.ToString();
    }

    private static ProbabilityResult ParseResponse(string response, List<DataPayload> data)
    {
        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var probability = root.GetProperty("probability").GetDouble();
            var confidence = root.GetProperty("confidence").GetDouble();

            double? edge = null;
            if (root.TryGetProperty("edge", out var edgeProp) && edgeProp.ValueKind == JsonValueKind.Number)
                edge = edgeProp.GetDouble();

            var kpis = new List<KpiEntry>();
            if (root.TryGetProperty("kpis", out var kpisArr))
            {
                foreach (var kpi in kpisArr.EnumerateArray())
                {
                    var label = kpi.GetProperty("label").GetString() ?? "";
                    var value = kpi.GetProperty("value").GetString() ?? "";
                    var context = kpi.TryGetProperty("context", out var ctx)
                        ? ctx.GetString() : null;
                    kpis.Add(new KpiEntry(label, value, context));
                }
            }

            var reasoning = root.GetProperty("reasoning").GetString() ?? "";
            var sources = data.Select(d => d.SourceName).Distinct().ToList();

            return new ProbabilityResult(probability, confidence, edge, kpis, reasoning, sources);
        }
        catch
        {
            return new ProbabilityResult(
                0.5, 0.1, null,
                [new KpiEntry("Error", "Could not parse probability response")],
                "Failed to analyze — try rephrasing your question.",
                data.Select(d => d.SourceName).Distinct().ToList());
        }
    }
}
