using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BlackJack.Application;
using BlackJack.Domain;
using BlackJack.Presentation.UiServices;

namespace BlackJack.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
  private readonly IGameSession _session;
  private readonly IExitService _exitService;
  private readonly IGameSettingsProvider _settingsProvider;

  public MainViewModel(IGameSession session, IExitService exitService, IGameSettingsProvider settingsProvider)
  {
    _session = session ?? throw new ArgumentNullException(nameof(session));
    _exitService = exitService ?? throw new ArgumentNullException(nameof(exitService));
    _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));

    var defaults = _settingsProvider.Defaults;
    PlayerName = "Player";
    StatusText = _session.Status;
    RoundStateText = "Idle";
    DeckCount = defaults.DeckCount;
    StandOnSoft17 = defaults.StandOnSoft17;
    Bankroll = _session.Bankroll;
    BetText = _session.MinBet.ToString("0", CultureInfo.CurrentCulture);
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

    var defaults = _settingsProvider.Defaults;
    var settings = new GameSettings(
      DeckCount,
      StandOnSoft17,
      defaults.MaxHands,
      defaults.AllowTenValueSplit,
      defaults.AllowResplitAces,
      defaults.RestrictSplitAcesToOneCard,
      defaults.AllowDoubleDownAfterSplitAces,
      _session.MinBet,
      _session.MaxBet,
      defaults.StartingBalance);
    _session.UpdateSettings(settings);

    if (!_session.TryStartRound(PlayerName, bet, out var error))
    {
      BetError = error;
      Bankroll = _session.Bankroll;
      StatusText = _session.Status;
      return;
    }

    UpdateFromState();
  }

  [RelayCommand(CanExecute = nameof(CanHit))]
  private void Hit()
  {
    if (_session.RoundState is null)
    {
      return;
    }

    _session.Hit();
    UpdateFromState();
  }

  [RelayCommand(CanExecute = nameof(CanStand))]
  private void Stand()
  {
    if (_session.RoundState is null)
    {
      return;
    }

    _session.Stand();
    UpdateFromState();
  }


  [RelayCommand(CanExecute = nameof(CanDoubleDown))]
  private void DoubleDown()
  {
    if (_session.RoundState is null)
    {
      return;
    }

    if (!_session.TryDoubleDown(out var error))
    {
      StatusText = error;
      UpdateFromState();
      return;
    }

    UpdateFromState();
  }

  [RelayCommand(CanExecute = nameof(CanSplit))]
  private void Split()
  {
    if (_session.RoundState is null)
    {
      return;
    }

    if (!_session.TrySplit(out var error))
    {
      StatusText = error;
      UpdateFromState();
      return;
    }

    UpdateFromState();
  }

  [RelayCommand]
  private void Exit()
  {
    if (_exitService.ConfirmExit())
    {
      _exitService.Exit();
    }
  }

  private bool CanHit() => _session.RoundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanStand() => _session.RoundState is not null && IsRoundActive && IsPlayerTurn;

  private bool CanDoubleDown() => _session.CanDoubleDown;

  private bool CanSplit() => _session.CanSplit;

  private void UpdateFromState()
  {
    var roundState = _session.RoundState;
    var lastResult = _session.LastResult;

    Bankroll = _session.Bankroll;
    StatusText = _session.Status;

    if (roundState is null)
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

    IsRoundActive = !roundState.IsRoundOver;
    IsPlayerTurn = roundState.IsPlayerTurn;
    DealerValue = _session.GetDealerVisibleValue();
    DealerValueText = _session.IsDealerHoleCardHidden
      ? "Value: Hidden"
      : $"Value: {DealerValue}";
    RoundStateText = ResolveRoundStateText(roundState);
    IsSplitAvailable = _session.CanSplit;
    IsDoubleDownAvailable = _session.CanDoubleDown;

    PlayerHands.Clear();
    for (var index = 0; index < roundState.Player.Hands.Count; index++)
    {
      var hand = roundState.Player.Hands[index];
      var cards = hand.Cards.Select(FormatCard).ToList();
      var isActive = index == roundState.Player.ActiveHandIndex;
      var (outcomeText, outcomeTone, payoutText) = ResolveOutcomeText(roundState, lastResult, index);
      PlayerHands.Add(new PlayerHandViewModel(index, hand.BestValue, isActive, outcomeText, outcomeTone, payoutText, cards));
    }

    DealerCards.Clear();
    foreach (var card in _session.GetDealerVisibleCards())
    {
      DealerCards.Add(FormatCard(card));
    }

    if (_session.IsDealerHoleCardHidden)
    {
      DealerCards.Add("Hidden");
    }

    UpdateCommandStates();
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


  private bool TryGetValidBet(out decimal bet)
  {
    BetError = "";
    bet = 0m;

    if (!decimal.TryParse(BetText, NumberStyles.Number, CultureInfo.CurrentCulture, out bet))
    {
      BetError = "Enter a valid bet.";
      return false;
    }

    if (bet < _session.MinBet || bet > _session.MaxBet)
    {
      BetError = $"Bet must be between {_session.MinBet:0} and {_session.MaxBet:0}.";
      return false;
    }

    return true;
  }


  private void UpdateCommandStates()
  {
    HitCommand.NotifyCanExecuteChanged();
    StandCommand.NotifyCanExecuteChanged();
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
