namespace EdgeFinder.Core.Abstractions;

public interface IQueryAnalyzer
{
    Task<QueryIntent> AnalyzeAsync(string question, byte[]? imageData = null, CancellationToken ct = default);
}

public record QueryIntent(
    string Domain,
    string[] Entities,
    string? TimeFrame,
    string[] RequiredDataTypes,
    string NormalizedQuestion);
