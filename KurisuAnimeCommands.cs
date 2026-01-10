using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace KurisuBot
{
    [Command("anime")]
    public class KurisuAnimeCommands
    {
        [Command("search")]
        public async Task KurisuAnimeSearchCommand(
            CommandContext ctx,
            [Parameter("title")] string title
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

            KurisuAnime? anime = KurisuAnimeCache.SearchByTitleBestMatch(title);

            if (anime == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"*No anime exists in the database with a title similar to **'{title}'***"));

                return;
            }

            DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(anime, user.Username);

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }

        [Command("id")]
        public async Task KurisuAnimeIdCommand(
            CommandContext ctx,
            [Parameter("title")] string title
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

            KurisuAnime? anime = KurisuAnimeCache.SearchByTitleBestMatch(title);

            if (anime != null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"*No anime exists in the database with a title similar to **'{title}'***"));

                return;
            }

            DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(
                anime,
                requestUsername: user.Username,
                displayId: true,
                displayDescription: false,
                displayImage: false,
                displayCharactersCount: false
            );

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }

        [Command("add")]
        public async Task KurisuAnimeAddCommand(
            CommandContext ctx,
            [Parameter("title")] string title,
            [Parameter("description")] string description,
            [Parameter("imagekey")] int? imageKey = null
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

            if(KurisuAnimeCache.CheckForAnimeTitleSimilarity(title) is (int score, KurisuAnime similarAnime))
            {
                DiscordEmbedBuilder confirmationEmbed = UniversalEmbedBuilder.BuildConfirmationEmbed(
                    user.Username,
                    ConfirmationMessageType.AddAnimeToDatabase,
                    $"**{title}** is similar to the following title:\n\n**{similarAnime.Title}**, **{score}% Match**\n\nWould you still like to add the anime?"
                );

                TaskCompletionSource<bool> tcs = new();

                ConfirmationMessageData confirmationData = new()
                {
                    Owner = user,
                    Type = ConfirmationMessageType.AddAnimeToDatabase,
                    CompletionSource = tcs
                };

                DiscordMessage confirmationMessage = await UniversalEventHandler.RegisterAndSendConfirmationMessageAsync(ctx, confirmationData, confirmationEmbed, false);

                if(!await tcs.Task)
                {
                    return;
                }
            }

            KurisuAnime anime = await KurisuAnimeCache.AddAnimeAsync(title, description, imageKey);

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .WithContent($"Successfully added **'{anime.Title}'** to the database!"));

            DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(anime, displayId: true);

            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }
    }
}
