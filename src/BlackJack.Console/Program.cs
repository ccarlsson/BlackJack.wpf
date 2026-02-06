using BlackJack.Application;
using BlackJack.Domain;
using BlackJack.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BlackJack.ConsoleApp;

internal static class Program
{
  private enum ConsoleAction
  {
    NewRound,
    Hit,
    Stand,
    DoubleDown,
    Split,
    Status,
    Exit
  }

  public static void Main()
  {
    var services = new ServiceCollection();
    services.AddSingleton(GameSettings.Default);
    services.AddSingleton<IRandomProvider, RandomProvider>();
    services.AddSingleton<IGameService, GameService>();
    services.AddSingleton<IGameSession, GameSession>();

    using var provider = services.BuildServiceProvider();
    var session = provider.GetRequiredService<IGameSession>();

    while (true)
    {
      RenderSession(session);
      var action = PromptAction();

      if (action == ConsoleAction.Exit)
      {
        break;
      }

      ExecuteAction(session, action);
    }

    AnsiConsole.MarkupLine("[grey]Thanks for playing![/]");
  }

  private static ConsoleAction PromptAction()
  {
    var prompt = new SelectionPrompt<ConsoleAction>()
      .Title("Select an action")
      .PageSize(10)
      .AddChoices(new[]
      {
        ConsoleAction.NewRound,
        ConsoleAction.Hit,
        ConsoleAction.Stand,
        ConsoleAction.DoubleDown,
        ConsoleAction.Split,
        ConsoleAction.Status,
        ConsoleAction.Exit
      })
      .UseConverter(FormatActionLabel);

    return AnsiConsole.Prompt(prompt);
  }

  private static string FormatActionLabel(ConsoleAction action) => action switch
  {
    ConsoleAction.NewRound => "New round",
    ConsoleAction.DoubleDown => "Double down",
    ConsoleAction.Status => "Show status",
    _ => action.ToString()
  };

  private static void ExecuteAction(IGameSession session, ConsoleAction action)
  {
    try
    {
      switch (action)
      {
        case ConsoleAction.NewRound:
          StartNewRound(session);
          break;
        case ConsoleAction.Hit:
          if (EnsureActiveTurn(session, requiresUnlockedHand: true))
          {
            session.Hit();
          }
          break;
        case ConsoleAction.Stand:
          if (EnsureActiveTurn(session, requiresUnlockedHand: false))
          {
            session.Stand();
          }
          break;
        case ConsoleAction.DoubleDown:
          HandleDoubleDown(session);
          break;
        case ConsoleAction.Split:
          HandleSplit(session);
          break;
        case ConsoleAction.Status:
          Pause("Status refreshed.");
          break;
        case ConsoleAction.Exit:
          break;
        default:
          Pause("Unknown action.");
          break;
      }
    }
    catch (Exception ex)
    {
      Pause($"Action failed: {ex.Message}", isError: true);
    }
  }

  private static void StartNewRound(IGameSession session)
  {
    var namePrompt = new TextPrompt<string>("Player name:")
      .AllowEmpty()
      .Validate(name =>
        string.IsNullOrWhiteSpace(name)
          ? ValidationResult.Error("Name is required.")
          : ValidationResult.Success());

    var playerName = AnsiConsole.Prompt(namePrompt).Trim();

    var betPrompt = new TextPrompt<decimal>($"Bet ({session.MinBet:0}-{session.MaxBet:0}):")
      .ValidationErrorMessage("Enter a valid bet.")
      .Validate(bet =>
      {
        if (bet < session.MinBet || bet > session.MaxBet)
        {
          return ValidationResult.Error($"Bet must be between {session.MinBet:0} and {session.MaxBet:0}.");
        }

        if (bet > session.Bankroll)
        {
          return ValidationResult.Error("Bet exceeds available balance.");
        }

        return ValidationResult.Success();
      });

    var bet = AnsiConsole.Prompt(betPrompt);

    if (!session.TryStartRound(playerName, bet, out var error))
    {
      Pause(error, isError: true);
      return;
    }
  }

  private static void HandleDoubleDown(IGameSession session)
  {
    if (!EnsureActiveTurn(session, requiresUnlockedHand: true))
    {
      return;
    }

    if (!session.CanDoubleDown)
    {
      Pause("Double down is not available right now.");
      return;
    }

    if (!session.TryDoubleDown(out var error))
    {
      Pause(error, isError: true);
    }
  }

  private static void HandleSplit(IGameSession session)
  {
    if (!EnsureActiveTurn(session, requiresUnlockedHand: true))
    {
      return;
    }

    if (!session.CanSplit)
    {
      Pause("Split is not available right now.");
      return;
    }

    if (!session.TrySplit(out var error))
    {
      Pause(error, isError: true);
    }
  }

  private static bool EnsureActiveTurn(IGameSession session, bool requiresUnlockedHand)
  {
    if (session.RoundState is null)
    {
      Pause("No active round.", isError: true);
      return false;
    }

    var state = session.RoundState;

    if (state.IsRoundOver)
    {
      Pause("Round is already over.");
      return false;
    }

    if (!state.IsPlayerTurn)
    {
      Pause("It is not your turn.");
      return false;
    }

    if (requiresUnlockedHand && state.IsHandLocked(state.Player.ActiveHandIndex))
    {
      Pause("Active hand is locked.");
      return false;
    }

    return true;
  }

  private static void RenderSession(IGameSession session)
  {
    AnsiConsole.Clear();
    AnsiConsole.Write(new Rule("[yellow]BlackJack Console[/]").Centered());
    AnsiConsole.MarkupLine($"[bold]Bankroll:[/] {session.Bankroll:0}");
    AnsiConsole.MarkupLine($"[bold]Bet range:[/] {session.MinBet:0}-{session.MaxBet:0}");
    AnsiConsole.MarkupLine($"[bold]Status:[/] {Markup.Escape(session.Status)}");
    AnsiConsole.WriteLine();

    if (session.RoundState is null)
    {
      AnsiConsole.MarkupLine("[grey]No active round.[/]");
      return;
    }

    var state = session.RoundState;

    RenderDealer(session, state);
    RenderHands(session, state);

    if (session.LastResult is not null)
    {
      RenderResultSummary(session.LastResult);
    }
  }

  private static void RenderDealer(IGameSession session, RoundState state)
  {
    var visibleCards = session.GetDealerVisibleCards();
    var dealerCards = visibleCards.Select(FormatCard).ToList();

    if (session.IsDealerHoleCardHidden && state.Dealer.ActiveHand.Cards.Count > visibleCards.Count)
    {
      dealerCards.Add("??");
    }

    var dealerValue = session.IsDealerHoleCardHidden
      ? session.GetDealerVisibleValue()
      : state.Dealer.ActiveHand.BestValue;

    var dealerText = $"Cards: {string.Join(" ", dealerCards)}\nValue: {dealerValue}";
    AnsiConsole.Write(new Panel(dealerText).Header("Dealer").Border(BoxBorder.Rounded));
    AnsiConsole.WriteLine();
  }

  private static void RenderHands(IGameSession session, RoundState state)
  {
    var table = new Table().Border(TableBorder.Rounded);
    table.AddColumn("Hand");
    table.AddColumn("Cards");
    table.AddColumn("Totals");
    table.AddColumn("Bet");
    table.AddColumn("State");
    table.AddColumn("Outcome");

    for (var index = 0; index < state.Player.Hands.Count; index++)
    {
      var hand = state.Player.Hands[index];
      var cards = hand.Cards.Count == 0
        ? "-"
        : string.Join(" ", hand.Cards.Select(FormatCard));

      var totals = string.Join("/", hand.GetTotals().Distinct());
      var bet = state.HandBets.Count > index ? state.HandBets[index].ToString("0") : "-";
      var stateLabel = BuildHandStateLabel(state, hand, index);
      var outcomeLabel = BuildOutcomeLabel(session.LastResult, index);

      table.AddRow(
        $"{index + 1}",
        cards,
        totals,
        bet,
        stateLabel,
        outcomeLabel);
    }

    var header = $"Player: {Markup.Escape(state.Player.Name)}";
    AnsiConsole.Write(new Panel(table).Header(header).Border(BoxBorder.Rounded));
    AnsiConsole.WriteLine();
  }

  private static string BuildHandStateLabel(RoundState state, Hand hand, int index)
  {
    var labels = new List<string>();

    if (!state.IsRoundOver && state.IsPlayerTurn && state.Player.ActiveHandIndex == index)
    {
      labels.Add("Active");
    }

    if (state.IsHandLocked(index))
    {
      labels.Add("Locked");
    }

    if (hand.IsBlackjack)
    {
      labels.Add("Blackjack");
    }

    if (hand.IsBust)
    {
      labels.Add("Bust");
    }

    if (hand.IsSoft && !hand.IsBust)
    {
      labels.Add("Soft");
    }

    if (labels.Count == 0)
    {
      labels.Add("Ready");
    }

    return string.Join(", ", labels);
  }

  private static string BuildOutcomeLabel(RoundResult? result, int index)
  {
    if (result is null)
    {
      return "-";
    }

    var handResult = result.HandResults.FirstOrDefault(hand => hand.HandIndex == index);

    if (handResult is null)
    {
      return "-";
    }

    var outcome = handResult.Outcome switch
    {
      OutcomeType.PlayerWin => "Win",
      OutcomeType.DealerWin => "Loss",
      OutcomeType.Push => "Push",
      _ => handResult.Outcome.ToString()
    };

    var multiplier = handResult.PayoutMultiplier.ToString("+0.##;-0.##;0");
    return $"{outcome} ({multiplier}x)";
  }

  private static void RenderResultSummary(RoundResult result)
  {
    if (result.HandResults.Count == 0)
    {
      return;
    }

    var summary = string.Join(", ", result.HandResults.Select(hand =>
    {
      var outcome = hand.Outcome switch
      {
        OutcomeType.PlayerWin => "Win",
        OutcomeType.DealerWin => "Loss",
        OutcomeType.Push => "Push",
        _ => hand.Outcome.ToString()
      };

      return $"Hand {hand.HandIndex + 1}: {outcome}";
    }));

    AnsiConsole.MarkupLine($"[bold]Result:[/] {Markup.Escape(summary)}");
  }

  private static string FormatCard(Card card)
  {
    var rank = card.Rank switch
    {
      Rank.Ace => "A",
      Rank.King => "K",
      Rank.Queen => "Q",
      Rank.Jack => "J",
      Rank.Ten => "10",
      _ => ((int)card.Rank).ToString()
    };

    var suit = card.Suit switch
    {
      Suit.Hearts => "♥",
      Suit.Diamonds => "♦",
      Suit.Clubs => "♣",
      Suit.Spades => "♠",
      _ => "?"
    };

    return $"{rank}{suit}";
  }

  private static void Pause(string message, bool isError = false)
  {
    var style = isError ? "red" : "grey";
    AnsiConsole.MarkupLine($"[{style}]{Markup.Escape(message)}[/]");
    AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
    System.Console.ReadKey(true);
  }
}
