using DSharpPlus.Commands;
using DSharpPlus.Entities;
using KurisuBot.EmbedBuilders;
using KurisuBot.EventHandlers;

namespace KurisuBot.Commands
{
    [Command("character")]
    public class KurisuCharacterCommands
    {
        [Command("search")]
        public async Task KurisuCharacterSearchCommand(
            CommandContext ctx,
            [Parameter("name")] string name
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

            KurisuCharacter? character = KurisuCharacterCache.SearchByNameBestMatch(name);

            if(!await KurisuHelper.HandleNullCharacterSearchAsync(ctx, character, name))
            {
                return;
            }

            DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character!, user.Username);

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }

        [Command("id")]
        public async Task KurisuCharacterIdCommand(
            CommandContext ctx,
            [Parameter("name")] string name
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

            KurisuCharacter? character = KurisuCharacterCache.SearchByNameBestMatch(name);

            if(!await KurisuHelper.HandleNullCharacterSearchAsync(ctx, character, name))
            {
                return;
            }

            DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(
                character!, 
                requestUsername: user.Username, 
                displayId: true,
                displayAnime: false,
                displayImage: false,
                displayVariantsCount: false
            );

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }

        [Command("add")]
        public async Task KurisuCharacterAddCommand(
            CommandContext ctx,
            [Parameter("name")] string name,
            [Parameter("anime")] string animeTitle,
            [Parameter("imagekey")] int? imageKey = null
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

            KurisuAnime? anime = KurisuAnimeCache.SearchByTitleBestMatch(animeTitle);

            if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, animeTitle))
            {
                return;
            }

            if(!await KurisuHelper.ConfirmCharacterNameSimilarityAsync(ctx, user, name, anime!, ConfirmationMessageType.AddCharacterToDatabase))
            {
                return;
            }

            KurisuCharacter character = await KurisuCharacterCache.AddCharacterAsync(name, anime!, imageKey);

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .WithContent($"Successfully added **'{character.Name}'** to the database!"));

            DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character, displayId: true);

            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }

        [Command("edit")]
        public class KurisuCharacterEditCommands
        {
            [Command("name")]
            public async Task KurisuCharacterEditNameCommand(
                CommandContext ctx,
                [Parameter("id")] int id,
                [Parameter("new-name")] string newName
            )
            {
                await ctx.DeferResponseAsync();

                KurisuUser user = await KurisuUserCache.GetUserAsync(ctx);

                KurisuCharacter? character = KurisuCharacterCache.SearchByID(id);

                if(!await KurisuHelper.HandleNullCharacterSearchAsync(ctx, character, id: id))
                {
                    return;
                }

                foreach(KurisuAnime anime in character!.Animes)
                {
                    if(!await KurisuHelper.ConfirmCharacterNameSimilarityAsync(ctx, user, newName, anime, ConfirmationMessageType.UpdateCharacterInDatabase))
                    {
                        return;
                    }
                }

                await character.SetNameAsync(newName);

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"Successfully edited **'{character.Name}' (ID: {character.Id})**!"));

                DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character, displayId: true);

                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .AddEmbed(embed));
            }

            [Command("addanime")]
            public async Task KurisuCharacterEditAddAnimeCommand(
                CommandContext ctx,
                [Parameter("id")] int id,
                [Parameter("anime")] string animeTitle
            )
            {
                await ctx.DeferResponseAsync();

                KurisuAnime? anime = KurisuAnimeCache.SearchByTitleBestMatch(animeTitle);

                if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, animeTitle))
                {
                    return;
                }

                KurisuCharacter? character = KurisuCharacterCache.SearchByID(id);

                if(!await KurisuHelper.HandleNullCharacterSearchAsync(ctx, character))
                {
                    return;
                }

                if(!await character!.AddAnimeAsync(anime!))
                {
                    await ctx.EditResponseAsync(new DiscordMessageBuilder()
                        .WithContent($"Failed to add **'{anime!.Title}'** to **'{character.Name}' (ID: {character.Id})**!"));

                    return;
                }

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"Successfully added **'{anime!.Title}'** to **'{character.Name}' (ID: {character.Id})**!"));

                DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character, displayId: true);

                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .AddEmbed(embed));
            }

            [Command("removeanime")]
            public async Task KurisuCharacterEditRemoveAnimeCommand(
                CommandContext ctx,
                [Parameter("id")] int id,
                [Parameter("anime")] string animeTitle
            )
            {
                await ctx.DeferResponseAsync();

                KurisuAnime? anime = KurisuAnimeCache.SearchByTitleBestMatch(animeTitle);

                if(!await KurisuHelper.HandleNullAnimeSearchAsync(ctx, anime, animeTitle))
                {
                    return;
                }

                KurisuCharacter? character = KurisuCharacterCache.SearchByID(id);

                if(!await KurisuHelper.HandleNullCharacterSearchAsync(ctx, character))
                {
                    return;
                }

                if(!await character!.RemoveAnimeAsync(anime!))
                {
                    await ctx.EditResponseAsync(new DiscordMessageBuilder()
                        .WithContent($"Failed to remove **'{anime!.Title}'** to **'{character.Name}' (ID: {character.Id})**!"));

                    return;
                }

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"Successfully removed **'{anime!.Title}'** to **'{character.Name}' (ID: {character.Id})**!"));

                DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character, displayId: true);

                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .AddEmbed(embed));
            }
        }
    }
}