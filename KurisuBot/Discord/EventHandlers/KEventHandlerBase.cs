using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KurisuCore.Models;
using System.Collections.Concurrent;

namespace KurisuBot.Discord.EventHandlers
{
    internal abstract class KEventHandlerBase<TEntry>
    {
        private static readonly ConcurrentDictionary<ulong, TEntry> _active = new();

        protected static bool TryRegister(ulong messageId, TEntry entry)
            => _active.TryAdd(messageId, entry);

        protected static bool TryGet(ulong messageId, out TEntry? entry)
            => _active.TryGetValue(messageId, out entry);

        protected static bool TryRemove(ulong messageId, out TEntry? entry)
            => _active.TryRemove(messageId, out entry);

        protected static async Task HandleTimeoutAsync(ulong messageId, int timeoutSeconds, Func<Task> onTimeout)
        {
            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            if (!_active.TryRemove(messageId, out _)) return;

            await onTimeout();
        }

        protected static async Task<bool> ValidateInputAuthAsync(ComponentInteractionCreatedEventArgs e, KUser owner)
        {
            if (e.User.Id != (ulong)owner.Id)
            {
                await e.Interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("This message is not for you!")
                        .AsEphemeral()
                );

                return false;
            }

            return true;
        }

        protected static async Task UpdateMessageAsync(ComponentInteractionCreatedEventArgs e, DiscordEmbed embed)
            => await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }
}
