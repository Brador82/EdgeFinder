using EdgeFinder.Models;
using EdgeFinder.Services;

namespace EdgeFinder.Ui;

public class ConsoleUi
{
    private readonly AppState _state;
    private readonly OddsService _odds;
    private List<OddsGame> _games = [];
    private string _sportKey = "basketball_nba";

    public ConsoleUi(AppState state, OddsService odds)
    {
        _state = state;
        _odds = odds;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine();
            Console.WriteLine("  [1] Scanner    [2] Queue ({0})    [3] Ledger    [4] Quit",
                _state.Queue.Count);
            Console.WriteLine();
            Console.Write("  > ");
            var input = Console.ReadLine()?.Trim();

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

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
        Console.WriteLine("  ║              E D G E // F I N D E R             ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.Write("  Bankroll: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(MathEngine.FmtUsd(_state.Bankroll));
        Console.ResetColor();
        Console.Write("    In Queue: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(MathEngine.FmtUsd(queued));
        Console.ResetColor();
        Console.Write("    P&L: ");
        Console.ForegroundColor = pnl >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($"{pnlSign}{MathEngine.FmtUsd(Math.Abs(pnl))}");
        Console.ResetColor();

        if (_odds.RequestsRemaining != null)
        {
            Console.Write($"    API calls left: {_odds.RequestsRemaining}");
        }
        Console.WriteLine();
    }

    // ─── Scanner ─────────────────────────────────────────────────────────────

    private async Task ScannerMenu()
    {
        while (true)
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine();

            // Sport selection
            Console.Write("  Sport: ");
            for (int i = 0; i < Config.Sports.Length; i++)
            {
                var s = Config.Sports[i];
                if (s.Key == _sportKey)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{i + 1}]{s.Label}");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write($"[{i + 1}]{s.Label}");
                }
                Console.Write("  ");
            }
            Console.WriteLine("    [R]efresh  [B]ack");
            Console.WriteLine();

            // Fetch if no games loaded
            if (_games.Count == 0)
            {
                Console.Write("  Fetching odds...");
                _games = await _odds.FetchOddsAsync(_sportKey);
                Console.WriteLine(" done.");
                Console.WriteLine();
            }

            if (_games.Count == 0)
            {
                Console.WriteLine("  No games found for this sport right now.");
                Console.WriteLine();
                Console.Write("  > ");
                var inp = Console.ReadLine()?.Trim().ToUpperInvariant();
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

            // List games
            var edges = EdgeScanner.FindEdges(_games, _state.Bankroll);
            var gameIds = _games.Select(g => g.Id).Distinct().ToList();

            for (int gi = 0; gi < _games.Count; gi++)
            {
                var g = _games[gi];
                var h2h = g.Bookmakers.FirstOrDefault()?.Markets.FirstOrDefault(m => m.Key == "h2h");
                if (h2h == null || h2h.Outcomes.Count < 2) continue;

                var time = g.CommenceTime.ToLocalTime().ToString("MMM d, h:mm tt");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  {gi + 1,2}. ");
                Console.ResetColor();
                Console.Write($"{time} · {g.Bookmakers.Count} book(s)");
                Console.WriteLine();

                for (int oi = 0; oi < 2; oi++)
                {
                    var name = h2h.Outcomes[oi].Name;
                    var opp = edges.FirstOrDefault(e => e.Game.Id == g.Id && e.OutcomeIdx == oi);
                    if (opp == null) continue;

                    var edgeStr = MathEngine.FmtPct(opp.Edge);
                    var edgeColor = opp.Edge >= 0.08 ? ConsoleColor.Green
                                  : opp.Edge >= Config.MinEdge ? ConsoleColor.Yellow
                                  : ConsoleColor.DarkGray;

                    Console.Write($"       {(oi == 0 ? "A" : "B")}) {name,-24} ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"{MathEngine.FmtAm(opp.BestPrice),6}");
                    Console.ResetColor();
                    Console.Write($"  prob:{MathEngine.FmtPct(opp.ConsensusProb),6}  edge:");
                    Console.ForegroundColor = edgeColor;
                    Console.Write($"{(opp.Edge > 0 ? "+" : "")}{edgeStr,7}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                Console.WriteLine();
            }

            Console.WriteLine("  Enter game # to analyze, [1-4] change sport, [R]efresh, [B]ack");
            Console.Write("  > ");
            var input = Console.ReadLine()?.Trim().ToUpperInvariant();

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

            // Game selection
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

        Console.Clear();
        PrintHeader();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  ── BET ANALYSIS ─────────────────────────────────");
        Console.ResetColor();
        Console.WriteLine($"  {game.HomeTeam} vs {game.AwayTeam}");
        Console.WriteLine();

        Console.WriteLine("  Pick a side:");
        for (int i = 0; i < 2; i++)
        {
            var opp = edges.FirstOrDefault(e => e.Game.Id == game.Id && e.OutcomeIdx == i);
            if (opp == null) continue;
            Console.WriteLine($"    [{i + 1}] {opp.OutcomeName}  {MathEngine.FmtAm(opp.BestPrice)}  (prob: {MathEngine.FmtPct(opp.ConsensusProb)}, edge: {(opp.Edge > 0 ? "+" : "")}{MathEngine.FmtPct(opp.Edge)})");
        }
        Console.WriteLine("    [B] Back");
        Console.Write("  > ");
        var pick = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (pick is not ("1" or "2")) return;
        var idx = int.Parse(pick) - 1;

        var selected = edges.FirstOrDefault(e => e.Game.Id == game.Id && e.OutcomeIdx == idx);
        if (selected == null) return;

        // Allow custom probability
        Console.WriteLine();
        Console.WriteLine($"  Market consensus probability: {MathEngine.FmtPct(selected.ConsensusProb)}");
        Console.Write($"  Enter YOUR probability estimate (1-99, or Enter for market): ");
        var probInput = Console.ReadLine()?.Trim();

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

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  ── RESULTS ──────────────────────────────────────");
        Console.ResetColor();
        Console.WriteLine($"  Pick:        {selected.OutcomeName}");
        Console.WriteLine($"  Best line:   {MathEngine.FmtAm(selected.BestPrice)} @ {selected.BestBook}");
        Console.WriteLine($"  Market prob: {MathEngine.FmtPct(selected.ConsensusProb)}");
        Console.WriteLine($"  Your prob:   {MathEngine.FmtPct(userProb)}");

        Console.Write($"  Edge:        ");
        Console.ForegroundColor = edge >= 0.08 ? ConsoleColor.Green
                                : edge >= Config.MinEdge ? ConsoleColor.Yellow
                                : ConsoleColor.Red;
        Console.WriteLine($"{(edge > 0 ? "+" : "")}{MathEngine.FmtPct(edge)}");
        Console.ResetColor();

        if (hasEdge)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Full Kelly:  {MathEngine.FmtPct(fullK)}  ->  1/2 Kelly: {MathEngine.FmtPct(halfK)}");
            Console.WriteLine($"  Bet size:    {MathEngine.FmtUsd(size)} of {MathEngine.FmtUsd(_state.Bankroll)} bankroll");
            Console.WriteLine($"  To win:      +{MathEngine.FmtUsd(payout - size)}  (payout: {MathEngine.FmtUsd(payout)})");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("  [A]dd to queue  or  [B]ack? ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();

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
                StateStore.Save(_state);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n  Added to queue: {bet.Pick} {MathEngine.FmtAm(bet.Odds)} · {MathEngine.FmtUsd(bet.Size)}");
                Console.ResetColor();
                Console.Write("  Press Enter...");
                Console.ReadLine();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n  Edge below {Config.MinEdge * 100:F0}% threshold. Pass on this one.");
            Console.ResetColor();
            Console.Write("  Press Enter...");
            Console.ReadLine();
        }
    }

    // ─── Queue ───────────────────────────────────────────────────────────────

    private void QueueMenu()
    {
        while (true)
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  ── QUEUE ────────────────────────────────────────");
            Console.ResetColor();

            if (_state.Queue.Count == 0)
            {
                Console.WriteLine("\n  No bets queued. Find edges in the Scanner.");
                Console.Write("\n  Press Enter...");
                Console.ReadLine();
                return;
            }

            var total = _state.Queue.Sum(b => b.Size);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {_state.Queue.Count} pending approval · Total exposure: {MathEngine.FmtUsd(total)}");
            Console.ResetColor();
            Console.WriteLine();

            for (int i = 0; i < _state.Queue.Count; i++)
            {
                var b = _state.Queue[i];
                Console.Write($"  {i + 1,2}. ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{b.Pick,-22}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($" {MathEngine.FmtAm(b.Odds),6}");
                Console.ResetColor();
                Console.Write($" @ {b.Book}");
                Console.WriteLine();
                Console.Write("      ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{b.Game}");
                Console.ResetColor();
                Console.WriteLine();

                Console.Write("      edge:");
                var edgeColor = b.Edge >= 0.08 ? ConsoleColor.Green
                              : b.Edge >= Config.MinEdge ? ConsoleColor.Yellow
                              : ConsoleColor.DarkGray;
                Console.ForegroundColor = edgeColor;
                Console.Write($"{(b.Edge > 0 ? "+" : "")}{MathEngine.FmtPct(b.Edge),7}");
                Console.ResetColor();
                Console.Write($"  kelly:{MathEngine.FmtPct(b.Kelly),6}");
                Console.Write("  risk:");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{MathEngine.FmtUsd(b.Size)}");
                Console.ResetColor();
                Console.Write("  to-win:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"+{MathEngine.FmtUsd(b.Payout - b.Size)}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine("  Enter # to [A]pprove/[K]ill, or [B]ack");
            Console.Write("  > ");
            var input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "B") return;

            if (int.TryParse(input, out var num) && num >= 1 && num <= _state.Queue.Count)
            {
                var bet = _state.Queue[num - 1];
                Console.Write($"  {bet.Pick} · {MathEngine.FmtUsd(bet.Size)} — [A]pprove or [K]ill? ");
                var action = Console.ReadLine()?.Trim().ToUpperInvariant();

                if (action == "A")
                {
                    _state.Bankroll = Math.Round(_state.Bankroll - bet.Size, 2);
                    _state.Queue.RemoveAt(num - 1);
                    bet.Result = "pending";
                    _state.Ledger.Add(bet);
                    StateStore.Save(_state);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  Approved. {MathEngine.FmtUsd(bet.Size)} deducted from bankroll.");
                    Console.ResetColor();
                    Console.Write("  Press Enter...");
                    Console.ReadLine();
                }
                else if (action == "K")
                {
                    _state.Queue.RemoveAt(num - 1);
                    StateStore.Save(_state);
                    Console.WriteLine("  Bet removed from queue.");
                    Console.Write("  Press Enter...");
                    Console.ReadLine();
                }
            }
        }
    }

    // ─── Ledger ──────────────────────────────────────────────────────────────

    private void LedgerMenu()
    {
        while (true)
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  ── LEDGER ───────────────────────────────────────");
            Console.ResetColor();

            if (_state.Ledger.Count == 0)
            {
                Console.WriteLine("\n  No bets in ledger. Approve bets from the Queue to begin tracking.");
                Console.Write("\n  Press Enter...");
                Console.ReadLine();
                return;
            }

            // Stats
            var settled = _state.Ledger.Where(b => b.Result != "pending").ToList();
            var wins = settled.Count(b => b.Result == "win");
            var losses = settled.Count(b => b.Result == "loss");
            var pnl = CalcPnl();
            var risked = settled.Sum(b => b.Size);
            var roi = risked > 0 ? pnl / risked : 0;

            Console.WriteLine();
            Console.Write("  Bankroll: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(MathEngine.FmtUsd(_state.Bankroll));
            Console.ResetColor();

            Console.Write("   P&L: ");
            Console.ForegroundColor = pnl >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{(pnl >= 0 ? "+" : "-")}{MathEngine.FmtUsd(Math.Abs(pnl))}");
            Console.ResetColor();

            Console.Write($"   Record: {wins}W-{losses}L");

            Console.Write("   ROI: ");
            Console.ForegroundColor = roi >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{(roi >= 0 ? "+" : "-")}{MathEngine.FmtPct((double)Math.Abs(roi))}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();

            // List bets (newest first)
            var reversed = _state.Ledger.AsEnumerable().Reverse().ToList();
            int pending = 0;

            for (int i = 0; i < reversed.Count; i++)
            {
                var b = reversed[i];
                var origIdx = _state.Ledger.Count - 1 - i;

                Console.Write($"  {i + 1,2}. ");

                // Result badge
                if (b.Result == "win")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[WIN]  ");
                }
                else if (b.Result == "loss")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[LOSS] ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("[PEND] ");
                    pending++;
                }
                Console.ResetColor();

                Console.Write($"{b.Pick,-22} {MathEngine.FmtAm(b.Odds),6}  ");
                Console.Write($"risked:{MathEngine.FmtUsd(b.Size)}  edge:{(b.Edge > 0 ? "+" : "")}{MathEngine.FmtPct(b.Edge)}");

                if (b.Result == "win")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"  profit:+{MathEngine.FmtUsd(b.Payout - b.Size)}");
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"      {b.Game} · {b.Book}");
                Console.ResetColor();
            }

            Console.WriteLine();
            if (pending > 0)
                Console.WriteLine("  Enter # to settle a pending bet, or [B]ack");
            else
                Console.WriteLine("  [B]ack");
            Console.Write("  > ");
            var input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "B") return;

            if (int.TryParse(input, out var num) && num >= 1 && num <= reversed.Count)
            {
                var bet = reversed[num - 1];
                if (bet.Result != "pending")
                {
                    Console.WriteLine("  That bet is already settled.");
                    Console.Write("  Press Enter...");
                    Console.ReadLine();
                    continue;
                }

                Console.Write($"  {bet.Pick} · {MathEngine.FmtUsd(bet.Size)} — [W]in or [L]oss? ");
                var result = Console.ReadLine()?.Trim().ToUpperInvariant();

                if (result == "W")
                {
                    bet.Result = "win";
                    bet.Payout = Math.Round(bet.Size * (decimal)MathEngine.AmToDec(bet.Odds), 2);
                    _state.Bankroll = Math.Round(_state.Bankroll + bet.Payout, 2);
                    StateStore.Save(_state);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  WIN! +{MathEngine.FmtUsd(bet.Payout)} added to bankroll.");
                    Console.ResetColor();
                    Console.Write("  Press Enter...");
                    Console.ReadLine();
                }
                else if (result == "L")
                {
                    bet.Result = "loss";
                    bet.Payout = 0;
                    StateStore.Save(_state);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  Loss. {MathEngine.FmtUsd(bet.Size)} lost.");
                    Console.ResetColor();
                    Console.Write("  Press Enter...");
                    Console.ReadLine();
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
