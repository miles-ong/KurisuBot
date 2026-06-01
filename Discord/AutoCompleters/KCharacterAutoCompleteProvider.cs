using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace KurisuBot.Discord.AutoCompleters
{
    internal sealed class KCharacterAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            string query = ctx.UserInput ?? "";

            IEnumerable<DiscordAutoCompleteChoice> choices = Program.CharacterService
                .Search(query)
                .Select(c => new DiscordAutoCompleteChoice(c.Name, c.Id));

            return ValueTask.FromResult(choices);
        }
    }
}
