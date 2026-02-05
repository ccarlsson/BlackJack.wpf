using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BlackJack.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
  [ObservableProperty]
  private string _title = "Black Jack";

  [ObservableProperty]
  private string _statusText = "Select 'New round' to start.";

  [RelayCommand]
  private void NewRound()
  {
    StatusText = "New round initialized (placeholder).";
  }
}
