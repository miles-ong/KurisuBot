namespace KurisuCore.APIClient
{
    public sealed class KurisuAPIClient
    {
        public KAnimeAPIClient Anime { get; }
        public KCharacterAPIClient Character { get; }
        public KCharacterAnimeLinkAPIClient CALink { get; }
        public KVariantAPIClient Variant { get; }
        public KUserAPIClient User { get; }

        public KurisuAPIClient(string baseUrl, string apiKey)
        {
            var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            Anime = new KAnimeAPIClient(http);
            Character = new KCharacterAPIClient(http);
            CALink = new KCharacterAnimeLinkAPIClient(http);
            Variant = new KVariantAPIClient(http);
            User = new KUserAPIClient(http);
        }
    }
}
