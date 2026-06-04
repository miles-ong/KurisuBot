using CommunityToolkit.Mvvm.ComponentModel;
using KurisuDesktop.Data;

namespace KurisuDesktop.ViewModels
{
    public partial class PageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ApplicationPageNames _pageName;
    }
}
