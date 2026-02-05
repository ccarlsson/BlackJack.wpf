namespace BlackJack.Domain;

public sealed class Dealer : Player
{
  public Dealer(string name) : base(name)
  {
  }

  public bool ShouldStand(Hand hand, bool standOnSoft17)
  {
    var value = hand.BestValue;

    if (value > 17)
    {
      return true;
    }

    if (value < 17)
    {
      return false;
    }

    return standOnSoft17 || !hand.IsSoft;
  }
}
