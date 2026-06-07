using DotNetEnv;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using KurisuBot.AniList;
using KurisuBot.Discord.Commands;
using KurisuBot.Discord.EventHandlers;
using KurisuBot.Quiz;
using KurisuBot.Quiz.DataSources;
using KurisuCore.APIClient;

namespace KurisuBot
{
    internal sealed class Program
    {
        public static DiscordClient Client { get; private set; } = null!;
        public static KurisuAPIClient ApiClient { get; private set; } = null!;
        public static AniListService ALService { get; private set; } = null!;
        public static KQuizGenerator QuizGenerator { get; private set; } = null!;

        private static readonly HttpClient _httpClient = new();
        private static readonly string _consoleTag = "[KurisuBot]";

        static async Task Main(string[] args)
        {
            Console.WriteLine($"{_consoleTag} Loading environment variables...");

            Env.Load();

            string discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                ?? throw new Exception("Missing DISCORD_TOKEN");

            string apiUrl = Environment.GetEnvironmentVariable("API_URL")
                ?? throw new Exception("Missing API_URL");

            string apiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY")
                ?? throw new Exception("Missing ADMIN_API_KEY");

            Console.WriteLine($"{_consoleTag} Successfully loaded environment variables");

            ApiClient = new KurisuAPIClient(apiUrl, apiKey);

            Console.WriteLine($"{_consoleTag} Waiting for API...");

            while (!await ApiClient.CheckHealthAsync())
            {
                await Task.Delay(2000);
                Console.WriteLine($"{_consoleTag} API not ready, retrying...");
            }

            Console.WriteLine($"{_consoleTag} API ready");

            ALService = new AniListService(_httpClient);
            await ALService.InitializeAsync();
            QuizGenerator = new KQuizGenerator(new AniListQuizDataSource(ALService));

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(
                discordToken,
                TextCommandProcessor.RequiredIntents | SlashCommandProcessor.RequiredIntents | DiscordIntents.MessageContents
            );

            builder.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            builder.ConfigureEventHandlers(b => b
                .HandleComponentInteractionCreated(KQuizEventHandler.QuizInteractionAsync)
            );

            builder.UseCommands((_, extension) =>
            {
                extension.AddCommands([
                    typeof(KAnimeCommands),
                    typeof(KUserCommands),
                    typeof(KQuizCommands)
                ]);
            }, new CommandsConfiguration()
            {
                DebugGuildId = 1458538350653866089
            });

            Client = builder.Build();

            DiscordActivity status = new("Hacking to the Gate", DiscordActivityType.ListeningTo);
            await Client.ConnectAsync(status, DiscordUserStatus.Online);
            Console.WriteLine($"{_consoleTag} Online");

            await Task.Delay(-1);
        }
    }
}
