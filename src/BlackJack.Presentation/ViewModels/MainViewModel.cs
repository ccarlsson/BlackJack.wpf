using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BlackJack.Application;
using BlackJack.Domain;
using BlackJack.Infrastructure;

namespace BlackJack.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
  private readonly IGameService _gameService;
  private readonly IRandomProvider _randomProvider;
  private readonly GameSettings _settings;
  private RoundState? _roundState;

  public MainViewModel()
    : this(new GameService(), new RandomProvider(), GameSettings.Default)
  {
  }

  public MainViewModel(IGameService gameService, IRandomProvider randomProvider, GameSettings settings)
  {
    _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
    _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
    _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    PlayerName = "Player";
    StatusText = "Select 'New round' to start.";
    RoundStateText = "Idle";
  }

  public ObservableCollection<string> PlayerCards { get; } = new();

  public ObservableCollection<string> DealerCards { get; } = new();

  [ObservableProperty]
  private string _title = "Black Jack";

  [ObservableProperty]
  private string _statusText;

  [ObservableProperty]
  private string _roundStateText;

  [ObservableProperty]
  private string _playerName;

  [ObservableProperty]
  private int _playerValue;

  [ObservableProperty]
  private int _dealerValue;

  [ObservableProperty]
  private bool _isRoundActive;

  [ObservableProperty]
  private bool _isPlayerTurn;

  [RelayCommand]
  private void NewRound()
  {
    _roundState = _gameService.StartNewRound(_settings, _randomProvider, PlayerName);
    UpdateFromState();
    StatusText = "New round started.";
  }

  [RelayCommand(CanExecute = nameof(CanHit))]
  private void Hit()
  {
    if (_roundState is null)
    {
      return;
    }

    _roundState = _gameService.PlayerHit(_roundState);
    UpdateFromState();
  }

  [RelayCommand(CanExecute = nameof(CanStand))]
  private void Stand()
  {
    if (_roundState is null)
    {
      return;
    }

    _roundState = _gameService.PlayerStand(_roundState);
    UpdateFromState();
  }

  [RelayCommand(CanExecute = nameof(CanFinishRound))]
  private void FinishRound()
  {
    if (_roundState is null)
    {
      return;
    }

    var result = _gameService.FinishRound(_roundState);
    UpdateFromState();
    StatusText = BuildOutcomeSummary(result);
  }

  private bool CanHit() => _roundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanStand() => _roundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanFinishRound() => _roundState is not null && IsRoundActive && !IsPlayerTurn;

  private void UpdateFromState()
  {
    if (_roundState is null)
    {
      IsRoundActive = false;
      IsPlayerTurn = false;
      PlayerValue = 0;
      DealerValue = 0;
      RoundStateText = "Idle";
      PlayerCards.Clear();
      DealerCards.Clear();
      UpdateCommandStates();
      return;
    }

    IsRoundActive = !_roundState.IsRoundOver;
    IsPlayerTurn = _roundState.IsPlayerTurn;
    PlayerValue = _roundState.Player.ActiveHand.BestValue;
    DealerValue = _roundState.Dealer.ActiveHand.BestValue;
    RoundStateText = ResolveRoundStateText(_roundState);

    PlayerCards.Clear();
    foreach (var card in _roundState.Player.ActiveHand.Cards)
    {
      PlayerCards.Add(FormatCard(card));
    }

    DealerCards.Clear();
    foreach (var card in _roundState.Dealer.ActiveHand.Cards)
    {
      DealerCards.Add(FormatCard(card));
    }

    UpdateCommandStates();
  }

  private string BuildOutcomeSummary(RoundResult result)
  {
    if (result.HandResults.Count == 0)
    {
      return "Round finished.";
    }

    var hand = result.HandResults[0];

    return hand.Outcome switch
    {
      OutcomeType.PlayerWin => "Player wins.",
      OutcomeType.DealerWin => "Dealer wins.",
      _ => "Push."
    };
  }

  private static string FormatCard(Card card)
  {
    return $"{card.Rank} of {card.Suit}";
  }

  private static string ResolveRoundStateText(RoundState state)
  {
    if (state.IsRoundOver)
    {
      return "Round complete";
    }

    return state.IsPlayerTurn ? "Player turn" : "Dealer turn";
  }

  private void UpdateCommandStates()
  {
    HitCommand.NotifyCanExecuteChanged();
    StandCommand.NotifyCanExecuteChanged();
    FinishRoundCommand.NotifyCanExecuteChanged();
  }
}
