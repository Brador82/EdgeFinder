using System.Net.Http.Json;
using EdgeFinder.Core.Abstractions;
using EdgeFinder.Core.Models;

namespace EdgeFinder.DataSources.Sports;

public class OddsService : IOddsService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public string? RequestsRemaining { get; private set; }

    public OddsService(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public async Task<List<OddsGame>> FetchOddsAsync(string sportKey)
    {
        try
        {
            var url = $"https://api.the-odds-api.com/v4/sports/{sportKey}/odds/"
                    + $"?apiKey={_apiKey}&regions=us&markets=h2h&oddsFormat=american";

            var response = await _http.GetAsync(url);

            if (response.Headers.TryGetValues("x-requests-remaining", out var vals))
                RequestsRemaining = vals.FirstOrDefault();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"  API error: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                return [];
            }

            var games = await response.Content.ReadFromJsonAsync<List<OddsGame>>();
            return games ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Fetch error: {ex.Message}");
            return [];
        }
    }
}
