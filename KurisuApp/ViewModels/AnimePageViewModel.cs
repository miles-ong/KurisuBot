using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuApp.Data;
using KurisuCore.APIClient;
using KurisuCore.Models;
using KurisuCore.Result;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace KurisuApp.ViewModels
{
    public partial class AnimePageViewModel : PageViewModel
    {
        private readonly KAnimeAPIClient _client;

        [ObservableProperty]
        private KAnime? _selectedAnime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddAnimeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyChangesCommand))]
        private string _titleInput = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddAnimeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyChangesCommand))]
        private string _descriptionInput = string.Empty;

        public bool IsAddMode => SelectedAnime is null;
        public bool IsEditMode => SelectedAnime is not null;

        public bool CanAdd => SelectedAnime is null
            && !string.IsNullOrWhiteSpace(TitleInput)
            && !string.IsNullOrWhiteSpace(DescriptionInput);

        public bool CanApply => SelectedAnime is not null
            && !string.IsNullOrWhiteSpace(TitleInput) 
            && !string.IsNullOrWhiteSpace(DescriptionInput)
            && (TitleInput != SelectedAnime.Title || DescriptionInput != SelectedAnime.Description);

        public ObservableCollection<KAnime> SearchResults { get; } = new();

        public AnimePageViewModel() : base(ApplicationPageNames.Anime) { }

        public AnimePageViewModel(KAnimeAPIClient client) : base(ApplicationPageNames.Anime)
        {
            _client = client;
        }

        partial void OnSelectedAnimeChanged(KAnime? value)
        {
            if (value is not null)
            {
                TitleInput = value.Title;
                DescriptionInput = value.Description;
            }
            else
            {
                TitleInput = string.Empty;
                DescriptionInput = string.Empty;
            }

            OnPropertyChanged(nameof(IsAddMode));
            OnPropertyChanged(nameof(IsEditMode));
        }

        public async Task<IEnumerable<object>> SearchAnimesAsync(string query, CancellationToken cts)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            try
            {
                return await _client.SearchAsync(query);
            }
            catch
            {
                return [];
            }
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAnimeAsync()
        {
            try
            {
                Result<KAnime> result = await _client.AddAsync(TitleInput, DescriptionInput);

                if (result.Success)
                {
                    TitleInput = string.Empty;
                    DescriptionInput = string.Empty;
                }
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyChangesAsync()
        {
            if (SelectedAnime is null) return;

            try
            {
                await _client.SetTitleAsync(SelectedAnime.Id, TitleInput);
                await _client.SetDescriptionAsync(SelectedAnime.Id, DescriptionInput);
            }
            catch { }
        }
    }
}
