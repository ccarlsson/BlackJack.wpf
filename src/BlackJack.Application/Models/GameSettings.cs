namespace BlackJack.Application;

public sealed record GameSettings(int DeckCount, bool StandOnSoft17)
{
  public static GameSettings Default => new(6, true);
}
