using EdgeFinder;
using EdgeFinder.Services;
using EdgeFinder.Ui;

var apiKey = Config.LoadApiKey();
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine();
    Console.WriteLine("  No API key found.");
    Console.WriteLine("  Set the ODDS_API_KEY environment variable");
    Console.WriteLine("  or add your key to appsettings.json");
    Console.WriteLine();
    Console.WriteLine("  Get a free key at: https://the-odds-api.com");
    return;
}

var state = StateStore.Load();
var odds = new OddsService(apiKey);
var ui = new ConsoleUi(state, odds);

await ui.RunAsync();
