using KurisuCore.Enums;
using KurisuCore.Models;
using System.Net.Http.Json;

namespace KurisuCore.APIClient
{
    public sealed class KUserAPIClient(HttpClient http)
    {
        public async Task<KUser?> GetOrCreateAsync(long id)
        {
            var response = await http.GetAsync($"users/{id}");

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<KUser>();
        }

        public async Task<bool> ModifyBalanceAsync(long id, KBalance balance, KOperation operation, int amount)
        {
            var response = await http.PatchAsJsonAsync($"users/{id}/balance", new { balance, operation, amount });

            return response.IsSuccessStatusCode;
        }
    }
}
