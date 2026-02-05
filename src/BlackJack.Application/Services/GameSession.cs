using BlackJack.Domain;

namespace BlackJack.Application;

public sealed class GameSession : IGameSession
{
  private readonly IGameService _gameService;
  private readonly IRandomProvider _randomProvider;
  private GameSettings _settings;

  public GameSession(IGameService gameService, IRandomProvider randomProvider, GameSettings settings)
  {
    _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
    _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
    _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    Bankroll = _settings.StartingBalance;
    Status = "Select 'New round' to start.";
  }

  public decimal Bankroll { get; private set; }

  public decimal MinBet => _settings.MinBet;

  public decimal MaxBet => _settings.MaxBet;

  public string Status { get; private set; }

  public RoundState? RoundState { get; private set; }

  public RoundResult? LastResult { get; private set; }

  public void UpdateSettings(GameSettings settings)
  {
    _settings = settings ?? throw new ArgumentNullException(nameof(settings));
  }

  public bool TryStartRound(string playerName, decimal bet, out string error)
  {
    error = "";

    if (bet < _settings.MinBet || bet > _settings.MaxBet)
    {
      error = $"Bet must be between {_settings.MinBet:0} and {_settings.MaxBet:0}.";
      return false;
    }

    if (bet > Bankroll)
    {
      error = "Bet exceeds available balance.";
      return false;
    }

    Bankroll -= bet;
    LastResult = null;
    RoundState = _gameService.StartNewRound(_settings, _randomProvider, playerName, bet);

    if (RoundState.IsRoundOver)
    {
      LastResult = _gameService.ResolveRound(RoundState);
      ApplyPayout(LastResult, RoundState);
    }

    Status = RoundState.IsRoundOver && LastResult is not null
      ? BuildRoundSummary(LastResult)
      : "New round started.";

    return true;
  }

  public void Hit()
  {
    if (RoundState is null)
    {
      return;
    }

    RoundState = _gameService.PlayerHit(RoundState);
    ResolveIfReady();
  }

  public void Stand()
  {
    if (RoundState is null)
    {
      return;
    }

    RoundState = _gameService.PlayerStand(RoundState);
    ResolveIfReady();
  }

  public bool TryDoubleDown(out string error)
  {
    error = "";

    if (RoundState is null)
    {
      error = "No active round.";
      return false;
    }

    if (Bankroll < RoundState.BaseBet)
    {
      error = "Not enough balance to double down.";
      return false;
    }

    RoundState = _gameService.PlayerDoubleDown(RoundState);
    Bankroll -= RoundState.BaseBet;
    Status = "Double down resolved.";
    ResolveIfReady();
    return true;
  }

  public bool TrySplit(out string error)
  {
    error = "";

    if (RoundState is null)
    {
      error = "No active round.";
      return false;
    }

    if (Bankroll < RoundState.BaseBet)
    {
      error = "Not enough balance to split.";
      return false;
    }

    RoundState = _gameService.PlayerSplit(RoundState);
    Bankroll -= RoundState.BaseBet;
    Status = "Split completed.";
    return true;
  }

  private void ResolveIfReady()
  {
    if (RoundState is null)
    {
      return;
    }

    if (RoundState.IsRoundOver || RoundState.IsPlayerTurn)
    {
      return;
    }

    LastResult = _gameService.ResolveRound(RoundState);
    ApplyPayout(LastResult, RoundState);
    Status = BuildRoundSummary(LastResult);
  }

  private void ApplyPayout(RoundResult result, RoundState state)
  {
    var net = 0m;

    foreach (var handResult in result.HandResults)
    {
      if (handResult.HandIndex < 0 || handResult.HandIndex >= state.HandBets.Count)
      {
        continue;
      }

      net += handResult.PayoutMultiplier * state.HandBets[handResult.HandIndex];
    }

    Bankroll += net;
  }

  private static string BuildRoundSummary(RoundResult result)
  {
    if (result.HandResults.Count == 0)
    {
      return "Round complete.";
    }

    if (result.HandResults.Any(hand => hand.PlayerBlackjack && hand.Outcome == OutcomeType.PlayerWin))
    {
      return "Blackjack!";
    }

    if (result.HandResults.Any(hand => hand.PlayerBlackjack && hand.Outcome == OutcomeType.Push))
    {
      return "Blackjack push.";
    }

    return "Round complete.";
  }
}
