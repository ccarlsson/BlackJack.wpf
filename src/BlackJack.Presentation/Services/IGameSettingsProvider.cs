using BlackJack.Application;

namespace BlackJack.Presentation.Services;

public interface IGameSettingsProvider
{
  GameSettings Defaults { get; }
}
