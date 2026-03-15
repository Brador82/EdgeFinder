namespace EdgeFinder.Models;

public class AppState
{
    public decimal Bankroll { get; set; } = Config.InitBankroll;
    public List<Bet> Queue { get; set; } = [];
    public List<Bet> Ledger { get; set; } = [];
}
