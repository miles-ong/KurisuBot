namespace KurisuCore.APIClient
{
    public sealed class KurisuAPIClient
    {
        public KAnimeAPIClient Anime { get; }
        public KCharacterAPIClient Character { get; }
        public KCharacterAnimeLinkAPIClient CALink { get; }
        public KVariantAPIClient Variant { get; }
        public KUserAPIClient User { get; }

        private readonly HttpClient _http;

        public KurisuAPIClient(string baseUrl, string apiKey)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            Anime = new KAnimeAPIClient(_http);
            Character = new KCharacterAPIClient(_http);
            CALink = new KCharacterAnimeLinkAPIClient(_http);
            Variant = new KVariantAPIClient(_http);
            User = new KUserAPIClient(_http);
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _http.GetAsync("health");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
    }
}
