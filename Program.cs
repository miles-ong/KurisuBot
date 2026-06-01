using DotNetEnv;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using KurisuBot.AniList;
using KurisuBot.Core.Quiz;
using KurisuBot.Core.Quiz.DataSources;
using KurisuBot.Core.Services;
using KurisuBot.Discord;
using KurisuBot.Discord.EventHandlers;
using System.Text.Json;

namespace KurisuBot.Engine
{
    internal sealed class Program
    {
        public static DiscordClient Client { get; private set; } = null!;

        public static KAnimeService AnimeService { get; private set; } = null!;
        public static KCharacterService CharacterService { get; private set; } = null!;
        public static KCharacterAnimeLinkService CharacterAnimeLinkService { get; private set; } = null!;
        public static KVariantService VariantService { get; private set; } = null!;
        public static KCardService CardService { get; private set; } = null!;
        public static AniListService ALService { get; private set; } = null!;
        public static KUserService UserService { get; private set; } = null!;
        public static KQuizGenerator QuizGenerator { get; private set; } = null!;

        private static readonly HttpClient _httpClient = new();
        private static readonly string _consoleTag = "[Program]";

        static async Task Main(string[] args)
        {
            Env.Load();

            Console.WriteLine($"{_consoleTag} Loading environment variables...");

            string discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                ?? throw new Exception("Missing DISCORD_TOKEN");

            string supabaseConnection = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION")
                ?? throw new Exception("Missing SUPABASE_CONNECTION");

            Console.WriteLine($"{_consoleTag} Successfully loaded environment variables");

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

            builder.UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) =>
            {
                extension.AddCommands(
                [
                    typeof(KAnimeCommands),
                    typeof(KUserCommands),
                    typeof(KQuizCommands)
                ]);
            }, new CommandsConfiguration()
            {
                DebugGuildId = 1458538350653866089
            });

            Client = builder.Build();

            Console.WriteLine($"{_consoleTag} Constructing services...");
            AnimeService = new KAnimeService(supabaseConnection);
            CharacterService = new KCharacterService(supabaseConnection);
            CharacterAnimeLinkService = new KCharacterAnimeLinkService(supabaseConnection);
            VariantService = new KVariantService(supabaseConnection);
            CardService = new KCardService(supabaseConnection);
            ALService = new AniListService(_httpClient);
            UserService = new KUserService(supabaseConnection, async id =>
            {
                DiscordUser dUser = await Client.GetUserAsync((ulong)id);

                return dUser.Username;
            });
            QuizGenerator = new KQuizGenerator(new AniListQuizDataSource(ALService));

            Console.WriteLine($"{_consoleTag} Initializing services...");
            await AnimeService.InitializeAsync();
            await CharacterService.InitializeAsync();
            await CharacterAnimeLinkService.InitializeAsync();
            await VariantService.InitializeAsync();
            CardService.Initialize();
            await ALService.InitializeAsync();
            UserService.Initialize();
            Console.WriteLine($"{_consoleTag} Successfully initialized services!");

            DiscordActivity status = new("Hacking to the Gate", DiscordActivityType.ListeningTo);
            await Client.ConnectAsync(status, DiscordUserStatus.Online);
            Console.WriteLine($"{_consoleTag} Online!");

            await Task.Delay(-1);
        }
    }
}
