namespace BlackJack.Domain;

public sealed record Card(Suit Suit, Rank Rank)
{
  public int BaseValue => Rank switch
  {
    Rank.Ten or Rank.Jack or Rank.Queen or Rank.King => 10,
    Rank.Ace => 1,
    _ => (int)Rank
  };

  public bool IsAce => Rank == Rank.Ace;
}
