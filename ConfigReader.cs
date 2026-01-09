using System.Text.Json;

namespace KurisuBot
{
    internal sealed class DiscordConfig
    {
        public string Token { get; set; } = "";
        public string Prefix { get; set; } = "";

        public bool IsRead()
        {
            return !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Prefix);
        }
    }

    internal sealed class SupabaseConfig
    {
        public string ConnectionString { get; set; } = "";

        public bool IsRead()
        {
            return !string.IsNullOrEmpty(ConnectionString);
        }
    }

    internal class ConfigReader
    {
        public DiscordConfig Discord { get; set; } = new();
        public SupabaseConfig Supabase { get; set; } = new();

        public async Task<ConfigReader> ReadConfig()
        {
            using StreamReader sr = new StreamReader("config.json");
            string jsonString = await sr.ReadToEndAsync();
            ConfigReader reader = JsonSerializer.Deserialize<ConfigReader>(jsonString);

            Discord = reader.Discord;
            Supabase = reader.Supabase;

            Console.WriteLine($"[ConfigReader] DiscordConfig Read: {Discord.IsRead()}");
            Console.WriteLine($"[ConfigReader] SupabaseConfig Read: {Supabase.IsRead()}");

            return reader;
        }
    }
}
