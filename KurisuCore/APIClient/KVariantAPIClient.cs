using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using System.Net.Http.Json;

namespace KurisuCore.APIClient
{
    public sealed class KVariantAPIClient(HttpClient http)
    {
        public async Task<KVariant?> GetAsync(int id)
        {
            var response = await http.GetAsync($"variants/{id}");

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<KVariant>();
        }

        public async Task<Result<KVariant>> AddAsync(int characterId, int animeId, KRarity rarity, string title)
        {
            var response = await http.PostAsJsonAsync("variants", new { characterId, animeId, rarity, title });

            if (!response.IsSuccessStatusCode)
            {
                KDetail error = await response.Content.ReadFromJsonAsync<KDetail>();

                return Result<KVariant>.Fail(error);
            }

            KVariant variant = (await response.Content.ReadFromJsonAsync<KVariant>())!;

            return Result<KVariant>.Ok(variant);
        }

        public async Task<KDetail> SetAnimeAsync(int id, KAnime newAnime)
        {
            var response = await http.PatchAsJsonAsync($"variants/{id}/anime", newAnime);

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> SetRarityAsync(int id, KRarity newRarity)
        {
            var response = await http.PatchAsJsonAsync($"variants/{id}/rarity", newRarity);

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> SetTitleAsync(int id, string newTitle)
        {
            var response = await http.PatchAsJsonAsync($"variants/{id}/title", newTitle);

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> DeleteAsync(int id)
        {
            var response = await http.DeleteAsync($"variants/{id}");

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }
    }
}
