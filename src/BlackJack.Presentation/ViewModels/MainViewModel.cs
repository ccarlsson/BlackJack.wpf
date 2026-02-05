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
    DeckCount = _settings.DeckCount;
    StandOnSoft17 = _settings.StandOnSoft17;
  }

  public ObservableCollection<PlayerHandViewModel> PlayerHands { get; } = new();

  public ObservableCollection<string> DealerCards { get; } = new();

  public IReadOnlyList<int> DeckCounts { get; } = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

  [ObservableProperty]
  private string _title = "Black Jack";

  [ObservableProperty]
  private string _statusText;

  [ObservableProperty]
  private string _roundStateText;

  [ObservableProperty]
  private string _playerName;

  [ObservableProperty]
  private int _deckCount;

  [ObservableProperty]
  private string _deckCountError = "";

  [ObservableProperty]
  private bool _standOnSoft17;

  [ObservableProperty]
  private int _dealerValue;

  [ObservableProperty]
  private string _dealerValueText = "";

  [ObservableProperty]
  private bool _isRoundActive;

  [ObservableProperty]
  private bool _isPlayerTurn;

  public bool IsSettingsEnabled => !IsRoundActive;

  [ObservableProperty]
  private bool _isSplitAvailable;

  [ObservableProperty]
  private bool _isDoubleDownAvailable;

  [RelayCommand]
  private void NewRound()
  {
    var settings = new GameSettings(DeckCount, StandOnSoft17);
    _roundState = _gameService.StartNewRound(settings, _randomProvider, PlayerName);
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

  [RelayCommand(CanExecute = nameof(CanDoubleDown))]
  private void DoubleDown()
  {
    if (_roundState is null)
    {
      return;
    }

    _roundState = _gameService.PlayerHit(_roundState);
    _roundState = _gameService.PlayerStand(_roundState);
    UpdateFromState();
    StatusText = "Double down resolved.";
  }

  [RelayCommand(CanExecute = nameof(CanSplit))]
  private void Split()
  {
    if (_roundState is null)
    {
      return;
    }

    _roundState = _gameService.PlayerSplit(_roundState);
    UpdateFromState();
    StatusText = "Split completed.";
  }

  private bool CanHit() => _roundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanStand() => _roundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanFinishRound() => _roundState is not null && IsRoundActive && !IsPlayerTurn;

  private bool CanDoubleDown() => _roundState is not null && IsRoundActive && IsPlayerTurn && IsDoubleDownAvailable;

  private bool CanSplit() => _roundState is not null && IsRoundActive && IsPlayerTurn && IsSplitAvailable;

  private void UpdateFromState()
  {
    if (_roundState is null)
    {
      IsRoundActive = false;
      IsPlayerTurn = false;
      DealerValue = 0;
      DealerValueText = "";
      RoundStateText = "Idle";
      IsSplitAvailable = false;
      IsDoubleDownAvailable = false;
      PlayerHands.Clear();
      DealerCards.Clear();
      UpdateCommandStates();
      return;
    }

    IsRoundActive = !_roundState.IsRoundOver;
    IsPlayerTurn = _roundState.IsPlayerTurn;
    DealerValue = ResolveDealerValue(_roundState);
    DealerValueText = ResolveDealerValueText(_roundState);
    RoundStateText = ResolveRoundStateText(_roundState);
    IsSplitAvailable = ResolveSplitAvailability(_roundState);
    IsDoubleDownAvailable = ResolveDoubleDownAvailability(_roundState);

    PlayerHands.Clear();
    for (var index = 0; index < _roundState.Player.Hands.Count; index++)
    {
      var hand = _roundState.Player.Hands[index];
      var cards = hand.Cards.Select(FormatCard).ToList();
      var isActive = index == _roundState.Player.ActiveHandIndex;
      PlayerHands.Add(new PlayerHandViewModel(index, hand.BestValue, isActive, cards));
    }

    DealerCards.Clear();
    foreach (var cardLabel in BuildDealerCardList(_roundState))
    {
      DealerCards.Add(cardLabel);
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

  private static IEnumerable<string> BuildDealerCardList(RoundState state)
  {
    var dealerCards = state.Dealer.ActiveHand.Cards;

    if (state.IsRoundOver || !state.IsPlayerTurn)
    {
      return dealerCards.Select(FormatCard);
    }

    if (dealerCards.Count == 0)
    {
      return Array.Empty<string>();
    }

    return new[] { FormatCard(dealerCards[0]), "Hidden" };
  }

  private static int ResolveDealerValue(RoundState state)
  {
    var dealerHand = state.Dealer.ActiveHand;

    if (state.IsRoundOver || !state.IsPlayerTurn)
    {
      return dealerHand.BestValue;
    }

    if (dealerHand.Cards.Count == 0)
    {
      return 0;
    }

    return dealerHand.Cards[0].BaseValue;
  }

  private static string ResolveDealerValueText(RoundState state)
  {
    return state.IsRoundOver || !state.IsPlayerTurn
      ? $"Value: {state.Dealer.ActiveHand.BestValue}"
      : "Value: Hidden";
  }

  private static string ResolveRoundStateText(RoundState state)
  {
    if (state.IsRoundOver)
    {
      return "Round complete";
    }

    return state.IsPlayerTurn ? "Player turn" : "Dealer turn";
  }

  private static bool ResolveSplitAvailability(RoundState state)
  {
    if (state.IsRoundOver || !state.IsPlayerTurn)
    {
      return false;
    }

    var cards = state.Player.ActiveHand.Cards;

    return state.Player.Hands.Count == 1 && cards.Count == 2 && cards[0].Rank == cards[1].Rank;
  }

  private static bool ResolveDoubleDownAvailability(RoundState state)
  {
    if (state.IsRoundOver || !state.IsPlayerTurn)
    {
      return false;
    }

    return state.Player.ActiveHand.Cards.Count == 2;
  }

  private void UpdateCommandStates()
  {
    HitCommand.NotifyCanExecuteChanged();
    StandCommand.NotifyCanExecuteChanged();
    FinishRoundCommand.NotifyCanExecuteChanged();
    DoubleDownCommand.NotifyCanExecuteChanged();
    SplitCommand.NotifyCanExecuteChanged();
  }

  partial void OnIsRoundActiveChanged(bool value)
  {
    OnPropertyChanged(nameof(IsSettingsEnabled));
  }

  partial void OnDeckCountChanged(int value)
  {
    var clamped = Math.Clamp(value, 1, 8);

    if (clamped != value)
    {
      DeckCount = clamped;
      DeckCountError = "Deck count must be between 1 and 8.";
      return;
    }

    DeckCountError = "";
  }
}
