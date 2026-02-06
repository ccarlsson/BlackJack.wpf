using BlackJack.Application;

namespace BlackJack.Presentation.UiServices;

public interface IGameSettingsProvider
{
  GameSettings Defaults { get; }
}
