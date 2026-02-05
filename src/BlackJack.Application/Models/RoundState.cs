using BlackJack.Domain;

namespace BlackJack.Application;

public sealed class RoundState
{
  private readonly HashSet<int> _lockedHandIndices = new();
  private readonly List<decimal> _handBets = new();

  public RoundState(
    Shoe shoe,
    HumanPlayer player,
    Dealer dealer,
    bool standOnSoft17,
    int maxHands,
    bool allowTenValueSplit,
    bool allowResplitAces,
    bool restrictSplitAcesToOneCard,
    bool allowDoubleDownAfterSplitAces,
    decimal baseBet)
  {
    Shoe = shoe ?? throw new ArgumentNullException(nameof(shoe));
    Player = player ?? throw new ArgumentNullException(nameof(player));
    Dealer = dealer ?? throw new ArgumentNullException(nameof(dealer));
    StandOnSoft17 = standOnSoft17;
    MaxHands = maxHands;
    AllowTenValueSplit = allowTenValueSplit;
    AllowResplitAces = allowResplitAces;
    RestrictSplitAcesToOneCard = restrictSplitAcesToOneCard;
    AllowDoubleDownAfterSplitAces = allowDoubleDownAfterSplitAces;
    BaseBet = baseBet;
    _handBets.Add(baseBet);
    IsPlayerTurn = true;
  }

  public Shoe Shoe { get; }

  public HumanPlayer Player { get; }

  public Dealer Dealer { get; }

  public bool StandOnSoft17 { get; }

  public int MaxHands { get; }

  public bool AllowTenValueSplit { get; }

  public bool AllowResplitAces { get; }

  public bool RestrictSplitAcesToOneCard { get; }

  public bool AllowDoubleDownAfterSplitAces { get; }

  public decimal BaseBet { get; }

  public IReadOnlyList<decimal> HandBets => _handBets;

  public bool IsPlayerTurn { get; internal set; }

  public bool IsRoundOver { get; internal set; }

  public IReadOnlySet<int> LockedHandIndices => _lockedHandIndices;

  public bool IsHandLocked(int index) => _lockedHandIndices.Contains(index);

  internal void LockHand(int index) => _lockedHandIndices.Add(index);

  internal void AddHandBet(decimal bet) => _handBets.Add(bet);

  internal void IncreaseHandBet(int index, decimal amount) => _handBets[index] += amount;
}
