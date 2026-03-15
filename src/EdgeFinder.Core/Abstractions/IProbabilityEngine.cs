namespace EdgeFinder.Core.Abstractions;

public interface IProbabilityEngine
{
    Task<ProbabilityResult> CalculateAsync(
        QueryIntent intent,
        List<DataPayload> data,
        CancellationToken ct = default);
}

public record ProbabilityResult(
    double Probability,
    double Confidence,
    double? Edge,
    List<KpiEntry> KPIs,
    string Reasoning,
    List<string> DataSourcesUsed);

public record KpiEntry(string Label, string Value, string? Context = null);
