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
    public partial class AnimePageViewModel : PageViewModel
    {
        private readonly KurisuAPIClient _client;
        private readonly NotificationService _notification;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLinkMode))]
        [NotifyPropertyChangedFor(nameof(IsUnlinkMode))]
        [NotifyCanExecuteChangedFor(nameof(LinkCharacterCommand))]
        private KCharacter? _selectedCharacter;

        [ObservableProperty]
        private string _characterSearchInput = string.Empty;

        public ObservableCollection<KAnime> SearchResults { get; } = new();
        public ObservableCollection<KCharacter> CharacterSearchResults { get; } = new();
        public ObservableCollection<KCharacter> LinkedCharacters { get; } = new();

        public bool IsAddMode => SelectedAnime is null;
        public bool IsEditMode => SelectedAnime is not null;
        public bool IsLinkMode => SelectedCharacter is null
            || (SelectedCharacter is not null && !LinkedCharacters.Contains(SelectedCharacter));
        public bool IsUnlinkMode => SelectedCharacter is not null
            && LinkedCharacters.Contains(SelectedCharacter);
        public bool CanAdd => SelectedAnime is null
            && !string.IsNullOrWhiteSpace(TitleInput)
            && !string.IsNullOrWhiteSpace(DescriptionInput);
        public bool CanApply => SelectedAnime is not null
            && !string.IsNullOrWhiteSpace(TitleInput) 
            && !string.IsNullOrWhiteSpace(DescriptionInput)
            && (TitleInput != SelectedAnime.Title || DescriptionInput != SelectedAnime.Description);
        public bool CanLink => SelectedAnime is not null
            && SelectedCharacter is not null;

        public AnimePageViewModel() : base(ApplicationPageNames.Anime) { }

        public AnimePageViewModel(KurisuAPIClient client, NotificationService notification) : base(ApplicationPageNames.Anime)
        {
            _client = client;
            _notification = notification;
        }

        partial void OnSelectedAnimeChanged(KAnime? value)
        {
            if (value is not null)
            {
                TitleInput = value.Title;
                DescriptionInput = value.Description;
                _ = LoadLinkedCharactersAsync();
            }
            else
            {
                TitleInput = string.Empty;
                DescriptionInput = string.Empty;
                LinkedCharacters.Clear();
            }

            CharacterSearchInput = string.Empty;
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

        public async Task<IEnumerable<object>> SearchCharactersAsync(string query, CancellationToken cts)
        {
            if (string.IsNullOrEmpty(query)) return [];

            try
            {
                return await _client.Character.SearchAsync(query);
            }
            catch
            {
                return [];
            }
        }

        private async Task LoadLinkedCharactersAsync()
        {
            if (SelectedAnime is null) return;

            try
            {
                LinkedCharacters.Clear();
                List<KCharacter> characters = await _client.Anime.GetCharactersAsync(SelectedAnime.Id);

                foreach (KCharacter c in characters)
                {
                    LinkedCharacters.Add(c);
                }
            }
            catch 
            {
                _notification.ShowFail("Failed to load linked characters for anime. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAnimeAsync()
        {
            try
            {
                Result<KAnime> result = await _client.Anime.AddAsync(TitleInput, DescriptionInput);

                if (result.Success)
                {
                    TitleInput = string.Empty;
                    DescriptionInput = string.Empty;
                    _notification.ShowSuccess($"Successfully added {result.Value}.");
                }
                else
                {
                    _notification.ShowFail($"Failed to add anime: {result.Error}.");
                }
            }
            catch 
            {
                _notification.ShowFail("Failed to add anime. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyChangesAsync()
        {
            if (SelectedAnime is null) return;

            try
            {
                if (!SelectedAnime.Title.Equals(TitleInput))
                {
                    KDetail result = await _client.Anime.SetTitleAsync(SelectedAnime.Id, TitleInput);

                    if (result == KDetail.Completed) _notification.ShowSuccess("Successfully changed title.");
                    else _notification.ShowFail($"Failed to change title: {result}.");
                }

                if (!SelectedAnime.Description.Equals(DescriptionInput))
                {
                    KDetail result = await _client.Anime.SetDescriptionAsync(SelectedAnime.Id, DescriptionInput);

                    if (result == KDetail.Completed) _notification.ShowSuccess("Successfully changed description.");
                    else _notification.ShowFail($"Failed to change description: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail("Failed to apply changes to anime. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanLink))]
        private async Task LinkCharacterAsync()
        {
            if (SelectedAnime is null || SelectedCharacter is null) return;

            try
            {
                KDetail result = await _client.CALink.LinkAsync(SelectedCharacter.Id, SelectedAnime.Id);

                if (result == KDetail.Completed)
                {
                    if (!LinkedCharacters.Contains(SelectedCharacter)) LinkedCharacters.Add(SelectedCharacter);

                    SelectedCharacter = null;
                    CharacterSearchInput = string.Empty;
                    OnPropertyChanged(nameof(IsLinkMode));
                    OnPropertyChanged(nameof(IsUnlinkMode));
                    _notification.ShowSuccess($"Successfully linked {SelectedCharacter} to {SelectedAnime}.");
                }
                else
                {
                    _notification.ShowFail($"Failed to link {SelectedCharacter} to {SelectedAnime}: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail($"Failed to link {SelectedCharacter} to {SelectedAnime}. Check your connection and try again.");
            }
        }

        [RelayCommand]
        private async Task UnlinkCharacterAsync(KCharacter character)
        {
            if (SelectedAnime is null) return;

            try
            {
                KDetail result = await _client.CALink.UnlinkAsync(character.Id, SelectedAnime.Id);

                if (result == KDetail.Completed)
                {
                    LinkedCharacters.Remove(character);
                    OnPropertyChanged(nameof(IsLinkMode));
                    OnPropertyChanged(nameof(IsUnlinkMode));
                    _notification.ShowSuccess($"Successfully unlinked {character} from {SelectedAnime}.");
                }
                else
                {
                    _notification.ShowFail($"Failed to unlink {character} from {SelectedAnime}: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail($"Failed to unlink {character} from {SelectedAnime}. Check your connection and try again.");
            }
        }
    }
}
