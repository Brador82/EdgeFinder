using System.Text.Json;

namespace EdgeFinder;

public static class Config
{
    public const decimal InitBankroll = 100m;
    public const double KellyFraction = 0.5;
    public const double MinEdge = 0.03;

    public static readonly (string Key, string Label)[] Sports =
    [
        ("americanfootball_nfl", "NFL"),
        ("basketball_nba", "NBA"),
        ("baseball_mlb", "MLB"),
        ("icehockey_nhl", "NHL"),
    ];

    public static string LoadApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable("ODDS_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey;

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                var json = JsonDocument.Parse(File.ReadAllText(path));
                if (json.RootElement.TryGetProperty("OddsApiKey", out var val))
                    return val.GetString() ?? "";
            }
        }
        catch { }

        return "";
    }
}
