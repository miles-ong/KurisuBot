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

            List<KurisuAnime> animes = KurisuAnimeCache.SearchByTitle(title);

            if (!animes.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"No animes exist in the database with a title similar to '{title}'"));

                return;
            }

            KurisuAnime anime = animes[0];

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle(anime.Title)
                .WithDescription(anime.Description);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed));
        }

        [Command("add")]
        public async Task KurisuAnimeAddCommand(
            CommandContext ctx,
            [Parameter("title")] string title,
            [Parameter("description")] string description,
            [Parameter("imagekey")] string? imagekey = null
        )
        {
            await ctx.DeferResponseAsync();

            KurisuAnime anime = await KurisuAnimeCache.Add(title, description, imagekey);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Successfully added '{anime.Title}' to the database!"));
        }

        [Command("id")]
        public async Task KurisuAnimeIdCommand(
            CommandContext ctx,
            [Parameter("title")] string title
        )
        {
            await ctx.DeferResponseAsync();

            List<KurisuAnime> animes = KurisuAnimeCache.SearchByTitle(title);

            if(!animes.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"No animes exist in the database with a title similar to '{title}'"));

                return;
            }

            KurisuAnime anime = animes[0];

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle(anime.Title)
                .WithDescription($"**ID:** {anime.Id}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed));
        }

        [Command("update")]
        public async Task KurisuAnimeUpdateCommand(
            CommandContext ctx,
            [Parameter("id")] int id,
            [Parameter("new-title")] string? newTitle = null,
            [Parameter("new-description")] string? newDescription = null,
            [Parameter("new-image-key")] string? newImageKey = null
        )
        {
            await ctx.DeferResponseAsync();

            KurisuAnime? anime = KurisuAnimeCache.SearchByID(id);

            if (anime == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"No anime exists in the database with the ID: {id}"));

                return;
            }

            string updateDescription = "";

            if(newTitle != null)
            {
                if(await anime.SetTitle(newTitle))
                {
                    updateDescription += $"**Title:** {newTitle}\n";
                }
            }

            if(newDescription != null)
            {
                if(await anime.SetDescription(newDescription))
                {
                    updateDescription += $"**Description:** {newDescription}\n";
                }
            }

            if(newImageKey != null)
            {
                if(await anime.SetImageKey(newImageKey))
                {
                    updateDescription += $"**Image Key:** {newImageKey}\n";
                }
            }
            
            if(String.IsNullOrEmpty(updateDescription))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Please update at least 1 field of the requested anime!"));

                return;
            }
            else
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    .WithTitle($"Updated Anime with ID: {id}")
                    .WithDescription(updateDescription);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(embed));
            }
        }
    }
}
