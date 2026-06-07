using CommunityToolkit.Mvvm.ComponentModel;
using KurisuApp.Data;

namespace KurisuApp.ViewModels
{
    public partial class PageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ApplicationPageNames _pageName;

        protected PageViewModel(ApplicationPageNames pageName)
        {
            _pageName = pageName;
        }
    }
}
