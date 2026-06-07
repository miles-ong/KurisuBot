using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuApp.Data;
using KurisuApp.Factories;
using KurisuCore.APIClient;
using System.Threading;
using System.Threading.Tasks;

namespace KurisuApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    private readonly KurisuAPIClient _apiClient;
    private CancellationTokenSource _healthCts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnimePageIsActive))]
    [NotifyPropertyChangedFor(nameof(CharacterPageIsActive))]
    [NotifyPropertyChangedFor(nameof(VariantPageIsActive))]
    private PageViewModel _currentPage;

    public bool AnimePageIsActive => CurrentPage.PageName == ApplicationPageNames.Anime;
    public bool CharacterPageIsActive => CurrentPage.PageName == ApplicationPageNames.Character;
    public bool VariantPageIsActive => CurrentPage.PageName == ApplicationPageNames.Variant;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApiStatus))]
    private bool _apiOnline;

    public string ApiStatus => ApiOnline ? "API Status: Online" : "API Status: Offline";

    public MainViewModel()
    {
        CurrentPage = new AnimePageViewModel();
    }

    public MainViewModel(PageFactory pageFactory, KurisuAPIClient apiClient)
    {
        _pageFactory = pageFactory;
        _apiClient = apiClient;
        GoToAnime();
        _ = StartHealthPollAsync();
    }

    private async Task StartHealthPollAsync()
    {
        var token = _healthCts.Token;

        while (!token.IsCancellationRequested)
        {
            await CheckHealthAsync();
            await Task.Delay(15000, token).ContinueWith(_ => { });
        }
    }

    private async Task CheckHealthAsync()
    {
        try
        {
            ApiOnline = await _apiClient.CheckHealthAsync();
        }
        catch
        {
            ApiOnline = false;
        }
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