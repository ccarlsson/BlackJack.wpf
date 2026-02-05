using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
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
  private RoundResult? _lastResult;
  private decimal _currentBet;

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
    Bankroll = _settings.StartingBalance;
    BetText = _settings.MinBet.ToString("0", CultureInfo.CurrentCulture);
    _currentBet = _settings.MinBet;
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
  private decimal _bankroll;

  [ObservableProperty]
  private string _betText = "";

  [ObservableProperty]
  private string _betError = "";

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
    if (!TryGetValidBet(out var bet))
    {
      return;
    }

    if (bet > Bankroll)
    {
      BetError = "Bet exceeds available balance.";
      return;
    }

    var settings = new GameSettings(
      DeckCount,
      StandOnSoft17,
      _settings.MaxHands,
      _settings.AllowTenValueSplit,
      _settings.AllowResplitAces,
      _settings.RestrictSplitAcesToOneCard,
      _settings.AllowDoubleDownAfterSplitAces,
      _settings.MinBet,
      _settings.MaxBet,
      _settings.StartingBalance);
    _lastResult = null;
    Bankroll -= bet;
    _roundState = _gameService.StartNewRound(settings, _randomProvider, PlayerName, bet);
    if (_roundState.IsRoundOver)
    {
      _lastResult = _gameService.FinishRound(_roundState);
      ApplyPayout(_lastResult, _roundState);
    }
    UpdateFromState();
    StatusText = _roundState.IsRoundOver && _lastResult is not null
      ? BuildRoundSummary(_lastResult)
      : "New round started.";
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
    _lastResult = result;
    ApplyPayout(result, _roundState);
    UpdateFromState();
    StatusText = BuildRoundSummary(result);
  }

  [RelayCommand(CanExecute = nameof(CanDoubleDown))]
  private void DoubleDown()
  {
    if (_roundState is null)
    {
      return;
    }

    if (Bankroll < _roundState.BaseBet)
    {
      StatusText = "Not enough balance to double down.";
      return;
    }

    _roundState = _gameService.PlayerDoubleDown(_roundState);
    Bankroll -= _roundState.BaseBet;
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

    if (Bankroll < _roundState.BaseBet)
    {
      StatusText = "Not enough balance to split.";
      return;
    }

    _roundState = _gameService.PlayerSplit(_roundState);
    Bankroll -= _roundState.BaseBet;
    UpdateFromState();
    StatusText = "Split completed.";
  }

  [RelayCommand]
  private void Exit()
  {
    var result = MessageBox.Show(
      "Do you want to exit the game?",
      "Exit",
      MessageBoxButton.YesNo,
      MessageBoxImage.Question);

    if (result == MessageBoxResult.Yes)
    {
      System.Windows.Application.Current?.MainWindow?.Close();
    }
  }

  private bool CanHit() => _roundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanStand() => _roundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanFinishRound() => _roundState is not null && IsRoundActive && !IsPlayerTurn;

  private bool CanDoubleDown() => _roundState is not null && IsRoundActive && IsPlayerTurn && IsDoubleDownAvailable && Bankroll >= _roundState.BaseBet;

  private bool CanSplit() => _roundState is not null && IsRoundActive && IsPlayerTurn && IsSplitAvailable && Bankroll >= _roundState.BaseBet;

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
      var (outcomeText, outcomeTone, payoutText) = ResolveOutcomeText(_roundState, _lastResult, index);
      PlayerHands.Add(new PlayerHandViewModel(index, hand.BestValue, isActive, outcomeText, outcomeTone, payoutText, cards));
    }

    DealerCards.Clear();
    foreach (var cardLabel in BuildDealerCardList(_roundState))
    {
      DealerCards.Add(cardLabel);
    }

    if (_roundState.IsRoundOver && _lastResult is not null)
    {
      StatusText = BuildRoundSummary(_lastResult);
    }

    UpdateCommandStates();
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

  private static (string Text, string Tone, string Payout) ResolveOutcomeText(
    RoundState state,
    RoundResult? result,
    int handIndex)
  {
    if (!state.IsRoundOver || result is null)
    {
      return ("", "", "");
    }

    var handResult = result.HandResults.FirstOrDefault(item => item.HandIndex == handIndex);

    if (handResult is null)
    {
      return ("", "", "");
    }

    var payoutText = handResult.PayoutMultiplier switch
    {
      > 0m => $"Payout: +{handResult.PayoutMultiplier:0.##}x",
      < 0m => $"Payout: {handResult.PayoutMultiplier:0.##}x",
      _ => "Payout: 0x"
    };

    if (handResult.PlayerBlackjack)
    {
      var blackjackTone = handResult.Outcome == OutcomeType.DealerWin ? "Lose" : "Blackjack";
      return ("Result: Blackjack", blackjackTone, payoutText);
    }

    var outcome = handResult.Outcome switch
    {
      OutcomeType.PlayerWin => ("Result: Win", "Win", payoutText),
      OutcomeType.DealerWin => ("Result: Lose", "Lose", payoutText),
      _ => ("Result: Push", "Push", payoutText)
    };

    return outcome;
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

  private bool TryGetValidBet(out decimal bet)
  {
    BetError = "";
    bet = 0m;

    if (!decimal.TryParse(BetText, NumberStyles.Number, CultureInfo.CurrentCulture, out bet))
    {
      BetError = "Enter a valid bet.";
      return false;
    }

    if (bet < _settings.MinBet || bet > _settings.MaxBet)
    {
      BetError = $"Bet must be between {_settings.MinBet:0} and {_settings.MaxBet:0}.";
      return false;
    }

    _currentBet = bet;
    return true;
  }

  private static bool ResolveSplitAvailability(RoundState state)
  {
    if (state.IsRoundOver || !state.IsPlayerTurn)
    {
      return false;
    }

    var cards = state.Player.ActiveHand.Cards;

    if (state.Player.Hands.Count >= state.MaxHands)
    {
      return false;
    }

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

  private static bool ResolveDoubleDownAvailability(RoundState state)
  {
    if (state.IsRoundOver || !state.IsPlayerTurn)
    {
      return false;
    }

    if (state.IsHandLocked(state.Player.ActiveHandIndex))
    {
      return state.AllowDoubleDownAfterSplitAces && state.Player.ActiveHand.Cards.Count == 2;
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

  partial void OnBetTextChanged(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      BetError = "";
      return;
    }

    TryGetValidBet(out _);
  }
}
