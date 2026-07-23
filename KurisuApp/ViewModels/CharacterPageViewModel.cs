using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuApp.Data;
using KurisuApp.Notification;
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
        private readonly NotificationService _notification;

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

        public CharacterPageViewModel(KurisuAPIClient client, NotificationService notification) : base(ApplicationPageNames.Character)
        {
            _client = client;
            _notification = notification;
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
            }
            catch 
            {
                _notification.ShowFail("Failed to load linked animes for character. Check your connection and try again.");
            }
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
                    _notification.ShowSuccess($"Successfully added {result.Value}.");
                }
                else
                {
                    _notification.ShowFail($"Failed to add character: {result.Error}.");
                }
            }
            catch 
            {
                _notification.ShowFail("Failed to add character. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyChangesAsync()
        {
            if (SelectedCharacter is null) return;

            try
            {
                KDetail result = await _client.Character.SetNameAsync(SelectedCharacter.Id, NameInput);

                if (result == KDetail.Completed) _notification.ShowSuccess("Successfully changed name.");
                else _notification.ShowFail($"Failed to change name: {result}");
            }
            catch 
            {
                _notification.ShowFail("Failed to apply changes to character. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanLink))]
        private async Task LinkAnimeAsync()
        {
            if (SelectedCharacter is null || SelectedAnime is null) return;

            try
            {
                KDetail result = await _client.CALink.LinkAsync(SelectedCharacter.Id, SelectedAnime.Id);

                if (result == KDetail.Completed)
                {
                    if (!LinkedAnimes.Contains(SelectedAnime)) LinkedAnimes.Add(SelectedAnime);

                    SelectedAnime = null;
                    AnimeSearchInput = string.Empty;
                    OnPropertyChanged(nameof(IsLinkMode));
                    OnPropertyChanged(nameof(IsUnlinkMode));
                    _notification.ShowSuccess($"Successfully linked {SelectedAnime} to {SelectedCharacter}.");
                }
                else
                {
                    _notification.ShowFail($"Failed to link {SelectedAnime} to {SelectedCharacter}: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail($"Failed to link {SelectedAnime} to {SelectedCharacter}. Check your connection and try again.");
            }
        }

        [RelayCommand]
        private async Task UnlinkAnimeAsync(KAnime anime)
        {
            if (SelectedCharacter is null) return;

            try
            {
                KDetail result = await _client.CALink.UnlinkAsync(SelectedCharacter.Id, anime.Id);

                if (result == KDetail.Completed)
                {
                    LinkedAnimes.Remove(anime);
                    OnPropertyChanged(nameof(IsLinkMode));
                    OnPropertyChanged(nameof(IsUnlinkMode));
                }
                else
                {
                    _notification.ShowFail($"Failed to unlink {anime} from {SelectedCharacter}: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail($"Failed to unlink {anime} from {SelectedCharacter}. Check your connection and try again.");
            }
        }
    }
}
