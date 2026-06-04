using CommunityToolkit.Mvvm.ComponentModel;

namespace KurisuDesktop.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _test = "test";
    }
}
