namespace KurisuBot.Engine
{
    internal sealed class KConfig
    {
        public DiscordConfig Discord { get; set; } = new();
        public SupabaseConfig Supabase { get; set; } = new();
        public MeilisearchConfig Meilisearch { get; set; } = new();
    }

    internal sealed class DiscordConfig
    {
        public string Token { get; set; } = "";
        public string Prefix { get; set; } = "";
    }

    internal sealed class SupabaseConfig
    {
        public string ConnectionString { get; set; } = "";
    }

    internal sealed class MeilisearchConfig
    {
        public string Url { get; set; } = "";
        public string Key { get; set; } = "";
    }
}
