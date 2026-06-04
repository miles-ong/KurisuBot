using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuDesktop.Data;
using KurisuDesktop.Factories;

namespace KurisuDesktop.ViewModels
{
    public partial class MainViewModel : PageViewModel
    {
        private PageFactory _pageFactory;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnimePageIsActive))]
        [NotifyPropertyChangedFor(nameof(CharacterPageIsActive))]
        [NotifyPropertyChangedFor(nameof(VariantPageIsActive))]
        private PageViewModel _currentPage;

        public bool AnimePageIsActive => CurrentPage.PageName == ApplicationPageNames.Anime;
        public bool CharacterPageIsActive => CurrentPage.PageName == ApplicationPageNames.Character;
        public bool VariantPageIsActive => CurrentPage.PageName == ApplicationPageNames.Variant;

        public MainViewModel()
        {
            CurrentPage = new AnimePageViewModel();
        }

        public MainViewModel(PageFactory pageFactory)
        {
            _pageFactory = pageFactory;

            GoToAnime();
        }

        [RelayCommand]
        private void GoToAnime()
            => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Anime);

        [RelayCommand]
        private void GoToCharacter()
            => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Character);

        [RelayCommand]
        private void GoToVariant()
            => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Variant);
    }
}
