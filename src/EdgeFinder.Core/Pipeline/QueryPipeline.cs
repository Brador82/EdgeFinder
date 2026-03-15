using EdgeFinder.Core.Abstractions;

namespace EdgeFinder.Core.Pipeline;

public class QueryPipeline
{
    private readonly IQueryAnalyzer _analyzer;
    private readonly IDataSourceRegistry _registry;
    private readonly IProbabilityEngine _engine;

    public QueryPipeline(IQueryAnalyzer analyzer, IDataSourceRegistry registry, IProbabilityEngine engine)
    {
        _analyzer = analyzer;
        _registry = registry;
        _engine = engine;
    }

    public async Task<PipelineResult> RunAsync(string question, byte[]? imageData = null, CancellationToken ct = default)
    {
        // Step 1: Analyze the question
        var intent = await _analyzer.AnalyzeAsync(question, imageData, ct);

        // Step 2: Build data requirements from the intent
        var requirements = intent.RequiredDataTypes.Select(dt => new DataRequirement(
            DataType: dt,
            Domain: intent.Domain,
            Parameters: BuildParameters(intent),
            From: null,
            To: null
        )).ToList();

        // Step 3: Fetch data from all matching sources in parallel
        var fetchTasks = new List<Task<DataPayload>>();
        var sourcesUsed = new List<string>();

        foreach (var req in requirements)
        {
            var sources = _registry.GetSourcesForRequirement(req);
            foreach (var source in sources)
            {
                sourcesUsed.Add(source.Name);
                fetchTasks.Add(source.FetchAsync(req, ct));
            }
        }

        var payloads = fetchTasks.Count > 0
            ? (await Task.WhenAll(fetchTasks)).ToList()
            : new List<DataPayload>();

        // Step 4: Calculate probabilities
        var result = await _engine.CalculateAsync(intent, payloads, ct);

        return new PipelineResult(intent, payloads, result);
    }

    private static Dictionary<string, string> BuildParameters(QueryIntent intent)
    {
        var parameters = new Dictionary<string, string>();

        foreach (var entity in intent.Entities)
        {
            // First entity is typically the primary team/subject
            if (!parameters.ContainsKey("team"))
                parameters["team"] = entity;
            else if (!parameters.ContainsKey("opponent"))
                parameters["opponent"] = entity;
        }

        // Try to infer sport from entities
        if (!parameters.ContainsKey("sport") && intent.Domain == "sports")
        {
            var sport = InferSport(intent.Entities);
            if (sport != null)
                parameters["sport"] = sport;
        }

        if (intent.TimeFrame != null)
            parameters["timeframe"] = intent.TimeFrame;

        return parameters;
    }

    private static string? InferSport(string[] entities)
    {
        var nbaTeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Lakers", "Celtics", "Warriors", "Nets", "Knicks", "76ers", "Sixers",
            "Bucks", "Heat", "Bulls", "Suns", "Mavericks", "Mavs", "Nuggets",
            "Clippers", "Raptors", "Hawks", "Cavaliers", "Cavs", "Pacers",
            "Trail Blazers", "Blazers", "Thunder", "Pelicans", "Grizzlies",
            "Timberwolves", "Wolves", "Spurs", "Kings", "Pistons", "Magic",
            "Hornets", "Wizards", "Jazz", "Rockets"
        };

        var nflTeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Chiefs", "Eagles", "Bills", "49ers", "Cowboys", "Dolphins", "Lions",
            "Ravens", "Bengals", "Jaguars", "Chargers", "Jets", "Vikings",
            "Packers", "Seahawks", "Steelers", "Browns", "Commanders", "Saints",
            "Falcons", "Bears", "Giants", "Raiders", "Broncos", "Titans",
            "Colts", "Panthers", "Texans", "Cardinals", "Rams", "Buccaneers", "Bucs"
        };

        var mlbTeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Yankees", "Dodgers", "Astros", "Braves", "Mets", "Phillies",
            "Padres", "Mariners", "Blue Jays", "Guardians", "Orioles",
            "Cardinals", "Rays", "Twins", "Rangers", "Red Sox", "Cubs",
            "White Sox", "Brewers", "Reds", "Diamondbacks", "D-backs",
            "Giants", "Angels", "Pirates", "Rockies", "Tigers", "Royals",
            "Athletics", "Marlins", "Nationals"
        };

        foreach (var entity in entities)
        {
            var words = entity.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (nbaTeams.Contains(word)) return "nba";
                if (nflTeams.Contains(word)) return "nfl";
                if (mlbTeams.Contains(word)) return "mlb";
            }
        }

        return null;
    }
}

public record PipelineResult(
    QueryIntent Intent,
    List<DataPayload> Data,
    ProbabilityResult Result);
