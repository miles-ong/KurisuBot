using CommunityToolkit.Mvvm.ComponentModel;
using KurisuApp.Data;
using KurisuCore.APIClient;
using KurisuCore.Models;

namespace KurisuApp.ViewModels
{
    public partial class VariantPageViewModel : PageViewModel
    {
        private readonly KurisuAPIClient _client;

        [ObservableProperty]
        private KCharacter? _selectedCharacter;

        [ObservableProperty]
        private KVariant? _selectedVariant;



        public VariantPageViewModel() : base(ApplicationPageNames.Variant) { }

        public VariantPageViewModel(KurisuAPIClient client) : base(ApplicationPageNames.Variant)
        {
            _client = client;
        }
    }
}
