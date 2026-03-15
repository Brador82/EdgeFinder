namespace EdgeFinder.Models;

public class Bet
{
    public long Id { get; set; }
    public string Game { get; set; } = "";
    public string Pick { get; set; } = "";
    public int Odds { get; set; }
    public string Book { get; set; } = "";
    public double MarketProb { get; set; }
    public double UserProb { get; set; }
    public double Edge { get; set; }
    public double Kelly { get; set; }
    public decimal Size { get; set; }
    public decimal Payout { get; set; }
    public string Result { get; set; } = "pending";
}
