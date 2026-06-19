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
    public partial class CharacterPageViewModel : PageViewModel
    {
        private readonly KurisuAPIClient _client;

        [ObservableProperty]
        private KCharacter? _selectedCharacter;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLinkMode))]
        [NotifyPropertyChangedFor(nameof(IsUnlinkMode))]
        [NotifyCanExecuteChangedFor(nameof(LinkAnimeCommand))]
        private KAnime? _selectedAnime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddCharacterCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyChangesCommand))]
        private string _nameInput = string.Empty;

        [ObservableProperty]
        private string _animeSearchInput = string.Empty;

        public ObservableCollection<KCharacter> SearchResults { get; } = new();
        public ObservableCollection<KAnime> AnimeSearchResults { get; } = new();
        public ObservableCollection<KAnime> LinkedAnimes { get; } = new();

        public bool IsAddMode => SelectedCharacter is null;
        public bool IsEditMode => SelectedCharacter is not null;
        public bool IsLinkMode => SelectedAnime is null
            || (SelectedAnime is not null && !LinkedAnimes.Contains(SelectedAnime));
        public bool IsUnlinkMode => SelectedAnime is not null
            && LinkedAnimes.Contains(SelectedAnime);

        public bool CanAdd => SelectedCharacter is null
            && !string.IsNullOrWhiteSpace(NameInput);

        public bool CanApply => SelectedCharacter is not null
            && !string.IsNullOrWhiteSpace(NameInput)
            && NameInput != SelectedCharacter.Name;

        public bool CanLink => SelectedCharacter is not null
            && SelectedAnime is not null;

        public CharacterPageViewModel() : base(ApplicationPageNames.Character) { }

        public CharacterPageViewModel(KurisuAPIClient client) : base(ApplicationPageNames.Character)
        {
            _client = client;
        }

        partial void OnSelectedCharacterChanged(KCharacter? value)
        {
            if (value is not null)
            {
                NameInput = value.Name;
                _ = LoadLinkedAnimesAsync();
            }
            else
            {
                NameInput = string.Empty;
                LinkedAnimes.Clear();
            }

            AnimeSearchInput = string.Empty;
            OnPropertyChanged(nameof(IsAddMode));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(CanLink));
        }

        public async Task<IEnumerable<object>> SearchCharactersAsync(string query, CancellationToken cts)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            try
            {
                return await _client.Character.SearchAsync(query);
            }
            catch
            {
                return [];
            }
        }

        public async Task<IEnumerable<object>> SearchAnimesAsync(string query, CancellationToken cts)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            try
            {
                return await _client.Anime.SearchAsync(query);
            }
            catch
            {
                return [];
            }
        }

        private async Task LoadLinkedAnimesAsync()
        {
            if (SelectedCharacter is null) return;

            try
            {
                LinkedAnimes.Clear();
                List<KAnime> animes = await _client.Character.GetAnimesAsync(SelectedCharacter.Id);

                foreach (KAnime a in animes)
                {
                    LinkedAnimes.Add(a);
                }

                OnPropertyChanged(nameof(IsLinkMode));
                OnPropertyChanged(nameof(IsUnlinkMode));
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddCharacterAsync()
        {
            try
            {
                Result<KCharacter> result = await _client.Character.AddAsync(NameInput);

                if (result.Success)
                {
                    NameInput = string.Empty;
                }
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyChangesAsync()
        {
            if (SelectedCharacter is null) return;

            try
            {
                await _client.Character.SetNameAsync(SelectedCharacter.Id, NameInput);
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanLink))]
        private async Task LinkAnimeAsync()
        {
            if (SelectedCharacter is null || SelectedAnime is null) return;

            try
            {
                await _client.CALink.LinkAsync(SelectedCharacter.Id, SelectedAnime.Id);

                if (!LinkedAnimes.Contains(SelectedAnime)) LinkedAnimes.Add(SelectedAnime);

                SelectedAnime = null;
                AnimeSearchInput = string.Empty;
                OnPropertyChanged(nameof(IsLinkMode));
                OnPropertyChanged(nameof(IsUnlinkMode));
            }
            catch { }
        }

        [RelayCommand]
        private async Task UnlinkAnimeAsync(KAnime anime)
        {
            if (SelectedCharacter is null) return;

            try
            {
                await _client.CALink.UnlinkAsync(SelectedCharacter.Id, anime.Id);
                LinkedAnimes.Remove(anime);
                OnPropertyChanged(nameof(IsLinkMode));
                OnPropertyChanged(nameof(IsUnlinkMode));
            }
            catch { }
        }
    }
}
