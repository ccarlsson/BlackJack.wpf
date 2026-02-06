using BlackJack.Application;
using BlackJack.Domain;
using Xunit;

namespace BlackJack.Tests.Application;

public sealed class GameSessionBankrollTests
{
  [Theory]
  [InlineData(OutcomeType.PlayerWin, 1.0, 1000, 10, 1010)]
  [InlineData(OutcomeType.Push, 0.0, 1000, 10, 1000)]
  [InlineData(OutcomeType.DealerWin, -1.0, 1000, 10, 990)]
  public void TryStartRound_SettlesBankroll_ForNormalOutcomes(
    OutcomeType outcome,
    double payoutMultiplier,
    decimal startingBankroll,
    decimal bet,
    decimal expectedFinalBankroll)
  {
    var settings = new GameSettings(
      DeckCount: 1,
      StandOnSoft17: true,
      MaxHands: 4,
      AllowTenValueSplit: true,
      AllowResplitAces: true,
      RestrictSplitAcesToOneCard: true,
      AllowDoubleDownAfterSplitAces: false,
      MinBet: 1m,
      MaxBet: 500m,
      StartingBalance: startingBankroll);

    var fakeService = new FakeResolvedGameService(
      baseBet: bet,
      handResult: new HandResult(
        HandIndex: 0,
        PlayerValue: 20,
        DealerValue: 18,
        Outcome: outcome,
        PlayerBlackjack: false,
        DealerBlackjack: false,
        PlayerBust: false,
        DealerBust: false,
        PayoutMultiplier: (decimal)payoutMultiplier));

    var session = new GameSession(fakeService, new DummyRandomProvider(), settings);

    var ok = session.TryStartRound("Tester", bet, out var error);

    Assert.True(ok, error);
    Assert.Equal(expectedFinalBankroll, session.Bankroll);
  }

  [Fact]
  public void TryStartRound_SettlesBankroll_ForBlackjack_ThreeToTwo()
  {
    var startingBankroll = 1000m;
    var bet = 10m;

    var settings = GameSettings.Default with
    {
      MinBet = 1m,
      MaxBet = 500m,
      StartingBalance = startingBankroll
    };

    var fakeService = new FakeResolvedGameService(
      baseBet: bet,
      handResult: new HandResult(
        HandIndex: 0,
        PlayerValue: 21,
        DealerValue: 20,
        Outcome: OutcomeType.PlayerWin,
        PlayerBlackjack: true,
        DealerBlackjack: false,
        PlayerBust: false,
        DealerBust: false,
        PayoutMultiplier: 1.5m));

    var session = new GameSession(fakeService, new DummyRandomProvider(), settings);

    var ok = session.TryStartRound("Tester", bet, out var error);

    Assert.True(ok, error);
    Assert.Equal(1015m, session.Bankroll);
  }

  [Fact]
  public void TryDoubleDown_SettlesBankroll_ForWin()
  {
    var startingBankroll = 1000m;
    var bet = 10m;

    var settings = GameSettings.Default with
    {
      MinBet = 1m,
      MaxBet = 500m,
      StartingBalance = startingBankroll
    };

    var handResult = new HandResult(
      HandIndex: 0,
      PlayerValue: 20,
      DealerValue: 18,
      Outcome: OutcomeType.PlayerWin,
      PlayerBlackjack: false,
      DealerBlackjack: false,
      PlayerBust: false,
      DealerBust: false,
      PayoutMultiplier: 1m);

    var fakeService = new FakeFlowGameService(bet, new[] { handResult });
    var session = new GameSession(fakeService, new DummyRandomProvider(), settings);

    var ok = session.TryStartRound("Tester", bet, out var error);
    Assert.True(ok, error);

    var doubled = session.TryDoubleDown(out var doubleDownError);
    Assert.True(doubled, doubleDownError);

    Assert.Equal(1020m, session.Bankroll);
  }

  [Fact]
  public void SplitThenStand_SettlesBankroll_ForMixedOutcomes()
  {
    var startingBankroll = 1000m;
    var bet = 10m;

    var settings = GameSettings.Default with
    {
      MinBet = 1m,
      MaxBet = 500m,
      StartingBalance = startingBankroll
    };

    var results = new[]
    {
      new HandResult(
        HandIndex: 0,
        PlayerValue: 19,
        DealerValue: 17,
        Outcome: OutcomeType.PlayerWin,
        PlayerBlackjack: false,
        DealerBlackjack: false,
        PlayerBust: false,
        DealerBust: false,
        PayoutMultiplier: 1m),
      new HandResult(
        HandIndex: 1,
        PlayerValue: 18,
        DealerValue: 20,
        Outcome: OutcomeType.DealerWin,
        PlayerBlackjack: false,
        DealerBlackjack: false,
        PlayerBust: false,
        DealerBust: false,
        PayoutMultiplier: -1m)
    };

    var fakeService = new FakeFlowGameService(bet, results);
    var session = new GameSession(fakeService, new DummyRandomProvider(), settings);

    var ok = session.TryStartRound("Tester", bet, out var error);
    Assert.True(ok, error);

    var split = session.TrySplit(out var splitError);
    Assert.True(split, splitError);

    session.Stand();

    Assert.Equal(1000m, session.Bankroll);
  }

  private sealed class DummyRandomProvider : IRandomProvider
  {
    public int Next(int minInclusive, int maxExclusive) => minInclusive;
  }

  private sealed class FakeResolvedGameService : IGameService
  {
    private readonly decimal _baseBet;
    private readonly HandResult _handResult;

    public FakeResolvedGameService(decimal baseBet, HandResult handResult)
    {
      _baseBet = baseBet;
      _handResult = handResult;
    }

    public RoundState StartNewRound(GameSettings settings, IRandomProvider randomProvider, string playerName, decimal baseBet)
    {
      var shoe = new Shoe(1);
      var player = new HumanPlayer(playerName);
      var dealer = new Dealer("Dealer");

      var state = new RoundState(
        shoe,
        player,
        dealer,
        standOnSoft17: true,
        maxHands: settings.MaxHands,
        allowTenValueSplit: settings.AllowTenValueSplit,
        allowResplitAces: settings.AllowResplitAces,
        restrictSplitAcesToOneCard: settings.RestrictSplitAcesToOneCard,
        allowDoubleDownAfterSplitAces: settings.AllowDoubleDownAfterSplitAces,
        baseBet: _baseBet);

      state.IsPlayerTurn = false;
      state.IsRoundOver = true;

      return state;
    }

    public RoundResult ResolveRound(RoundState state)
      => new(new[] { _handResult });

    public RoundState PlayerHit(RoundState state) => throw new NotSupportedException();

    public RoundState PlayerStand(RoundState state) => throw new NotSupportedException();

    public RoundState PlayerDoubleDown(RoundState state) => throw new NotSupportedException();

    public RoundState PlayerSplit(RoundState state) => throw new NotSupportedException();
  }

  private sealed class FakeFlowGameService : IGameService
  {
    private readonly decimal _baseBet;
    private readonly IReadOnlyList<HandResult> _handResults;
    private RoundState? _state;

    public FakeFlowGameService(decimal baseBet, IReadOnlyList<HandResult> handResults)
    {
      _baseBet = baseBet;
      _handResults = handResults;
    }

    public RoundState StartNewRound(GameSettings settings, IRandomProvider randomProvider, string playerName, decimal baseBet)
    {
      var shoe = new Shoe(1);
      var player = new HumanPlayer(playerName);
      var dealer = new Dealer("Dealer");

      _state = new RoundState(
        shoe,
        player,
        dealer,
        standOnSoft17: true,
        maxHands: settings.MaxHands,
        allowTenValueSplit: settings.AllowTenValueSplit,
        allowResplitAces: settings.AllowResplitAces,
        restrictSplitAcesToOneCard: settings.RestrictSplitAcesToOneCard,
        allowDoubleDownAfterSplitAces: settings.AllowDoubleDownAfterSplitAces,
        baseBet: _baseBet);

      _state.IsPlayerTurn = true;
      _state.IsRoundOver = false;

      return _state;
    }

    public RoundState PlayerDoubleDown(RoundState state)
    {
      state.IncreaseHandBet(0, _baseBet);
      state.IsPlayerTurn = false;
      state.IsRoundOver = false;
      return state;
    }

    public RoundState PlayerSplit(RoundState state)
    {
      state.AddHandBet(_baseBet);
      state.IsPlayerTurn = true;
      state.IsRoundOver = false;
      return state;
    }

    public RoundState PlayerStand(RoundState state)
    {
      state.IsPlayerTurn = false;
      state.IsRoundOver = false;
      return state;
    }

    public RoundResult ResolveRound(RoundState state)
      => new(_handResults);

    public RoundState PlayerHit(RoundState state) => throw new NotSupportedException();
  }
}
