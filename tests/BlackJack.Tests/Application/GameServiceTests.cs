using BlackJack.Application;
using BlackJack.Domain;
using Xunit;

namespace BlackJack.Tests.Application;

public class GameServiceTests
{
  [Fact]
  public void StartNewRound_DealsTwoCardsEach()
  {
    var service = new GameService();
    var settings = new GameSettings(1, true, 4, true, true, true, false);
    var state = service.StartNewRound(settings, new FixedRandomProvider(), "Tester");

    Assert.Equal(2, state.Player.ActiveHand.Cards.Count);
    Assert.Equal(2, state.Dealer.ActiveHand.Cards.Count);
    Assert.Equal(48, state.Shoe.Remaining);
    Assert.Equal(!state.IsRoundOver, state.IsPlayerTurn);
  }

  [Fact]
  public void PlayerHit_AddsCardToHand()
  {
    var service = new GameService();
    var state = BuildState();

    var updated = service.PlayerHit(state);

    Assert.Equal(3, updated.Player.ActiveHand.Cards.Count);
    Assert.True(updated.IsPlayerTurn);
  }

  [Fact]
  public void PlayerStand_EndsPlayerTurn()
  {
    var service = new GameService();
    var state = BuildState();

    var updated = service.PlayerStand(state);

    Assert.False(updated.IsPlayerTurn);
  }

  [Fact]
  public void PlayerSplit_CreatesSecondHandAndDealsCards()
  {
    var service = new GameService();
    var state = BuildSplitState();

    var updated = service.PlayerSplit(state);

    Assert.Equal(2, updated.Player.Hands.Count);
    Assert.Equal(2, updated.Player.Hands[0].Cards.Count);
    Assert.Equal(2, updated.Player.Hands[1].Cards.Count);
    Assert.Equal(0, updated.Player.ActiveHandIndex);
  }

  [Fact]
  public void PlayerSplit_AllowsTenValuePair()
  {
    var service = new GameService();
    var state = BuildTenValueSplitState();

    var updated = service.PlayerSplit(state);

    Assert.Equal(2, updated.Player.Hands.Count);
  }

  [Fact]
  public void PlayerSplit_Aces_LocksHandsAndKeepsTurn()
  {
    var service = new GameService();
    var state = BuildAceSplitState();

    var updated = service.PlayerSplit(state);

    Assert.True(updated.IsPlayerTurn);
    Assert.True(updated.IsHandLocked(0));
    Assert.True(updated.IsHandLocked(1));
  }

  private static RoundState BuildState()
  {
    var shoe = new Shoe(1);
    var player = new HumanPlayer("Tester");
    var dealer = new Dealer("Dealer");

    player.ActiveHand.Add(new Card(Suit.Spades, Rank.Two));
    player.ActiveHand.Add(new Card(Suit.Hearts, Rank.Three));
    dealer.ActiveHand.Add(new Card(Suit.Clubs, Rank.Four));
    dealer.ActiveHand.Add(new Card(Suit.Diamonds, Rank.Five));

    return new RoundState(
      shoe,
      player,
      dealer,
      standOnSoft17: true,
      maxHands: 4,
      allowTenValueSplit: true,
      allowResplitAces: true,
      restrictSplitAcesToOneCard: true,
      allowDoubleDownAfterSplitAces: false);
  }

  private static RoundState BuildSplitState()
  {
    var shoe = new Shoe(1);
    var player = new HumanPlayer("Tester");
    var dealer = new Dealer("Dealer");

    player.ActiveHand.Add(new Card(Suit.Spades, Rank.Eight));
    player.ActiveHand.Add(new Card(Suit.Hearts, Rank.Eight));
    dealer.ActiveHand.Add(new Card(Suit.Clubs, Rank.Four));
    dealer.ActiveHand.Add(new Card(Suit.Diamonds, Rank.Five));

    return new RoundState(
      shoe,
      player,
      dealer,
      standOnSoft17: true,
      maxHands: 4,
      allowTenValueSplit: true,
      allowResplitAces: true,
      restrictSplitAcesToOneCard: true,
      allowDoubleDownAfterSplitAces: false);
  }

  private static RoundState BuildTenValueSplitState()
  {
    var shoe = new Shoe(1);
    var player = new HumanPlayer("Tester");
    var dealer = new Dealer("Dealer");

    player.ActiveHand.Add(new Card(Suit.Spades, Rank.King));
    player.ActiveHand.Add(new Card(Suit.Hearts, Rank.Queen));
    dealer.ActiveHand.Add(new Card(Suit.Clubs, Rank.Four));
    dealer.ActiveHand.Add(new Card(Suit.Diamonds, Rank.Five));

    return new RoundState(
      shoe,
      player,
      dealer,
      standOnSoft17: true,
      maxHands: 4,
      allowTenValueSplit: true,
      allowResplitAces: true,
      restrictSplitAcesToOneCard: true,
      allowDoubleDownAfterSplitAces: false);
  }

  private static RoundState BuildAceSplitState()
  {
    var shoe = new Shoe(1);
    var player = new HumanPlayer("Tester");
    var dealer = new Dealer("Dealer");

    player.ActiveHand.Add(new Card(Suit.Spades, Rank.Ace));
    player.ActiveHand.Add(new Card(Suit.Hearts, Rank.Ace));
    dealer.ActiveHand.Add(new Card(Suit.Clubs, Rank.Four));
    dealer.ActiveHand.Add(new Card(Suit.Diamonds, Rank.Five));

    return new RoundState(
      shoe,
      player,
      dealer,
      standOnSoft17: true,
      maxHands: 4,
      allowTenValueSplit: true,
      allowResplitAces: true,
      restrictSplitAcesToOneCard: true,
      allowDoubleDownAfterSplitAces: false);
  }

  private sealed class FixedRandomProvider : IRandomProvider
  {
    public int Next(int minInclusive, int maxExclusive) => minInclusive;
  }
}
