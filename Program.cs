using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Npgsql;

namespace KurisuBot
{
    internal class Program
    {
        public static DiscordClient Client { get; set; }

        static async Task Main(string[] args)
        {
            ConfigReader reader = await new ConfigReader().ReadConfig();

            await KurisuAnimeCache.Initialize(reader.Supabase.ConnectionString);

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(
                reader.Discord.Token,
                TextCommandProcessor.RequiredIntents | SlashCommandProcessor.RequiredIntents | DiscordIntents.MessageContents
            );

            builder.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            //builder.ConfigureEventHandlers(b => b
            //    .HandleComponentInteractionCreated()
            //);

            builder.UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) =>
            {
                extension.AddCommands(new[]
                {
                    typeof(KurisuAnimeCommands)
                });

                extension.AddProcessor(new TextCommandProcessor(new()
                {
                    PrefixResolver = new DefaultPrefixResolver(true, "?", "&").ResolvePrefixAsync
                }));

                extension.AddProcessor(new SlashCommandProcessor());
            }, new CommandsConfiguration()
            {
                DebugGuildId = 1458538350653866089
            });

            Client = builder.Build();

            DiscordActivity status = new("Hacking to the Gate", DiscordActivityType.ListeningTo);
            await Client.ConnectAsync(status, DiscordUserStatus.Online);
            await Task.Delay(-1);
        }
    }
}