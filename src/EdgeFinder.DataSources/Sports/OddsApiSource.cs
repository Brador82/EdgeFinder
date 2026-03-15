using System.Text.Json;
using EdgeFinder.Core.Abstractions;
using EdgeFinder.Core.Models;

namespace EdgeFinder.DataSources.Sports;

public class OddsApiSource : IDataSource
{
    private readonly OddsService _odds;

    public string Name => "The Odds API";
    public string Domain => "sports";
    public string[] SupportedDataTypes => ["odds", "h2h_odds"];

    public OddsApiSource(OddsService odds)
    {
        _odds = odds;
    }

    public Task<bool> CanFulfillAsync(DataRequirement requirement)
    {
        var canFulfill = requirement.Domain == "sports"
                      && SupportedDataTypes.Contains(requirement.DataType);
        return Task.FromResult(canFulfill);
    }

    public async Task<DataPayload> FetchAsync(DataRequirement requirement, CancellationToken ct = default)
    {
        var sportKey = ResolveSportKey(requirement.Parameters);
        var games = await _odds.FetchOddsAsync(sportKey);

        // Filter to specific teams if requested
        if (requirement.Parameters.TryGetValue("team", out var team) && !string.IsNullOrEmpty(team))
        {
            var teamLower = team.ToLowerInvariant();
            games = games.Where(g =>
                g.HomeTeam.Contains(teamLower, StringComparison.OrdinalIgnoreCase) ||
                g.AwayTeam.Contains(teamLower, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var json = JsonSerializer.Serialize(games, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return new DataPayload(Name, "odds", json, DateTimeOffset.UtcNow);
    }

    private static string ResolveSportKey(Dictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("sport_key", out var key))
            return key;

        if (parameters.TryGetValue("sport", out var sport))
        {
            return sport.ToLowerInvariant() switch
            {
                "nfl" or "football" => "americanfootball_nfl",
                "nba" or "basketball" => "basketball_nba",
                "mlb" or "baseball" => "baseball_mlb",
                "nhl" or "hockey" => "icehockey_nhl",
                _ => "basketball_nba"
            };
        }

        return "basketball_nba";
    }
}
