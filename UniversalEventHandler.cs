using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public enum ConfirmationMessageType
    {
        Purchase,
        Sell,
        Trade,
        AddAnimeToDatabase,
        AddCharacterToDatabase,
        UpdateAnimeInDatabase,
        UpdateCharacterInDatabase
    }

    public struct ConfirmationMessageData
    {
        public KurisuUser Owner;
        public ConfirmationMessageType Type;
        public TaskCompletionSource<bool> CompletionSource;
    }

    public static class UniversalEventHandler
    {
        private static readonly int _confirmationMessageExpiryTime = 30;
        private static ConcurrentDictionary<ulong, ConfirmationMessageData> _activeConfirmationMessages = new();

        public static async Task<DiscordMessage> RegisterAndSendConfirmationMessageAsync(
            CommandContext ctx,
            ConfirmationMessageData confirmationData,
            DiscordEmbedBuilder embed,
            bool sendAsNewMessage = false
        )
        {
            DiscordMessageBuilder confirmationMessageContent = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddActionRowComponent(UniversalEmbedBuilder.GetConfirmationButtons());

            DiscordMessage message;

            if(sendAsNewMessage)
            {
                message = await ctx.Channel.SendMessageAsync(confirmationMessageContent);
            }
            else
            {
                message = await ctx.EditResponseAsync(confirmationMessageContent);
            }

            _activeConfirmationMessages.TryAdd(message.Id, confirmationData);
            _ = HandleConfirmationTimeoutAsync(message);

            return message;
        }

        public static async Task ConfirmationInteractionAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs e)
        {
            if(!e.Id.StartsWith("confirm") && !e.Id.StartsWith("cancel"))
            {
                return;
            }

            if(!_activeConfirmationMessages.TryGetValue(e.Message.Id, out ConfirmationMessageData targetData))
            {
                return;
            }

            if(await ValidateInputAuthAsync(e, targetData.Owner))
            {
                _activeConfirmationMessages.TryRemove(e.Message.Id, out _);

                bool confirmed = e.Id.Equals("confirm");
                targetData.CompletionSource.TrySetResult(confirmed);

                if(!confirmed)
                {
                    DiscordEmbedBuilder embed = UniversalEmbedBuilder.BuildConfirmationResultEmbed(targetData.Owner.Username, targetData.Type, false);

                    DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed);

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, response);
                }
                else
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                }
            }
        }

        public static async Task<bool> ValidateInputAuthAsync(ComponentInteractionCreatedEventArgs e, KurisuUser messageOwner, string denyMessage = "This message is not for you!")
        {
            if(e.User.Id != (ulong)messageOwner.Id)
            {
                await e.Interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(denyMessage)
                        .AsEphemeral()
                );

                return false;
            }

            return true;
        }

        private static async Task HandleConfirmationTimeoutAsync(DiscordMessage message)
        {
            await Task.Delay(TimeSpan.FromSeconds(_confirmationMessageExpiryTime));

            if(_activeConfirmationMessages.TryRemove(message.Id, out ConfirmationMessageData confirmationData))
            {
                DiscordEmbedBuilder embed = UniversalEmbedBuilder.BuildConfirmationResultEmbed(confirmationData.Owner.Username, confirmationData.Type, false);

                DiscordMessageBuilder timeoutMessageContent = new DiscordMessageBuilder()
                    .AddEmbed(embed);

                await message.ModifyAsync(timeoutMessageContent);
            }
        }
    }
}
