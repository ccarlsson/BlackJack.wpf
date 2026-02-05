using BlackJack.Domain;

namespace BlackJack.Application;

public sealed class GameService : IGameService
{
  public RoundState StartNewRound(GameSettings settings, IRandomProvider randomProvider, string playerName)
  {
    if (settings is null)
    {
      throw new ArgumentNullException(nameof(settings));
    }

    if (randomProvider is null)
    {
      throw new ArgumentNullException(nameof(randomProvider));
    }

    if (string.IsNullOrWhiteSpace(playerName))
    {
      throw new ArgumentException("Player name is required.", nameof(playerName));
    }

    var shoe = new Shoe(settings.DeckCount);
    shoe.Shuffle(randomProvider);

    var player = new HumanPlayer(playerName.Trim());
    var dealer = new Dealer("Dealer");

    DealInitialCards(shoe, player, dealer);

    var state = new RoundState(
      shoe,
      player,
      dealer,
      settings.StandOnSoft17,
      settings.MaxHands,
      settings.AllowTenValueSplit,
      settings.AllowResplitAces,
      settings.RestrictSplitAcesToOneCard,
      settings.AllowDoubleDownAfterSplitAces);

    if (player.ActiveHand.IsBlackjack || dealer.ActiveHand.IsBlackjack)
    {
      state.IsPlayerTurn = false;
      state.IsRoundOver = true;
    }

    return state;
  }

  public RoundState PlayerHit(RoundState state)
  {
    if (state is null)
    {
      throw new ArgumentNullException(nameof(state));
    }

    EnsurePlayerTurn(state);

    if (state.IsHandLocked(state.Player.ActiveHandIndex))
    {
      throw new InvalidOperationException("Active hand is locked.");
    }

    state.Player.ActiveHand.Add(state.Shoe.Draw());

    if (state.Player.ActiveHand.IsBust)
    {
      AdvancePlayerHand(state);
    }

    return state;
  }

  public RoundState PlayerStand(RoundState state)
  {
    if (state is null)
    {
      throw new ArgumentNullException(nameof(state));
    }

    EnsurePlayerTurn(state);
    AdvancePlayerHand(state);
    return state;
  }

  public RoundState PlayerSplit(RoundState state)
  {
    if (state is null)
    {
      throw new ArgumentNullException(nameof(state));
    }

    EnsurePlayerTurn(state);

    if (!CanSplit(state))
    {
      throw new InvalidOperationException("Split is not available.");
    }

    var activeHand = state.Player.ActiveHand;
    var isAceSplit = activeHand.Cards[0].Rank == Rank.Ace && activeHand.Cards[1].Rank == Rank.Ace;
    var secondCard = activeHand.RemoveAt(1);
    var splitHand = new Hand();
    splitHand.Add(secondCard);
    state.Player.AddHand(splitHand);

    activeHand.Add(state.Shoe.Draw());
    splitHand.Add(state.Shoe.Draw());

    if (isAceSplit && state.RestrictSplitAcesToOneCard)
    {
      var activeIndex = state.Player.ActiveHandIndex;
      var splitIndex = state.Player.Hands.Count - 1;
      state.LockHand(activeIndex);
      state.LockHand(splitIndex);

      if (!state.AllowResplitAces)
      {
        state.IsPlayerTurn = false;
      }
    }

    return state;
  }

  public RoundResult FinishRound(RoundState state)
  {
    if (state is null)
    {
      throw new ArgumentNullException(nameof(state));
    }

    if (!state.IsRoundOver)
    {
      if (state.IsPlayerTurn)
      {
        throw new InvalidOperationException("Player turn is still active.");
      }

      DealerPlay(state);
      state.IsRoundOver = true;
    }

    return Evaluate(state);
  }

  private static void DealInitialCards(Shoe shoe, HumanPlayer player, Dealer dealer)
  {
    player.StartNewRound();
    dealer.StartNewRound();

    player.ActiveHand.Add(shoe.Draw());
    dealer.ActiveHand.Add(shoe.Draw());
    player.ActiveHand.Add(shoe.Draw());
    dealer.ActiveHand.Add(shoe.Draw());
  }

  private static void EnsurePlayerTurn(RoundState state)
  {
    if (state.IsRoundOver)
    {
      throw new InvalidOperationException("Round is already over.");
    }

    if (!state.IsPlayerTurn)
    {
      throw new InvalidOperationException("It is not the player's turn.");
    }
  }

  private static void AdvancePlayerHand(RoundState state)
  {
    if (state.Player.HasNextHand())
    {
      state.Player.MoveToNextHand();
      return;
    }

    state.IsPlayerTurn = false;
  }

  private static bool CanSplit(RoundState state)
  {
    if (state.Player.Hands.Count >= state.MaxHands)
    {
      return false;
    }

    var cards = state.Player.ActiveHand.Cards;

    if (cards.Count != 2)
    {
      return false;
    }

    var sameRank = cards[0].Rank == cards[1].Rank;
    var tenValuePair = state.AllowTenValueSplit && cards[0].BaseValue == 10 && cards[1].BaseValue == 10;
    var isAcePair = cards[0].Rank == Rank.Ace && cards[1].Rank == Rank.Ace;
    var canByValue = sameRank || tenValuePair;

    if (!canByValue)
    {
      return false;
    }

    if (state.IsHandLocked(state.Player.ActiveHandIndex))
    {
      return state.AllowResplitAces && isAcePair;
    }

    return true;
  }

  private static void DealerPlay(RoundState state)
  {
    var dealerHand = state.Dealer.ActiveHand;

    while (!state.Dealer.ShouldStand(dealerHand, state.StandOnSoft17))
    {
      dealerHand.Add(state.Shoe.Draw());
    }
  }

  private static RoundResult Evaluate(RoundState state)
  {
    var dealerHand = state.Dealer.ActiveHand;
    var dealerValue = dealerHand.BestValue;
    var dealerBust = dealerHand.IsBust;
    var dealerBlackjack = dealerHand.IsBlackjack;
    var results = new List<HandResult>(state.Player.Hands.Count);

    for (var index = 0; index < state.Player.Hands.Count; index++)
    {
      var hand = state.Player.Hands[index];
      var playerValue = hand.BestValue;
      var playerBust = hand.IsBust;
      var playerBlackjack = hand.IsBlackjack;

      var outcome = EvaluateOutcome(playerValue, dealerValue, playerBust, dealerBust, playerBlackjack, dealerBlackjack);

      results.Add(new HandResult(
        index,
        playerValue,
        dealerValue,
        outcome,
        playerBlackjack,
        dealerBlackjack,
        playerBust,
        dealerBust));
    }

    return new RoundResult(results);
  }

  private static OutcomeType EvaluateOutcome(
    int playerValue,
    int dealerValue,
    bool playerBust,
    bool dealerBust,
    bool playerBlackjack,
    bool dealerBlackjack)
  {
    if (playerBlackjack && dealerBlackjack)
    {
      return OutcomeType.Push;
    }

    if (playerBlackjack)
    {
      return OutcomeType.PlayerWin;
    }

    if (dealerBlackjack)
    {
      return OutcomeType.DealerWin;
    }

    if (playerBust)
    {
      return OutcomeType.DealerWin;
    }

    if (dealerBust)
    {
      return OutcomeType.PlayerWin;
    }

    if (playerValue > dealerValue)
    {
      return OutcomeType.PlayerWin;
    }

    if (playerValue < dealerValue)
    {
      return OutcomeType.DealerWin;
    }

    return OutcomeType.Push;
  }
}
