using System.Windows;
using BlackJack.Presentation.UiServices;

namespace BlackJack.Bootstrapper.UiServices;

public sealed class ExitService : IExitService
{
  public bool ConfirmExit()
  {
    var result = MessageBox.Show(
      "Do you want to exit the game?",
      "Exit",
      MessageBoxButton.YesNo,
      MessageBoxImage.Question);

    return result == MessageBoxResult.Yes;
  }

  public void Exit()
  {
    System.Windows.Application.Current?.MainWindow?.Close();
  }
}
