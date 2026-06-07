using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KurisuCore.Models;

namespace KurisuBot.Discord.AutoCompleters
{
    internal sealed class KCharacterAutoCompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            string query = ctx.UserInput ?? "";
            List<KCharacter> results = await Program.ApiClient.Character.SearchAsync(query);

            return results.Select(c => new DiscordAutoCompleteChoice(c.Name, c.Id));
        }
    }
}
