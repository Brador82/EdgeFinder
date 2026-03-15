using EdgeFinder.Models;

namespace EdgeFinder.Services;

public record EdgeOpportunity(
    OddsGame Game,
    int OutcomeIdx,
    string OutcomeName,
    double ConsensusProb,
    int BestPrice,
    string BestBook,
    double Edge,
    double FullKelly,
    double HalfKelly,
    decimal BetSize,
    decimal Payout);

public static class EdgeScanner
{
    public static List<EdgeOpportunity> FindEdges(List<OddsGame> games, decimal bankroll)
    {
        var opportunities = new List<EdgeOpportunity>();

        foreach (var game in games)
        {
            var h2h = game.Bookmakers.FirstOrDefault()?.Markets.FirstOrDefault(m => m.Key == "h2h");
            if (h2h == null || h2h.Outcomes.Count < 2)
                continue;

            for (int i = 0; i < 2; i++)
            {
                var name = h2h.Outcomes[i].Name;
                var prob = MathEngine.GetConsensusProb(game, i);
                var best = MathEngine.GetBestLine(game, name);
                if (best == null) continue;

                var dec = MathEngine.AmToDec(best.Value.Price);
                var edge = MathEngine.CalcEdge(prob, dec);
                var fullK = MathEngine.CalcKelly(prob, dec);
                var halfK = fullK * Config.KellyFraction;
                var size = Math.Round(Math.Min((decimal)halfK * bankroll, bankroll), 2);
                var payout = Math.Round(size * (decimal)dec, 2);

                opportunities.Add(new EdgeOpportunity(
                    game, i, name, prob, best.Value.Price, best.Value.Book,
                    edge, fullK, halfK, size, payout));
            }
        }

        return opportunities;
    }
}
