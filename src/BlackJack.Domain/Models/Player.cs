namespace BlackJack.Domain;

public abstract class Player
{
  private readonly List<Hand> _hands = new();

  protected Player(string name)
  {
    Name = name;
    StartNewRound();
  }

  public string Name { get; }

  public IReadOnlyList<Hand> Hands => _hands;

  public int ActiveHandIndex { get; private set; }

  public Hand ActiveHand => _hands[ActiveHandIndex];

  public void StartNewRound()
  {
    _hands.Clear();
    _hands.Add(new Hand());
    ActiveHandIndex = 0;
  }

  public void AddHand(Hand hand)
  {
    _hands.Add(hand);
  }

  public bool HasNextHand() => ActiveHandIndex + 1 < _hands.Count;

  public void MoveToNextHand()
  {
    if (!HasNextHand())
    {
      throw new InvalidOperationException("No more hands.");
    }

    ActiveHandIndex++;
  }
}
