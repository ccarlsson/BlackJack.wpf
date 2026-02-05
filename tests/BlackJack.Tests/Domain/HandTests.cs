using BlackJack.Domain;
using Xunit;

namespace BlackJack.Tests.Domain;

public class HandTests
{
  [Fact]
  public void BestValue_AceAndKing_IsBlackjack()
  {
    var hand = new Hand();
    hand.Add(new Card(Suit.Spades, Rank.Ace));
    hand.Add(new Card(Suit.Hearts, Rank.King));

    Assert.Equal(21, hand.BestValue);
    Assert.True(hand.IsBlackjack);
    Assert.False(hand.IsBust);
  }

  [Fact]
  public void BestValue_AceAndSix_IsSoft17()
  {
    var hand = new Hand();
    hand.Add(new Card(Suit.Spades, Rank.Ace));
    hand.Add(new Card(Suit.Hearts, Rank.Six));

    Assert.Equal(17, hand.BestValue);
    Assert.True(hand.IsSoft);
  }

  [Fact]
  public void IsBust_WhenOver21()
  {
    var hand = new Hand();
    hand.Add(new Card(Suit.Spades, Rank.Ten));
    hand.Add(new Card(Suit.Hearts, Rank.Queen));
    hand.Add(new Card(Suit.Clubs, Rank.Two));

    Assert.True(hand.IsBust);
  }

  [Fact]
  public void BestValue_MultipleAces_PicksBestUnder21()
  {
    var hand = new Hand();
    hand.Add(new Card(Suit.Spades, Rank.Ace));
    hand.Add(new Card(Suit.Hearts, Rank.Ace));
    hand.Add(new Card(Suit.Clubs, Rank.Nine));

    Assert.Equal(21, hand.BestValue);
    Assert.True(hand.IsSoft);
  }
}
