using Avalonia.Controls;
using KurisuApp.ViewModels;

namespace KurisuApp.Views;

public partial class VariantPageView : UserControl
{
    public VariantPageView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is VariantPageViewModel vm)
            {
                CharacterSearchBox.AsyncPopulator = vm.SearchCharactersAsync;
            }
        };
    }
}