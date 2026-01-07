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

    internal class ConfigReader
    {
        public DiscordConfig DiscordC { get; set; } = new();

        public async Task<ConfigReader> ReadConfig()
        {
            using StreamReader sr = new StreamReader("config.json");
            string jsonString = await sr.ReadToEndAsync();
            ConfigReader reader = JsonSerializer.Deserialize<ConfigReader>(jsonString);

            DiscordC = reader.DiscordC;

            Console.WriteLine($"[ConfigReader] DiscordConfig Read: {DiscordC.IsRead()}");

            return reader;
        }
    }
}
