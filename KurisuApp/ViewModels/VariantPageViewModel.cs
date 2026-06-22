using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KurisuApp.Data;
using KurisuApp.Notification;
using KurisuCore.APIClient;
using KurisuCore.BackblazeClient;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KurisuApp.ViewModels
{
    public partial class VariantPageViewModel : PageViewModel
    {
        private readonly KurisuAPIClient _client;
        private readonly NotificationService _notification;
        private readonly BackblazeClient _backblaze;
        private static readonly HttpClient _http = new();

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDisplayImage))]
        [NotifyPropertyChangedFor(nameof(CannotDisplayImage))]
        private Bitmap? _variantImage;

        public ObservableCollection<KCharacter> SearchResults { get; } = new();
        public ObservableCollection<KVariant> Variants { get; } = new();
        public ObservableCollection<KAnime> Animes { get; } = new();
        public IEnumerable<KRarity> Rarities => Enum.GetValues<KRarity>();

        public string ImageStatusText { get; private set; } = string.Empty;

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
        public bool CanDisplayImage => VariantImage is not null;
        public bool CannotDisplayImage => !CanDisplayImage;

        public VariantPageViewModel() : base(ApplicationPageNames.Variant) 
        {
            _backblaze = new();
            _notification = new();
        }

        public VariantPageViewModel(KurisuAPIClient client, NotificationService notification, BackblazeClient backblaze) : base(ApplicationPageNames.Variant)
        {
            _client = client;
            _notification = notification;
            _backblaze = backblaze;
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
                _ = LoadVariantImageAsync();
            }
            else
            {
                SelectedAnime = Animes.Count > 0 ? Animes[0] : null;
                SelectedRarity = KRarity.R;
                TitleInput = string.Empty;
                VariantImage = null;
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

        public async Task UploadVariantImageAsync(Stream stream, string contentType)
        {
            if (SelectedVariant is null) return;

            try
            {
                await _backblaze.UploadAsync(SelectedVariant.ImageFileName, stream, contentType);
                await LoadVariantImageAsync();
                _notification.ShowSuccess("Successfully uploaded image.");
            }
            catch 
            {
                _notification.ShowFail("Failed to upload image.");
            }
        }

        public void NotifyImageError(string message)
            => _notification.ShowFail(message);

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
            catch
            {
                _notification.ShowFail("Failed to load variants for character.");
            }
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
            catch
            {
                _notification.ShowFail("Failed to load linked animes for character.");
            }
        }

        private async Task LoadVariantImageAsync()
        {
            if (SelectedVariant is null) return;

            ImageStatusText = "Loading Image...";
            VariantImage = null;

            try
            {
                string url = _backblaze.GetUrl(SelectedVariant.ImageFileName);
                HttpResponseMessage response = await _http.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ImageStatusText = "No Image Uploaded";
                    VariantImage = null;
                    return;
                }

                response.EnsureSuccessStatusCode();

                using Stream stream = await response.Content.ReadAsStreamAsync();
                using MemoryStream buffer = new();
                await stream.CopyToAsync(buffer);
                buffer.Position = 0;
                VariantImage = new Bitmap(buffer);
            }
            catch (HttpRequestException ex)
            {
                ImageStatusText = "Network Error";
                VariantImage = null;
                _notification.ShowFail($"Failed to load image: {ex.Message}.");
            }
            catch (Exception ex)
            {
                ImageStatusText = "Unable to Display Image";
                VariantImage = null;
                _notification.ShowFail($"Failed to load image: {ex.Message}.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddVariantAsync()
        {
            if (SelectedCharacter is null || SelectedAnime is null) return;

            try
            {
                Result<KVariant> result = await _client.Variant.AddAsync(
                    SelectedCharacter.Id,
                    SelectedAnime.Id,
                    SelectedRarity,
                    TitleInput
                );

                if (result.Success)
                {
                    Variants.Add(result.Value);
                    SelectedAnime = Animes.Count > 0 ? Animes[0] : null;
                    SelectedRarity = KRarity.R;
                    TitleInput = string.Empty;
                    _notification.ShowSuccess($"Successfully added {result.Value}");
                }
                else
                {
                    _notification.ShowFail($"Failed to add variant: {result.Error}");
                }
            }
            catch
            {
                _notification.ShowFail("Failed to add variant. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyChangesAsync()
        {
            if (SelectedVariant is null || SelectedAnime is null) return;

            try
            {
                if (!SelectedVariant.Anime.Equals(SelectedAnime))
                {
                    KResult result = await _client.Variant.SetAnimeAsync(SelectedVariant.Id, SelectedAnime);

                    if (result == KResult.Completed) _notification.ShowSuccess("Successfully changed anime.");
                    else _notification.ShowFail($"Failed to change anime: {result}.");
                }

                if (SelectedVariant.Rarity != SelectedRarity)
                {
                    KResult result = await _client.Variant.SetRarityAsync(SelectedVariant.Id, SelectedRarity);

                    if (result == KResult.Completed) _notification.ShowSuccess("Successfully changed rarity.");
                    else _notification.ShowFail($"Failed to change rarity: {result}.");
                }

                if (!SelectedVariant.Title.Equals(TitleInput))
                {
                    KResult result = await _client.Variant.SetTitleAsync(SelectedVariant.Id, TitleInput);

                    if (result == KResult.Completed) _notification.ShowSuccess("Successfully changed title.");
                    else _notification.ShowFail($"Failed to change title: {result}.");
                }
            }
            catch
            {
                _notification.ShowFail("Failed to apply changes to variant. Check your connection and try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task DeleteVariantAsync()
        {
            if (SelectedVariant is null) return;

            try
            {
                KResult result = await _client.Variant.DeleteAsync(SelectedVariant.Id);

                if (result == KResult.Completed)
                {
                    Variants.Remove(SelectedVariant);
                    _notification.ShowSuccess($"Successfully deleted {SelectedVariant}.");
                    SelectedVariant = null;
                }
                else
                {
                    _notification.ShowFail($"Failed to delete {SelectedVariant}: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail($"Failed to delete {SelectedVariant}. Check your connection and try again.");
            }
        }

        [RelayCommand]
        private void EditVariantFromList(KVariant variant)
            => SelectedVariant = variant;

        [RelayCommand]
        private async Task DeleteVariantFromListAsync(KVariant variant)
        {
            if (SelectedCharacter is null) return;

            try
            {
                KResult result = await _client.Variant.DeleteAsync(variant.Id);
                
                if (result == KResult.Completed)
                {
                    Variants.Remove(variant);

                    if (SelectedVariant == variant) SelectedVariant = null;

                    _notification.ShowSuccess($"Successfully deleted {variant}.");
                }
                else
                {
                    _notification.ShowFail($"Failed to delete {variant}: {result}.");
                }
            }
            catch 
            {
                _notification.ShowFail($"Failed to delete {variant}. Check your connection and try again.");
            }
        }

        [RelayCommand]
        private void DeselectVariant()
            => SelectedVariant = null;
    }
}
