using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BlackJack.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _titel = "Black Jack";

    [ObservableProperty]
    private string _statusText = "Välj 'Ny runda' för att starta.";

    [RelayCommand]
    private void NyRunda()
    {
           StatusText = "Ny runda initierad (platshållare).";
    }
}
