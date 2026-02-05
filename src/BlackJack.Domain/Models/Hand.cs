namespace BlackJack.Domain;

public sealed class Hand
{
  private readonly List<Card> _cards = new();

  public IReadOnlyList<Card> Cards => _cards;

  public int BestValue
  {
    get
    {
      var (totals, _) = CalculateTotals();
      var bestUnder = totals.Where(total => total <= 21).DefaultIfEmpty().Max();
      return bestUnder == 0 ? totals.Min() : bestUnder;
    }
  }

  public bool IsBlackjack => _cards.Count == 2 && BestValue == 21;

  public bool IsBust
  {
    get
    {
      var (totals, _) = CalculateTotals();
      return totals.All(total => total > 21);
    }
  }

  public bool IsSoft
  {
    get
    {
      var (totals, minTotal) = CalculateTotals();
      return totals.Any(total => total <= 21 && total != minTotal);
    }
  }

  public void Add(Card card) => _cards.Add(card);

  public void Clear() => _cards.Clear();

  public IReadOnlyList<int> GetTotals() => CalculateTotals().Totals;

  private (List<int> Totals, int MinTotal) CalculateTotals()
  {
    var baseTotal = 0;
    var aceCount = 0;

    foreach (var card in _cards)
    {
      if (card.IsAce)
      {
        aceCount++;
        continue;
      }

      baseTotal += card.BaseValue;
    }

    var minTotal = baseTotal + aceCount;
    var totals = new List<int>(aceCount + 1);

    for (var i = 0; i <= aceCount; i++)
    {
      totals.Add(minTotal + (i * 10));
    }

    return (totals, minTotal);
  }
}
