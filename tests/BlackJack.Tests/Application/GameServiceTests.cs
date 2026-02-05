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
    var settings = new GameSettings(1, true);
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

  private static RoundState BuildState()
  {
    var shoe = new Shoe(1);
    var player = new HumanPlayer("Tester");
    var dealer = new Dealer("Dealer");

    player.ActiveHand.Add(new Card(Suit.Spades, Rank.Two));
    player.ActiveHand.Add(new Card(Suit.Hearts, Rank.Three));
    dealer.ActiveHand.Add(new Card(Suit.Clubs, Rank.Four));
    dealer.ActiveHand.Add(new Card(Suit.Diamonds, Rank.Five));

    return new RoundState(shoe, player, dealer, standOnSoft17: true);
  }

  private sealed class FixedRandomProvider : IRandomProvider
  {
    public int Next(int minInclusive, int maxExclusive) => minInclusive;
  }
}
