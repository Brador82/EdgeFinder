using Microsoft.Extensions.Configuration;
using EdgeFinder.Core.Abstractions;
using EdgeFinder.Core.Pipeline;
using EdgeFinder.Claude;
using EdgeFinder.DataSources.Persistence;
using EdgeFinder.DataSources.Sports;
using EdgeFinder.DataSources.Registry;
using EdgeFinder.Console.Ui;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var oddsApiKey = Environment.GetEnvironmentVariable("ODDS_API_KEY")
              ?? config["OddsApiKey"]
              ?? "";

if (string.IsNullOrWhiteSpace(oddsApiKey))
{
    Console.WriteLine();
    Console.WriteLine("  No Odds API key found.");
    Console.WriteLine("  Set the ODDS_API_KEY environment variable");
    Console.WriteLine("  or add your key to appsettings.json");
    Console.WriteLine();
    Console.WriteLine("  Get a free key at: https://the-odds-api.com");
    return;
}

var anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                   ?? config["AnthropicApiKey"]
                   ?? "";

// Services
var store = new JsonStateStore() as IStateStore;
var state = store.Load();
var oddsHttp = new HttpClient();
var oddsService = new OddsService(oddsHttp, oddsApiKey);
IOddsService odds = oddsService;

// Wire up Ask mode if Anthropic key is available
AskUi? askUi = null;

if (!string.IsNullOrWhiteSpace(anthropicApiKey))
{
    var claudeHttp = new HttpClient();
    var claudeClient = new ClaudeClient(claudeHttp, anthropicApiKey);
    var analyzer = new ClaudeQueryAnalyzer(claudeClient);
    var engine = new ClaudeProbabilityEngine(claudeClient);

    var registry = new DataSourceRegistry();
    registry.Register(new OddsApiSource(oddsService));

    var pipeline = new QueryPipeline(analyzer, registry, engine);
    askUi = new AskUi(pipeline);
}
else
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("  Note: No Anthropic API key found. Ask mode disabled.");
    Console.WriteLine("  Add AnthropicApiKey to appsettings.json to enable it.");
    Console.ResetColor();
    Console.WriteLine();
    Thread.Sleep(2000);
}

var ui = new ConsoleUi(state, odds, store, askUi);
await ui.RunAsync();
