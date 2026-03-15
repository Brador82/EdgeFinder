using EdgeFinder.Core.Models;

namespace EdgeFinder.Core.Abstractions;

public interface IOddsService
{
    string? RequestsRemaining { get; }
    Task<List<OddsGame>> FetchOddsAsync(string sportKey);
}
