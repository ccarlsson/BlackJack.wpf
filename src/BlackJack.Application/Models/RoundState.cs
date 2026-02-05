using BlackJack.Domain;

namespace BlackJack.Application;

public sealed class RoundState
{
  public RoundState(Shoe shoe, HumanPlayer player, Dealer dealer, bool standOnSoft17)
  {
    Shoe = shoe ?? throw new ArgumentNullException(nameof(shoe));
    Player = player ?? throw new ArgumentNullException(nameof(player));
    Dealer = dealer ?? throw new ArgumentNullException(nameof(dealer));
    StandOnSoft17 = standOnSoft17;
    IsPlayerTurn = true;
  }

  public Shoe Shoe { get; }

  public HumanPlayer Player { get; }

  public Dealer Dealer { get; }

  public bool StandOnSoft17 { get; }

  public bool IsPlayerTurn { get; internal set; }

  public bool IsRoundOver { get; internal set; }
}
