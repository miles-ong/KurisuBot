using KurisuCore.Models;
using KurisuCore.Result;
using System.Net.Http.Json;

namespace KurisuCore.APIClient
{
    public sealed class KCharacterAPIClient(HttpClient http)
    {
        public async Task<KCharacter?> GetAsync(int id)
        {
            var response = await http.GetAsync($"characters/{id}");

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<KCharacter>();
        }

        public async Task<List<KAnime>> GetAnimesAsync(int id)
        {
            var response = await http.GetAsync($"characters/{id}/anime");

            if (!response.IsSuccessStatusCode) return [];

            return await response.Content.ReadFromJsonAsync<List<KAnime>>() ?? [];
        }

        public async Task<List<KVariant>> GetVariantsAsync(int id)
        {
            var response = await http.GetAsync($"characters/{id}/variants");

            if (!response.IsSuccessStatusCode) return [];

            return await response.Content.ReadFromJsonAsync<List<KVariant>>() ?? [];
        }

        public async Task<List<KCharacter>> SearchAsync(string query, int limit = 10)
        {
            var response = await http.GetAsync($"characters/search?query={Uri.EscapeDataString(query)}&limit={limit}");

            if (!response.IsSuccessStatusCode) return [];

            return await response.Content.ReadFromJsonAsync<List<KCharacter>>() ?? [];
        }

        public async Task<Result<KCharacter>> AddAsync(string name)
        {
            var response = await http.PostAsJsonAsync("characters", name);

            if (!response.IsSuccessStatusCode)
            {
                KDetail error = await response.Content.ReadFromJsonAsync<KDetail>();

                return Result<KCharacter>.Fail(error);
            }

            KCharacter character = (await response.Content.ReadFromJsonAsync<KCharacter>())!;

            return Result<KCharacter>.Ok(character);
        }

        public async Task<KDetail> SetNameAsync(int id, string newName)
        {
            var response = await http.PatchAsJsonAsync($"characters/{id}/name", newName);

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> DeleteAsync(int id)
        {
            var response = await http.DeleteAsync($"characters/{id}");

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }
    }
}
