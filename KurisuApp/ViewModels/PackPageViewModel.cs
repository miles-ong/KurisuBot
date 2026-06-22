using KurisuApp.Data;
using KurisuApp.Notification;
using KurisuCore.APIClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KurisuApp.ViewModels
{
    public partial class PackPageViewModel : PageViewModel
    {
        private readonly KurisuAPIClient _client;
        private readonly NotificationService _notification;

        public PackPageViewModel() : base(ApplicationPageNames.Pack) { }

        public PackPageViewModel(KurisuAPIClient client, NotificationService notification) : base(ApplicationPageNames.Pack)
        {
            _client = client;
            _notification = notification;
        }
    }
}
