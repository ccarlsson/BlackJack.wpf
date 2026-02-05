using System.Collections.ObjectModel;

namespace BlackJack.Presentation.ViewModels;

public sealed class PlayerHandViewModel
{
  public PlayerHandViewModel(int index, int value, bool isActive, IEnumerable<string> cards)
  {
    Title = $"Hand {index + 1}";
    ValueText = $"Value: {value}";
    IsActive = isActive;
    Cards = new ObservableCollection<string>(cards);
  }

  public string Title { get; }

  public string ValueText { get; }

  public bool IsActive { get; }

  public ObservableCollection<string> Cards { get; }
}
