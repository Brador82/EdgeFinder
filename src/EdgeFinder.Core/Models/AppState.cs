namespace EdgeFinder.Core.Models;

public class AppState
{
    public decimal Bankroll { get; set; } = 100m;
    public List<Bet> Queue { get; set; } = [];
    public List<Bet> Ledger { get; set; } = [];
}
