using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuApp.Data;
using KurisuCore.APIClient;
using KurisuCore.Models;
using KurisuCore.Result;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace KurisuApp.ViewModels
{
    public partial class CharacterPageViewModel : PageViewModel
    {
        private readonly KCharacterAPIClient _client;

        [ObservableProperty]
        private KCharacter? _selectedCharacter;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddCharacterCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyChangesCommand))]
        private string _nameInput = string.Empty;

        public bool IsAddMode => SelectedCharacter is null;
        public bool IsEditMode => SelectedCharacter is not null;

        public bool CanAdd => SelectedCharacter is null
            && !string.IsNullOrWhiteSpace(NameInput);

        public bool CanApply => SelectedCharacter is not null
            && !string.IsNullOrWhiteSpace(NameInput)
            && NameInput != SelectedCharacter.Name;

        public ObservableCollection<KCharacter> SearchResults { get; } = new();

        public CharacterPageViewModel() : base(ApplicationPageNames.Character) { }

        public CharacterPageViewModel(KCharacterAPIClient client) : base(ApplicationPageNames.Character)
        {
            _client = client;
        }

        partial void OnSelectedCharacterChanged(KCharacter? value)
        {
            if (value is not null) NameInput = value.Name;
            else NameInput = string.Empty;

            OnPropertyChanged(nameof(IsAddMode));
            OnPropertyChanged(nameof(IsEditMode));
        }

        public async Task<IEnumerable<object>> SearchCharactersAsync(string query, CancellationToken cts)
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
        private async Task AddCharacterAsync()
        {
            try
            {
                Result<KCharacter> result = await _client.AddAsync(NameInput);

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
                await _client.SetNameAsync(SelectedCharacter.Id, NameInput);
            }
            catch { }
        }
    }
}
