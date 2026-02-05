using BlackJack.Domain;
using Xunit;

namespace BlackJack.Tests.Domain;

public class DealerTests
{
  [Fact]
  public void ShouldStand_OnSoft17_WhenConfiguredTrue()
  {
    var dealer = new Dealer("Dealer");
    var hand = new Hand();
    hand.Add(new Card(Suit.Spades, Rank.Ace));
    hand.Add(new Card(Suit.Hearts, Rank.Six));

    Assert.True(dealer.ShouldStand(hand, standOnSoft17: true));
  }

  [Fact]
  public void ShouldHit_OnSoft17_WhenConfiguredFalse()
  {
    var dealer = new Dealer("Dealer");
    var hand = new Hand();
    hand.Add(new Card(Suit.Spades, Rank.Ace));
    hand.Add(new Card(Suit.Hearts, Rank.Six));

    Assert.False(dealer.ShouldStand(hand, standOnSoft17: false));
  }
}
