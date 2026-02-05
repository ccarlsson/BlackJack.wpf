using BlackJack.Application;

namespace BlackJack.Presentation.Services;

public sealed class GameSettingsProvider : IGameSettingsProvider
{
  public GameSettings Defaults => GameSettings.Default;
}
