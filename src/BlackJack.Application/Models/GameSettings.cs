namespace BlackJack.Application;

public sealed record GameSettings(
  int DeckCount,
  bool StandOnSoft17,
  int MaxHands,
  bool AllowTenValueSplit,
  bool AllowResplitAces,
  bool RestrictSplitAcesToOneCard,
  bool AllowDoubleDownAfterSplitAces,
  decimal MinBet,
  decimal MaxBet,
  decimal StartingBalance)
{
  public static GameSettings Default => new(6, true, 4, true, true, true, false, 10m, 500m, 1000m);
}
