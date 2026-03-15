using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EdgeFinder.Core.Abstractions;
using EdgeFinder.DataSources.Persistence;
using EdgeFinder.DataSources.Sports;
using EdgeFinder.Console.Ui;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var apiKey = Environment.GetEnvironmentVariable("ODDS_API_KEY")
          ?? config["OddsApiKey"]
          ?? "";

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

var services = new ServiceCollection();

services.AddSingleton<IStateStore>(new JsonStateStore());
services.AddHttpClient<IOddsService, OddsService>((http) => new OddsService(http, apiKey));

var provider = services.BuildServiceProvider();

var store = provider.GetRequiredService<IStateStore>();
var state = store.Load();
var odds = provider.GetRequiredService<IOddsService>();

var ui = new ConsoleUi(state, odds, store);
await ui.RunAsync();
