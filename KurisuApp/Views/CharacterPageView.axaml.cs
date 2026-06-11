using Avalonia.Controls;
using KurisuApp.ViewModels;

namespace KurisuApp.Views;

public partial class CharacterPageView : UserControl
{
    public CharacterPageView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is CharacterPageViewModel vm)
            {
                SearchBox.AsyncPopulator = vm.SearchCharactersAsync;
            }
        };
    }
}