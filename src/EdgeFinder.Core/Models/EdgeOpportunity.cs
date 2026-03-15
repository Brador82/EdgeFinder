namespace EdgeFinder.Core.Models;

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
