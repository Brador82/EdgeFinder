using EdgeFinder.Core.Abstractions;
using EdgeFinder.Core.Pipeline;

namespace EdgeFinder.Console.Ui;

public class AskUi
{
    private readonly QueryPipeline _pipeline;

    public AskUi(QueryPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            System.Console.Clear();
            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
            System.Console.WriteLine("  ║              A S K // E D G E                   ║");
            System.Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
            System.Console.ResetColor();
            System.Console.WriteLine();
            System.Console.WriteLine("  Ask any question about a bet, wager, or probability.");
            System.Console.WriteLine("  Examples:");
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine("    \"Do the Lakers have edge tonight?\"");
            System.Console.WriteLine("    \"Chiefs or Bills this weekend?\"");
            System.Console.WriteLine("    \"Celtics vs Heat — who wins?\"");
            System.Console.ResetColor();
            System.Console.WriteLine();
            System.Console.WriteLine("  Type [B] to go back.");
            System.Console.WriteLine();
            System.Console.Write("  > ");
            var input = System.Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || input.Equals("B", StringComparison.OrdinalIgnoreCase))
                return;

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine("  Analyzing your question...");
            System.Console.ResetColor();

            try
            {
                var result = await _pipeline.RunAsync(input);

                System.Console.Clear();
                System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                System.Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
                System.Console.WriteLine("  ║              A S K // E D G E                   ║");
                System.Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
                System.Console.ResetColor();
                System.Console.WriteLine();

                // Question
                System.Console.ForegroundColor = ConsoleColor.White;
                System.Console.WriteLine($"  Q: {result.Intent.NormalizedQuestion}");
                System.Console.ResetColor();
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"  Domain: {result.Intent.Domain}  |  Entities: {string.Join(", ", result.Intent.Entities)}");
                System.Console.ResetColor();
                System.Console.WriteLine();

                // Probability gauge
                var prob = result.Result.Probability;
                var conf = result.Result.Confidence;
                System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                System.Console.WriteLine("  ── PROBABILITY ──────────────────────────────────");
                System.Console.ResetColor();

                var barLen = 40;
                var filled = (int)(prob * barLen);
                System.Console.Write("  ");
                System.Console.ForegroundColor = prob >= 0.6 ? ConsoleColor.Green
                                               : prob >= 0.4 ? ConsoleColor.Yellow
                                               : ConsoleColor.Red;
                System.Console.Write(new string('█', filled));
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.Write(new string('░', barLen - filled));
                System.Console.ResetColor();
                System.Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write($"  {prob * 100:F1}%");
                System.Console.ResetColor();
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.Write($"  (confidence: {conf * 100:F0}%)");
                System.Console.ResetColor();
                System.Console.WriteLine();

                // Edge
                if (result.Result.Edge.HasValue)
                {
                    var edge = result.Result.Edge.Value;
                    System.Console.Write("  Edge: ");
                    System.Console.ForegroundColor = edge >= 0.08 ? ConsoleColor.Green
                                                   : edge >= 0.03 ? ConsoleColor.Yellow
                                                   : ConsoleColor.Red;
                    System.Console.Write($"{(edge > 0 ? "+" : "")}{edge * 100:F1}%");
                    System.Console.ResetColor();
                    System.Console.WriteLine();
                }
                System.Console.WriteLine();

                // KPIs
                if (result.Result.KPIs.Count > 0)
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                    System.Console.WriteLine("  ── KPIs ─────────────────────────────────────────");
                    System.Console.ResetColor();

                    foreach (var kpi in result.Result.KPIs)
                    {
                        System.Console.Write("  ");
                        System.Console.ForegroundColor = ConsoleColor.Cyan;
                        System.Console.Write($"{kpi.Label,-24}");
                        System.Console.ResetColor();
                        System.Console.ForegroundColor = ConsoleColor.White;
                        System.Console.Write(kpi.Value);
                        System.Console.ResetColor();

                        if (kpi.Context != null)
                        {
                            System.Console.ForegroundColor = ConsoleColor.DarkGray;
                            System.Console.Write($"  ({kpi.Context})");
                            System.Console.ResetColor();
                        }
                        System.Console.WriteLine();
                    }
                    System.Console.WriteLine();
                }

                // Reasoning
                System.Console.ForegroundColor = ConsoleColor.DarkCyan;
                System.Console.WriteLine("  ── REASONING ────────────────────────────────────");
                System.Console.ResetColor();
                System.Console.ForegroundColor = ConsoleColor.DarkGray;

                // Word-wrap the reasoning at ~70 chars
                var words = result.Result.Reasoning.Split(' ');
                var line = "  ";
                foreach (var word in words)
                {
                    if (line.Length + word.Length > 72)
                    {
                        System.Console.WriteLine(line);
                        line = "  ";
                    }
                    line += word + " ";
                }
                if (line.Trim().Length > 0)
                    System.Console.WriteLine(line);

                System.Console.ResetColor();
                System.Console.WriteLine();

                // Data sources
                if (result.Result.DataSourcesUsed.Count > 0)
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkGray;
                    System.Console.WriteLine($"  Data: {string.Join(", ", result.Result.DataSourcesUsed)}");
                    System.Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"  Error: {ex.Message}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine();
            System.Console.Write("  Press Enter to ask another question...");
            System.Console.ReadLine();
        }
    }
}
