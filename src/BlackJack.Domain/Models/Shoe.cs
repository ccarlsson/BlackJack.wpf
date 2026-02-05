namespace BlackJack.Domain;

public sealed class Shoe
{
  private readonly List<Card> _cards;

  public Shoe(int deckCount)
  {
    if (deckCount < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(deckCount));
    }

    DeckCount = deckCount;
    _cards = BuildCards(deckCount);
  }

  public int DeckCount { get; }

  public int Remaining => _cards.Count;

  public void Reset()
  {
    _cards.Clear();
    _cards.AddRange(BuildCards(DeckCount));
  }

  public void Shuffle(IRandomProvider randomProvider)
  {
    if (randomProvider is null)
    {
      throw new ArgumentNullException(nameof(randomProvider));
    }

    for (var i = _cards.Count - 1; i > 0; i--)
    {
      var j = randomProvider.Next(0, i + 1);
      (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
    }
  }

  public Card Draw()
  {
    if (_cards.Count == 0)
    {
      throw new InvalidOperationException("Shoe is empty.");
    }

    var card = _cards[^1];
    _cards.RemoveAt(_cards.Count - 1);
    return card;
  }

  private static List<Card> BuildCards(int deckCount)
  {
    var cards = new List<Card>(deckCount * 52);

    for (var deck = 0; deck < deckCount; deck++)
    {
      foreach (var suit in Enum.GetValues<Suit>())
      {
        foreach (var rank in Enum.GetValues<Rank>())
        {
          cards.Add(new Card(suit, rank));
        }
      }
    }

    return cards;
  }
}
