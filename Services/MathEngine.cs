using EdgeFinder.Models;

namespace EdgeFinder.Services;

public static class MathEngine
{
    public static double AmToDec(int am) =>
        am > 0 ? am / 100.0 + 1 : 100.0 / Math.Abs(am) + 1;

    public static double AmToImpl(int am) =>
        am > 0 ? 100.0 / (am + 100) : Math.Abs(am) / (Math.Abs(am) + 100.0);

    public static double GetConsensusProb(OddsGame game, int outcomeIdx)
    {
        var probs = new List<double>();

        foreach (var bk in game.Bookmakers)
        {
            var market = bk.Markets.FirstOrDefault(m => m.Key == "h2h");
            if (market == null || market.Outcomes.Count < 2)
                continue;

            var implied = market.Outcomes.Select(o => AmToImpl(o.Price)).ToList();
            var sum = implied.Sum();
            if (sum > 0)
                probs.Add(implied[outcomeIdx] / sum);
        }

        return probs.Count > 0 ? probs.Average() : 0.5;
    }

    public static (int Price, string Book)? GetBestLine(OddsGame game, string outcomeName)
    {
        (int Price, string Book)? best = null;

        foreach (var bk in game.Bookmakers)
        {
            var market = bk.Markets.FirstOrDefault(m => m.Key == "h2h");
            if (market == null) continue;

            var outcome = market.Outcomes.FirstOrDefault(o => o.Name == outcomeName);
            if (outcome == null) continue;

            if (best == null || AmToDec(outcome.Price) > AmToDec(best.Value.Price))
                best = (outcome.Price, bk.Title);
        }

        return best;
    }

    public static double CalcKelly(double p, double dec)
    {
        var b = dec - 1;
        if (b <= 0 || p <= 0 || p >= 1) return 0;
        return Math.Max(0, (b * p - (1 - p)) / b);
    }

    public static double CalcEdge(double prob, double dec) => prob * dec - 1;

    public static string FmtAm(int am) => am > 0 ? $"+{am}" : $"{am}";
    public static string FmtPct(double n, int d = 1) => $"{(n * 100).ToString($"F{d}")}%";
    public static string FmtUsd(decimal n) => $"${Math.Abs(n):F2}";
}
