namespace EdgeFinder.Core;

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
}
