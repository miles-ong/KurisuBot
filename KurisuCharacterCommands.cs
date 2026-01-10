using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace KurisuBot
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

            if(character == null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"*No character exists in the database with a name similar to **'{name}'***"));

                return;
            }

            DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character, user.Username);

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

            if (character == null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"*No character exists in the database with a name similar to **'{name}'***"));

                return;
            }

            DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(
                character, 
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

            if(anime == null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent($"*No anime exists in the database with a title similar to **'{animeTitle}'***"));

                return;
            }

            if(anime.CheckForCharacterNameSimilarity(name) is (int score, KurisuCharacter similarCharacter))
            {
                DiscordEmbedBuilder confirmationEmbed = UniversalEmbedBuilder.BuildConfirmationEmbed(
                    user.Username,
                    ConfirmationMessageType.AddCharacterToDatabase,
                    $"**{anime.Title}** contains a character with a similar name:\n\n **{similarCharacter.Name}**, **{score}% Match**\n\nWould you still like to add the character?"
                );

                TaskCompletionSource<bool> tcs = new();

                ConfirmationMessageData confirmationData = new()
                {
                    Owner = user,
                    Type = ConfirmationMessageType.AddCharacterToDatabase,
                    CompletionSource = tcs
                };

                DiscordMessage confirmationMessage = await UniversalEventHandler.RegisterAndSendConfirmationMessageAsync(ctx, confirmationData, confirmationEmbed, false);

                if(!await tcs.Task)
                {
                    return;
                }
            }

            KurisuCharacter character = await KurisuCharacterCache.AddCharacterAsync(name, anime, imageKey);

            await ctx.EditResponseAsync(new DiscordMessageBuilder()
                .WithContent($"Successfully added **'{character.Name}'** to the database!"));

            DiscordEmbedBuilder embed = KurisuCharacterEmbedBuilder.BuildCharacterInformationEmbed(character, displayId: true);

            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));
        }
    }
}