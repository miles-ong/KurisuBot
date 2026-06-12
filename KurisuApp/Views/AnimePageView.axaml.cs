using Avalonia.Controls;
using KurisuApp.ViewModels;

namespace KurisuApp.Views;

public partial class AnimePageView : UserControl
{
    public AnimePageView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is AnimePageViewModel vm)
            {
                SearchBox.AsyncPopulator = vm.SearchAnimesAsync;
                CharacterSearchBox.AsyncPopulator = vm.SearchCharactersAsync;
            }
        };
    }
}