using System.Text.Json.Serialization;

namespace EdgeFinder.Models;

public class OddsGame
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("home_team")]
    public string HomeTeam { get; set; } = "";

    [JsonPropertyName("away_team")]
    public string AwayTeam { get; set; } = "";

    [JsonPropertyName("commence_time")]
    public DateTime CommenceTime { get; set; }

    [JsonPropertyName("bookmakers")]
    public List<Bookmaker> Bookmakers { get; set; } = [];
}

public class Bookmaker
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("markets")]
    public List<Market> Markets { get; set; } = [];
}

public class Market
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("outcomes")]
    public List<Outcome> Outcomes { get; set; } = [];
}

public class Outcome
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("price")]
    public int Price { get; set; }
}
