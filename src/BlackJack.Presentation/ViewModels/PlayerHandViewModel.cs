using System.Collections.ObjectModel;

namespace BlackJack.Presentation.ViewModels;

public sealed class PlayerHandViewModel
{
  public PlayerHandViewModel(
    int index,
    int value,
    bool isActive,
    string outcomeText,
    string outcomeTone,
    string payoutText,
    IEnumerable<string> cards)
  {
    Title = $"Hand {index + 1}";
    ValueText = $"Value: {value}";
    IsActive = isActive;
    OutcomeText = outcomeText;
    OutcomeTone = outcomeTone;
    PayoutText = payoutText;
    Cards = new ObservableCollection<string>(cards);
  }

  public string Title { get; }

  public string ValueText { get; }

  public bool IsActive { get; }

  public string OutcomeText { get; }

  public string OutcomeTone { get; }

  public string PayoutText { get; }

  public ObservableCollection<string> Cards { get; }
}
