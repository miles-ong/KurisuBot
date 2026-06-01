using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KurisuBot.Core.Models;

namespace KurisuBot.Discord
{
    internal static class DiscordEventHandlerHelper
    {
        public static async Task<bool> ValidateInputAuthAsync(ComponentInteractionCreatedEventArgs e, KUser owner)
        {
            if (e.User.Id != (ulong)owner.Id)
            {
                await e.Interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("This message is not for you!")
                        .AsEphemeral());

                return false;
            }

            return true;
        }
    }
}
