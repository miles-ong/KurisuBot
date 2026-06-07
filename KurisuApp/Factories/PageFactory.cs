using KurisuApp.Data;
using KurisuApp.ViewModels;
using System;

namespace KurisuApp.Factories
{
    public class PageFactory(Func<ApplicationPageNames, PageViewModel> factory)
    {
        public PageViewModel GetPageViewModel(ApplicationPageNames pageName)
            => factory.Invoke(pageName);
    }
}
