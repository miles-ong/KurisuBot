using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuApp.Data;
using KurisuCore.APIClient;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace KurisuApp.ViewModels
{
    public partial class VariantPageViewModel : PageViewModel
    {
        private readonly KurisuAPIClient _client;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddVariantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVariantCommand))]
        private KCharacter? _selectedCharacter;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddVariantCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyChangesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVariantCommand))]
        private KVariant? _selectedVariant;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddVariantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVariantCommand))]
        private KAnime? _selectedAnime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddVariantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVariantCommand))]
        private KRarity _selectedRarity;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddMode))]
        [NotifyPropertyChangedFor(nameof(IsEditMode))]
        [NotifyCanExecuteChangedFor(nameof(AddVariantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVariantCommand))]
        private string _titleInput;

        public ObservableCollection<KCharacter> SearchResults { get; } = new();
        public ObservableCollection<KVariant> Variants { get; } = new();
        public ObservableCollection<KAnime> Animes { get; } = new();
        public IEnumerable<KRarity> Rarities => Enum.GetValues<KRarity>();

        public bool IsAddMode => SelectedVariant is null;
        public bool IsEditMode => SelectedVariant is not null;
        public bool CanAdd => SelectedCharacter is not null
            && SelectedVariant is null
            && SelectedAnime is not null
            && !string.IsNullOrWhiteSpace(TitleInput);
        public bool CanApply => SelectedCharacter is not null
            && SelectedVariant is not null
            && SelectedAnime is not null
            && !string.IsNullOrWhiteSpace(TitleInput)
            && (SelectedAnime != SelectedVariant.Anime || SelectedRarity != SelectedVariant.Rarity || TitleInput != SelectedVariant.Title);
        public bool CanDelete => SelectedCharacter is not null
            && SelectedVariant is not null;


        public VariantPageViewModel() : base(ApplicationPageNames.Variant) { }

        public VariantPageViewModel(KurisuAPIClient client) : base(ApplicationPageNames.Variant)
        {
            _client = client;
        }

        partial void OnSelectedCharacterChanged(KCharacter? value)
        {
            if (value is not null)
            {
                _ = LoadAnimesAsync();
                _ = LoadVariantsAsync();
            }
            else
            {
                Variants.Clear();
                Animes.Clear();
            }
        }

        partial void OnSelectedVariantChanged(KVariant? value)
        {
            if (value is not null)
            {
                SelectedAnime = value.Anime;
                TitleInput = value.Title;
                SelectedRarity = value.Rarity;
            }
            else
            {
                SelectedAnime = null;
                TitleInput = string.Empty;
                SelectedRarity = KRarity.R;
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

        private async Task LoadVariantsAsync()
        {
            if (SelectedCharacter is null) return;

            try
            {
                Variants.Clear();
                IEnumerable<KVariant> variants = await _client.Character.GetVariantsAsync(SelectedCharacter.Id);

                foreach(KVariant v in variants)
                {
                    Variants.Add(v);
                }
            }
            catch { }
        }

        private async Task LoadAnimesAsync()
        {
            if (SelectedCharacter is null) return;

            try
            {
                Animes.Clear();
                IEnumerable<KAnime> animes = await _client.Character.GetAnimesAsync(SelectedCharacter.Id);

                foreach(KAnime a in animes)
                {
                    Animes.Add(a);
                }

                if (Animes.Count > 0) SelectedAnime = Animes[0];
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddVariantAsync()
        {
            if (SelectedCharacter is null) return;
            if (SelectedAnime is null) return;

            try
            {
                Result<KVariant> result = await _client.Variant.AddAsync(
                    SelectedCharacter.Id,
                    SelectedAnime.Id,
                    SelectedRarity,
                    TitleInput
                );

                if (result.Success) Variants.Add(result.Value);
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyChangesAsync()
        {
            if (SelectedVariant is null) return;

            try
            {
                if (SelectedVariant.Anime != SelectedAnime) await _client.Variant.SetAnimeAsync(SelectedVariant.Id, SelectedAnime!);
                if (SelectedVariant.Rarity != SelectedRarity) await _client.Variant.SetRarityAsync(SelectedVariant.Id, SelectedRarity);
                if (!SelectedVariant.Title.Equals(TitleInput)) await _client.Variant.SetTitleAsync(SelectedVariant.Id, TitleInput);
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task DeleteVariantAsync()
        {
            if (SelectedVariant is null) return;

            try
            {
                await _client.Variant.DeleteAsync(SelectedVariant.Id);
                Variants.Remove(SelectedVariant);
                SelectedVariant = null;
            }
            catch { }
        }

        [RelayCommand]
        private void EditVariantFromList(KVariant variant)
        {
            SelectedVariant = variant;
        }

        [RelayCommand]
        private async Task DeleteVariantFromListAsync(KVariant variant)
        {
            if (SelectedCharacter is null) return;

            try
            {
                await _client.Variant.DeleteAsync(variant.Id);
                Variants.Remove(variant);

                if (SelectedVariant == variant) SelectedVariant = null;
            }
            catch { }
        }
    }
}
