using KurisuCore.Result;
using System.Net.Http.Json;

namespace KurisuCore.APIClient
{
    public sealed class KCharacterAnimeLinkAPIClient(HttpClient http)
    {
        public async Task<KDetail> LinkAsync(int characterId, int animeId)
        {
            var response = await http.PostAsJsonAsync("calinks", new { characterId, animeId });

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }

        public async Task<KDetail> UnlinkAsync(int characterId, int animeId)
        {
            var response = await http.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("calinks", UriKind.Relative),
                Content = JsonContent.Create(new { characterId, animeId })
            });

            if (!response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<KDetail>();

            return KDetail.Completed;
        }
    }
}
