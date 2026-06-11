using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KurisuCore.Models;

namespace KurisuBot.Discord.AutoCompleters
{
    internal sealed class KAnimeAutoCompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            string query = ctx.UserInput ?? "";
            List<KAnime> results = await Program.ApiClient.Anime.SearchAsync(query);
            Console.WriteLine($"Found: {results.Count}");

            return results.Select(a => new DiscordAutoCompleteChoice(a.Title, a.Id));
        }
    }
}
