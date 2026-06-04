using KurisuDesktop.Data;
using KurisuDesktop.ViewModels;
using System;

namespace KurisuDesktop.Factories
{
    public class PageFactory(Func<ApplicationPageNames, PageViewModel> factory)
    {
        public PageViewModel GetPageViewModel(ApplicationPageNames pageName)
            => factory.Invoke(pageName);
    }
}
