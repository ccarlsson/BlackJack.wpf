using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BlackJack.Application;
using BlackJack.Domain;
using BlackJack.Infrastructure;
using BlackJack.Presentation.Services;
using BlackJack.Presentation.ViewModels;

namespace BlackJack.Presentation;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
  private ServiceProvider? _serviceProvider;

  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    var services = new ServiceCollection();

    services.AddSingleton(GameSettings.Default);
    services.AddSingleton<IGameService, GameService>();
    services.AddSingleton<IRandomProvider, RandomProvider>();
    services.AddSingleton<IGameSession, GameSession>();
    services.AddSingleton<IGameSettingsProvider, GameSettingsProvider>();
    services.AddSingleton<IExitService, ExitService>();
    services.AddSingleton<MainViewModel>();
    services.AddSingleton<MainWindow>();

    _serviceProvider = services.BuildServiceProvider();

    var window = _serviceProvider.GetRequiredService<MainWindow>();
    window.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
    window.Show();
  }

  protected override void OnExit(ExitEventArgs e)
  {
    _serviceProvider?.Dispose();
    base.OnExit(e);
  }
}

