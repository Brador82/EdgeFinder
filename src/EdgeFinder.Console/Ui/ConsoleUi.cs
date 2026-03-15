using EdgeFinder.Core;
using EdgeFinder.Core.Abstractions;
using EdgeFinder.Core.Math;
using EdgeFinder.Core.Models;
using EdgeFinder.Core.Pipeline;

namespace EdgeFinder.Console.Ui;

public class ConsoleUi
{
    private readonly AppState _state;
    private readonly IOddsService _odds;
    private readonly IStateStore _store;
    private List<OddsGame> _games = [];
    private string _sportKey = "basketball_nba";

    public ConsoleUi(AppState state, IOddsService odds, IStateStore store)
    {
        _state = state;
        _odds = odds;
        _store = store;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            System.Console.Clear();
            PrintHeader();
            System.Console.WriteLine();
            System.Console.WriteLine("  [1] Scanner    [2] Queue ({0})    [3] Ledger    [4] Quit",
                _state.Queue.Count);
            System.Console.WriteLine();
            System.Console.Write("  > ");
            var input = System.Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1": await ScannerMenu(); break;
                case "2": QueueMenu(); break;
                case "3": LedgerMenu(); break;
                case "4": return;
            }
        }
    }

    // ─── Header ──────────────────────────────────────────────────────────────

    private void PrintHeader()
    {
        var pnl = CalcPnl();
        var pnlSign = pnl >= 0 ? "+" : "-";
        var queued = _state.Queue.Sum(b => b.Size);

        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
        System.Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
        System.Console.WriteLine("  ║              E D G E // F I N D E R             ║");
        System.Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
        System.Console.ResetColor();
        System.Console.WriteLine();
        System.Console.Write("  Bankroll: ");
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.Write(MathEngine.FmtUsd(_state.Bankroll));
        System.Console.ResetColor();
        System.Console.Write("    In Queue: ");
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.Write(MathEngine.FmtUsd(queued));
        System.Console.ResetColor();
        System.Console.Write("    P&L: ");
        System.Console.ForegroundColor = pnl >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        System.Console.Write($"{pnlSign}{MathEngine.FmtUsd(Math.Abs(pnl))}");
        System.Console.ResetColor();

        if (_odds.RequestsRemaining != null)
        {
            System.Console.Write($"    API calls left: {_odds.RequestsRemaining}");
        }
        System.Console.WriteLine();
    }

    // ─── Scanner ─────────────────────────────────────────────────────────────

    private async Task ScannerMenu()
    {
        while (true)
        {
            System.Console.Clear();
            PrintHeader();
            System.Console.WriteLine();

            System.Console.Write("  Sport: ");
            for (int i = 0; i < Config.Sports.Length; i++)
            {
                var s = Config.Sports[i];
                if (s.Key == _sportKey)
                {
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.Write($"[{i + 1}]{s.Label}");
                    System.Console.ResetColor();
                }
                else
                {
                    System.Console.Write($"[{i + 1}]{s.Label}");
                }
                System.Console.Write("  ");
            }
            System.Console.WriteLine("    [R]efresh  [B]ack");
            System.Console.WriteLine();

            if (_games.Count == 0)
            {
                System.Console.Write("  Fetching odds...");
                _games = await _odds.FetchOddsAsync(_sportKey);
                System.Console.WriteLine(" done.");
                System.Console.WriteLine();
            }

            if (_games.Count == 0)
            {
                System.Console.WriteLine("  No games found for this sport right now.");
                System.Console.WriteLine();
                System.Console.Write("  > ");
                var inp = System.Console.ReadLine()?.Trim().ToUpperInvariant();
                if (inp == "B") return;
                if (inp is "1" or "2" or "3" or "4")
                {
                    var idx = int.Parse(inp) - 1;
                    if (idx >= 0 && idx < Config.Sports.Length)
                    {
                        _sportKey = Config.Sports[idx].Key;
                        _games = [];
                    }
                }
                else if (inp == "R") _games = [];
                continue;
            }

            var edges = EdgeScanner.FindEdges(_games, _state.Bankroll);

            for (int gi = 0; gi < _games.Count; gi++)
            {
                var g = _games[gi];
                var h2h = g.Bookmakers.FirstOrDefault()?.Markets.FirstOrDefault(m => m.Key == "h2h");
                if (h2h == null || h2h.Outcomes.Count < 2) continue;

                var time = g.CommenceTime.ToLocalTime().ToString("MMM d, h:mm tt");
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.Write($"  {gi + 1,2}. ");
                System.Console.ResetColor();
                System.Console.Write($"{time} · {g.Bookmakers.Count} book(s)");
                System.Console.WriteLine();

                for (int oi = 0; oi < 2; oi++)
                {
                    var name = h2h.Outcomes[oi].Name;
                    var opp = edges.FirstOrDefault(e => e.Game.Id == g.Id && e.OutcomeIdx == oi);
                    if (opp == null) continue;

                    var edgeStr = MathEngine.FmtPct(opp.Edge);
                    var edgeColor = opp.Edge >= 0.08 ? ConsoleColor.Green
                                  : opp.Edge >= Config.MinEdge ? ConsoleColor.Yellow
                                  : ConsoleColor.DarkGray;

                    System.Console.Write($"       {(oi == 0 ? "A" : "B")}) {name,-24} ");
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.Write($"{MathEngine.FmtAm(opp.BestPrice),6}");
                    System.Console.ResetColor();
                    System.Console.Write($"  prob:{MathEngine.FmtPct(opp.ConsensusProb),6}  edge:");
                    System.Console.ForegroundColor = edgeColor;
                    System.Console.Write($"{(opp.Edge > 0 ? "+" : "")}{edgeStr,7}");
                    System.Console.ResetColor();
                    System.Console.WriteLine();
                }
                System.Console.WriteLine();
            }

            System.Console.WriteLine("  Enter game # to analyze, [1-4] change sport, [R]efresh, [B]ack");
            System.Console.Write("  > ");
            var input = System.Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "B") return;
            if (input == "R") { _games = []; continue; }
            if (input is "1" or "2" or "3" or "4")
            {
                var si = int.Parse(input) - 1;
                if (si >= 0 && si < Config.Sports.Length && Config.Sports[si].Key != _sportKey)
                {
                    _sportKey = Config.Sports[si].Key;
                    _games = [];
                    continue;
                }
            }

            if (int.TryParse(input, out var gameNum) && gameNum >= 1 && gameNum <= _games.Count)
            {
                await AnalyzeGame(_games[gameNum - 1], edges);
            }
        }
    }

    private async Task AnalyzeGame(OddsGame game, List<EdgeOpportunity> edges)
    {
        var h2h = game.Bookmakers.FirstOrDefault()?.Markets.FirstOrDefault(m => m.Key == "h2h");
        if (h2h == null || h2h.Outcomes.Count < 2) return;

        System.Console.Clear();
        PrintHeader();
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
        System.Console.WriteLine("  ── BET ANALYSIS ─────────────────────────────────");
        System.Console.ResetColor();
        System.Console.WriteLine($"  {game.HomeTeam} vs {game.AwayTeam}");
        System.Console.WriteLine();

        System.Console.WriteLine("  Pick a side:");
        for (int i = 0; i < 2; i++)
        {
            var opp = edges.FirstOrDefault(e => e.Game.Id == game.Id && e.OutcomeIdx == i);
            if (opp == null) continue;
            System.Console.WriteLine($"    [{i + 1}] {opp.OutcomeName}  {MathEngine.FmtAm(opp.BestPrice)}  (prob: {MathEngine.FmtPct(opp.ConsensusProb)}, edge: {(opp.Edge > 0 ? "+" : "")}{MathEngine.FmtPct(opp.Edge)})");
        }
        System.Console.WriteLine("    [B] Back");
        System.Console.Write("  > ");
        var pick = System.Console.ReadLine()?.Trim().ToUpperInvariant();

        if (pick is not ("1" or "2")) return;
        var idx = int.Parse(pick) - 1;

        var selected = edges.FirstOrDefault(e => e.Game.Id == game.Id && e.OutcomeIdx == idx);
        if (selected == null) return;

        System.Console.WriteLine();
        System.Console.WriteLine($"  Market consensus probability: {MathEngine.FmtPct(selected.ConsensusProb)}");
        System.Console.Write($"  Enter YOUR probability estimate (1-99, or Enter for market): ");
        var probInput = System.Console.ReadLine()?.Trim();

        double userProb = selected.ConsensusProb;
        if (int.TryParse(probInput, out var pctVal) && pctVal >= 1 && pctVal <= 99)
        {
            userProb = pctVal / 100.0;
        }

        var dec = MathEngine.AmToDec(selected.BestPrice);
        var edge = MathEngine.CalcEdge(userProb, dec);
        var fullK = MathEngine.CalcKelly(userProb, dec);
        var halfK = fullK * Config.KellyFraction;
        var size = Math.Round(Math.Min((decimal)halfK * _state.Bankroll, _state.Bankroll), 2);
        var payout = Math.Round(size * (decimal)dec, 2);
        var hasEdge = edge >= Config.MinEdge && size > 0;

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
        System.Console.WriteLine("  ── RESULTS ──────────────────────────────────────");
        System.Console.ResetColor();
        System.Console.WriteLine($"  Pick:        {selected.OutcomeName}");
        System.Console.WriteLine($"  Best line:   {MathEngine.FmtAm(selected.BestPrice)} @ {selected.BestBook}");
        System.Console.WriteLine($"  Market prob: {MathEngine.FmtPct(selected.ConsensusProb)}");
        System.Console.WriteLine($"  Your prob:   {MathEngine.FmtPct(userProb)}");

        System.Console.Write($"  Edge:        ");
        System.Console.ForegroundColor = edge >= 0.08 ? ConsoleColor.Green
                                : edge >= Config.MinEdge ? ConsoleColor.Yellow
                                : ConsoleColor.Red;
        System.Console.WriteLine($"{(edge > 0 ? "+" : "")}{MathEngine.FmtPct(edge)}");
        System.Console.ResetColor();

        if (hasEdge)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"  Full Kelly:  {MathEngine.FmtPct(fullK)}  ->  1/2 Kelly: {MathEngine.FmtPct(halfK)}");
            System.Console.WriteLine($"  Bet size:    {MathEngine.FmtUsd(size)} of {MathEngine.FmtUsd(_state.Bankroll)} bankroll");
            System.Console.WriteLine($"  To win:      +{MathEngine.FmtUsd(payout - size)}  (payout: {MathEngine.FmtUsd(payout)})");
            System.Console.ResetColor();
            System.Console.WriteLine();
            System.Console.Write("  [A]dd to queue  or  [B]ack? ");
            var action = System.Console.ReadLine()?.Trim().ToUpperInvariant();

            if (action == "A")
            {
                var bet = new Bet
                {
                    Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Game = $"{game.HomeTeam} vs {game.AwayTeam}",
                    Pick = selected.OutcomeName,
                    Odds = selected.BestPrice,
                    Book = selected.BestBook,
                    MarketProb = selected.ConsensusProb,
                    UserProb = userProb,
                    Edge = edge,
                    Kelly = halfK,
                    Size = size,
                    Payout = payout,
                    Result = "pending",
                };
                _state.Queue.Add(bet);
                _store.Save(_state);
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine($"\n  Added to queue: {bet.Pick} {MathEngine.FmtAm(bet.Odds)} · {MathEngine.FmtUsd(bet.Size)}");
                System.Console.ResetColor();
                System.Console.Write("  Press Enter...");
                System.Console.ReadLine();
            }
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"\n  Edge below {Config.MinEdge * 100:F0}% threshold. Pass on this one.");
            System.Console.ResetColor();
            System.Console.Write("  Press Enter...");
            System.Console.ReadLine();
        }
    }

    // ─── Queue ───────────────────────────────────────────────────────────────

    private void QueueMenu()
    {
        while (true)
        {
            System.Console.Clear();
            PrintHeader();
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.WriteLine("  ── QUEUE ────────────────────────────────────────");
            System.Console.ResetColor();

            if (_state.Queue.Count == 0)
            {
                System.Console.WriteLine("\n  No bets queued. Find edges in the Scanner.");
                System.Console.Write("\n  Press Enter...");
                System.Console.ReadLine();
                return;
            }

            var total = _state.Queue.Sum(b => b.Size);
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"  {_state.Queue.Count} pending approval · Total exposure: {MathEngine.FmtUsd(total)}");
            System.Console.ResetColor();
            System.Console.WriteLine();

            for (int i = 0; i < _state.Queue.Count; i++)
            {
                var b = _state.Queue[i];
                System.Console.Write($"  {i + 1,2}. ");
                System.Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write($"{b.Pick,-22}");
                System.Console.ResetColor();
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.Write($" {MathEngine.FmtAm(b.Odds),6}");
                System.Console.ResetColor();
                System.Console.Write($" @ {b.Book}");
                System.Console.WriteLine();
                System.Console.Write("      ");
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.Write($"{b.Game}");
                System.Console.ResetColor();
                System.Console.WriteLine();

                System.Console.Write("      edge:");
                var edgeColor = b.Edge >= 0.08 ? ConsoleColor.Green
                              : b.Edge >= Config.MinEdge ? ConsoleColor.Yellow
                              : ConsoleColor.DarkGray;
                System.Console.ForegroundColor = edgeColor;
                System.Console.Write($"{(b.Edge > 0 ? "+" : "")}{MathEngine.FmtPct(b.Edge),7}");
                System.Console.ResetColor();
                System.Console.Write($"  kelly:{MathEngine.FmtPct(b.Kelly),6}");
                System.Console.Write("  risk:");
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.Write($"{MathEngine.FmtUsd(b.Size)}");
                System.Console.ResetColor();
                System.Console.Write("  to-win:");
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write($"+{MathEngine.FmtUsd(b.Payout - b.Size)}");
                System.Console.ResetColor();
                System.Console.WriteLine();
                System.Console.WriteLine();
            }

            System.Console.WriteLine("  Enter # to [A]pprove/[K]ill, or [B]ack");
            System.Console.Write("  > ");
            var input = System.Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "B") return;

            if (int.TryParse(input, out var num) && num >= 1 && num <= _state.Queue.Count)
            {
                var bet = _state.Queue[num - 1];
                System.Console.Write($"  {bet.Pick} · {MathEngine.FmtUsd(bet.Size)} — [A]pprove or [K]ill? ");
                var action = System.Console.ReadLine()?.Trim().ToUpperInvariant();

                if (action == "A")
                {
                    _state.Bankroll = Math.Round(_state.Bankroll - bet.Size, 2);
                    _state.Queue.RemoveAt(num - 1);
                    bet.Result = "pending";
                    _state.Ledger.Add(bet);
                    _store.Save(_state);
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"  Approved. {MathEngine.FmtUsd(bet.Size)} deducted from bankroll.");
                    System.Console.ResetColor();
                    System.Console.Write("  Press Enter...");
                    System.Console.ReadLine();
                }
                else if (action == "K")
                {
                    _state.Queue.RemoveAt(num - 1);
                    _store.Save(_state);
                    System.Console.WriteLine("  Bet removed from queue.");
                    System.Console.Write("  Press Enter...");
                    System.Console.ReadLine();
                }
            }
        }
    }

    // ─── Ledger ──────────────────────────────────────────────────────────────

    private void LedgerMenu()
    {
        while (true)
        {
            System.Console.Clear();
            PrintHeader();
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.WriteLine("  ── LEDGER ───────────────────────────────────────");
            System.Console.ResetColor();

            if (_state.Ledger.Count == 0)
            {
                System.Console.WriteLine("\n  No bets in ledger. Approve bets from the Queue to begin tracking.");
                System.Console.Write("\n  Press Enter...");
                System.Console.ReadLine();
                return;
            }

            var settled = _state.Ledger.Where(b => b.Result != "pending").ToList();
            var wins = settled.Count(b => b.Result == "win");
            var losses = settled.Count(b => b.Result == "loss");
            var pnl = CalcPnl();
            var risked = settled.Sum(b => b.Size);
            var roi = risked > 0 ? pnl / risked : 0;

            System.Console.WriteLine();
            System.Console.Write("  Bankroll: ");
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write(MathEngine.FmtUsd(_state.Bankroll));
            System.Console.ResetColor();

            System.Console.Write("   P&L: ");
            System.Console.ForegroundColor = pnl >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            System.Console.Write($"{(pnl >= 0 ? "+" : "-")}{MathEngine.FmtUsd(Math.Abs(pnl))}");
            System.Console.ResetColor();

            System.Console.Write($"   Record: {wins}W-{losses}L");

            System.Console.Write("   ROI: ");
            System.Console.ForegroundColor = roi >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            System.Console.Write($"{(roi >= 0 ? "+" : "-")}{MathEngine.FmtPct((double)Math.Abs(roi))}");
            System.Console.ResetColor();
            System.Console.WriteLine();
            System.Console.WriteLine();

            var reversed = _state.Ledger.AsEnumerable().Reverse().ToList();
            int pending = 0;

            for (int i = 0; i < reversed.Count; i++)
            {
                var b = reversed[i];

                System.Console.Write($"  {i + 1,2}. ");

                if (b.Result == "win")
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.Write("[WIN]  ");
                }
                else if (b.Result == "loss")
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.Write("[LOSS] ");
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.Write("[PEND] ");
                    pending++;
                }
                System.Console.ResetColor();

                System.Console.Write($"{b.Pick,-22} {MathEngine.FmtAm(b.Odds),6}  ");
                System.Console.Write($"risked:{MathEngine.FmtUsd(b.Size)}  edge:{(b.Edge > 0 ? "+" : "")}{MathEngine.FmtPct(b.Edge)}");

                if (b.Result == "win")
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.Write($"  profit:+{MathEngine.FmtUsd(b.Payout - b.Size)}");
                    System.Console.ResetColor();
                }

                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"      {b.Game} · {b.Book}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine();
            if (pending > 0)
                System.Console.WriteLine("  Enter # to settle a pending bet, or [B]ack");
            else
                System.Console.WriteLine("  [B]ack");
            System.Console.Write("  > ");
            var input = System.Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "B") return;

            if (int.TryParse(input, out var num) && num >= 1 && num <= reversed.Count)
            {
                var bet = reversed[num - 1];
                if (bet.Result != "pending")
                {
                    System.Console.WriteLine("  That bet is already settled.");
                    System.Console.Write("  Press Enter...");
                    System.Console.ReadLine();
                    continue;
                }

                System.Console.Write($"  {bet.Pick} · {MathEngine.FmtUsd(bet.Size)} — [W]in or [L]oss? ");
                var result = System.Console.ReadLine()?.Trim().ToUpperInvariant();

                if (result == "W")
                {
                    bet.Result = "win";
                    bet.Payout = Math.Round(bet.Size * (decimal)MathEngine.AmToDec(bet.Odds), 2);
                    _state.Bankroll = Math.Round(_state.Bankroll + bet.Payout, 2);
                    _store.Save(_state);
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"  WIN! +{MathEngine.FmtUsd(bet.Payout)} added to bankroll.");
                    System.Console.ResetColor();
                    System.Console.Write("  Press Enter...");
                    System.Console.ReadLine();
                }
                else if (result == "L")
                {
                    bet.Result = "loss";
                    bet.Payout = 0;
                    _store.Save(_state);
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"  Loss. {MathEngine.FmtUsd(bet.Size)} lost.");
                    System.Console.ResetColor();
                    System.Console.Write("  Press Enter...");
                    System.Console.ReadLine();
                }
            }
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private decimal CalcPnl()
    {
        return _state.Ledger
            .Where(b => b.Result != "pending")
            .Sum(b => b.Result == "win" ? b.Payout - b.Size : -b.Size);
    }
}
