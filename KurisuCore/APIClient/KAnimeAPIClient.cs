using KurisuCore.Models;
using KurisuCore.Result;
using System.Net.Http.Json;

namespace KurisuCore.APIClient
{
    public sealed class KAnimeAPIClient(HttpClient http)
    {
        public async Task<KAnime?> GetAsync(int id)
        {
            var response = await http.GetAsync($"anime/{id}");

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<KAnime>();
        }

        public async Task<List<KCharacter>> GetCharactersAsync(int id)
        {
            var response = await http.GetAsync($"anime/{id}/characters");

            if (!response.IsSuccessStatusCode) return [];

            return await response.Content.ReadFromJsonAsync<List<KCharacter>>() ?? [];
        }

        public async Task<List<KAnime>> SearchAsync(string query, int limit = 10)
        {
            var response = await http.GetAsync($"anime/search?query={Uri.EscapeDataString(query)}&limit={limit}");

            if (!response.IsSuccessStatusCode) return [];

            return await response.Content.ReadFromJsonAsync<List<KAnime>>() ?? [];
        }

        public async Task<Result<KAnime>> AddAsync(string title, string? description = null)
        {
            var response = await http.PostAsJsonAsync("anime", new { title, description });

            if (!response.IsSuccessStatusCode)
            {
                KDetail error = await response.Content.ReadFromJsonAsync<KDetail>();

                return Result<KAnime>.Fail(error);
            }

            KAnime anime = (await response.Content.ReadFromJsonAsync<KAnime>())!;

            return Result<KAnime>.Ok(anime);
        }

        public async Task<KDetail> SetTitleAsync(int id, string newTitle)
        {
            var response = await http.PatchAsJsonAsync($"anime/{id}/title", newTitle);

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> SetDescriptionAsync(int id, string newDescription)
        {
            var response = await http.PatchAsJsonAsync($"anime/{id}/description", newDescription);

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> DeleteAsync(int id)
        {
            var response = await http.DeleteAsync($"anime/{id}");

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }
    }
}
