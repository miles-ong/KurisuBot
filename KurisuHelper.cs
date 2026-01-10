using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace KurisuBot
{
    public static class KurisuHelper
    {
        public static async Task<bool> HandleNullCharacterSearchAsync(CommandContext ctx, KurisuCharacter? character, string? name = null, int id = 0)
        {
            if (character == null)
            {
                string messageContent = !string.IsNullOrEmpty(name)
                    ? $"No character exists in the database with a name similar to **'{name}'**"
                    : $"No character exists in the database with the **ID: {id}**";

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent(messageContent));

                return false;
            }

            return true;
        }

        public static async Task<bool> HandleNullAnimeSearchAsync(CommandContext ctx, KurisuAnime? anime, string? title = null, int id = 0)
        {
            if (anime == null)
            {
                string messageContent = !string.IsNullOrEmpty(title)
                    ? $"No anime exists in the database with a title similar to **'{title}'**"
                    : $"No anime exists in the database with the **ID: {id}**";

                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .WithContent(messageContent));

                return false;
            }

            return true;
        }

        public static async Task<bool> ConfirmAnimeNameSimilarityAsync(CommandContext ctx, KurisuUser user, string title, ConfirmationMessageType confirmationType)
        {
            if(KurisuAnimeCache.CheckForAnimeTitleSimilarity(title) is (int score, KurisuAnime similarAnime))
            {
                if(score >= 100)
                {
                    await ctx.EditResponseAsync(new DiscordMessageBuilder()
                        .WithContent($"There already exists an anime with the same title as **'{title}'**!"));

                    return false;
                }

                string action = confirmationType switch
                {
                    ConfirmationMessageType.AddAnimeToDatabase => "Would you still like to add the anime?",
                    ConfirmationMessageType.UpdateAnimeInDatabase => "Would you still like to update the anime?",
                    _ => "Would you still like to follow through with this command?"
                };

                DiscordEmbedBuilder embed = UniversalEmbedBuilder.BuildConfirmationEmbed(
                    user.Username,
                    confirmationType,
                    $"**{title}** is similar to the following title:\n\n**{similarAnime.Title}**, **{score}%**\n\n{action}"
                );

                TaskCompletionSource<bool> tcs = new();

                ConfirmationMessageData confirmationData = new()
                {
                    Owner = user,
                    Type = confirmationType,
                    CompletionSource = tcs
                };

                DiscordMessage confirmationMessage = await UniversalEventHandler.RegisterAndSendConfirmationMessageAsync(ctx, confirmationData, embed, false);

                if(!await tcs.Task)
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<bool> ConfirmCharacterNameSimilarityAsync(CommandContext ctx, KurisuUser user, string name, KurisuAnime anime, ConfirmationMessageType confirmationType)
        {
            if (anime.CheckForCharacterNameSimilarity(name) is (int score, KurisuCharacter similarCharacter))
            {
                if (score >= 100)
                {
                    await ctx.EditResponseAsync(new DiscordMessageBuilder()
                        .WithContent($"**{anime.Title}** already contains a character with the name **'{name}'**!"));

                    return false;
                }

                string action = confirmationType switch
                {
                    ConfirmationMessageType.AddCharacterToDatabase => "Would you still like to add the character?",
                    ConfirmationMessageType.UpdateCharacterInDatabase => "Would you still like to update the character?",
                    _ => "Would you still like to follow through with this command?"
                };

                DiscordEmbedBuilder confirmationEmbed = UniversalEmbedBuilder.BuildConfirmationEmbed(
                    user.Username,
                    confirmationType,
                    $"**{anime.Title}** contains a character with a similar name:\n\n **{similarCharacter.Name}**, **{score}% Match**\n\n{action}"
                );

                TaskCompletionSource<bool> tcs = new();

                ConfirmationMessageData confirmationData = new()
                {
                    Owner = user,
                    Type = confirmationType,
                    CompletionSource = tcs
                };

                DiscordMessage confirmationMessage = await UniversalEventHandler.RegisterAndSendConfirmationMessageAsync(ctx, confirmationData, confirmationEmbed, false);

                if (!await tcs.Task)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
