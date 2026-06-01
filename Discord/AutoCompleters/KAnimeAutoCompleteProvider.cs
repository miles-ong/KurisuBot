using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KurisuBot.Engine;

namespace KurisuBot.Discord.AutoCompleters
{
    internal sealed class KAnimeAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            string query = ctx.UserInput ?? "";

            IEnumerable<DiscordAutoCompleteChoice> choices = Program.AnimeService
                .Search(query)
                .Select(a => new DiscordAutoCompleteChoice(a.Title, a.Id));

            return ValueTask.FromResult(choices);
        }
    }
}
