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

            if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, title))
            {
                return;
            }

            DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(anime!, user.Username);

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

            if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, title))
            {
                return;
            }

            DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(
                anime!,
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

            KurisuAnime? similarAnime = KurisuAnimeCache.SearchByTitleBestMatch(title);

            if(!await KurisuHelper.ConfirmAnimeNameSimilarityAsync(ctx, user, title, ConfirmationMessageType.AddAnimeToDatabase))
            {
                return;
            }

            KurisuAnime anime = await KurisuAnimeCache.AddAnimeAsync(title, description, imageKey);

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .WithContent($"Successfully added **'{anime.Title}'** to the database!"));

            DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(anime, displayId: true);

            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }

        [Command("edit")]
        public class KurisuAnimeEditCommands
        {
            [Command("title")]
            public async Task KurisuAnimeEditTitleCommand(
                CommandContext ctx,
                [Parameter("id")] int id,
                [Parameter("new-title")] string newTitle
            )
            {
                await ctx.DeferResponseAsync();

                KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

                KurisuAnime? anime = KurisuAnimeCache.SearchByID(id);

                if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, id: id))
                {
                    return;
                }

                if(!await KurisuHelper.ConfirmAnimeNameSimilarityAsync(ctx, user, newTitle, ConfirmationMessageType.UpdateAnimeInDatabase))
                {
                    return;
                }

                if(!await anime!.SetTitleAsync(newTitle))
                {
                    await ctx.EditResponseAsync(new DiscordMessageBuilder()
                        .WithContent($"Failed to edit **'{anime.Title}' (ID: {anime.Id})**..."));

                    return;
                }

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"Successfully edited **'{anime.Title}'** (ID: {anime.Id})**!"));

                DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(anime, displayId: true);

                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .AddEmbed(embed));
            }

            [Command("description")]
            public async Task KurisuAnimeEditDescriptionCommand(
                CommandContext ctx,
                [Parameter("id")] int id,
                [Parameter("new-description")] string newDescription
            )
            {
                await ctx.DeferResponseAsync();

                KurisuAnime? anime = KurisuAnimeCache.SearchByID(id);

                if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, id: id))
                {
                    return;
                }

                if(anime!.Description.Equals(newDescription))
                {
                    await ctx.EditResponseAsync(new DiscordMessageBuilder()
                        .WithContent($"Failed to edit **'{anime.Title}' (ID: {anime.Id})**, the new description must differ from the old description!"));

                    return;
                }

                await anime.SetDescriptionAsync(newDescription);

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"Successfully edited **'{anime.Title}'** (ID: {anime.Id})**!"));

                DiscordEmbedBuilder embed = KurisuAnimeEmbedBuilder.BuildAnimeInformationEmbed(anime, displayId: true);

                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .AddEmbed(embed));
            }
        }
    }
}
