using BlackJack.Application;
using BlackJack.Presentation.UiServices;

namespace BlackJack.Bootstrapper.UiServices;

public sealed class GameSettingsProvider : IGameSettingsProvider
{
  public GameSettings Defaults => GameSettings.Default;
}
