using DSharpPlus.Entities;
using KurisuBot.EventHandlers;

namespace KurisuBot.EmbedBuilders
{
    public static class UniversalEmbedBuilder
    {
        public static DiscordEmbedBuilder BuildConfirmationEmbed(string requestUsername, ConfirmationMessageType confirmationType, string confirmationDescription)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = $"{UniversalHelper.AddSpacesInEnum(confirmationType.ToString())} Confirmation",
                Description = confirmationDescription,
                Color = DiscordColor.Blurple
            };

            embed.WithFooter($"Requested by {requestUsername}");

            return embed;
        }

        public static DiscordEmbedBuilder BuildConfirmationResultEmbed(string requestUsername, ConfirmationMessageType confirmationType, bool isConfirmed)
        {
            string confirmationTypeString = UniversalHelper.AddSpacesInEnum(confirmationType.ToString());

            DiscordEmbedBuilder embed = new()
            {
                Title = $"{confirmationTypeString} Confirmation",
                Description = isConfirmed ? $":white_check_mark: {confirmationTypeString} confirmed!" : $":x: {confirmationTypeString} canceled...",
                Color = isConfirmed ? DiscordColor.Green : DiscordColor.Red
            };

            embed.WithFooter($"Requested by {requestUsername}");

            return embed;
        }

        public static DiscordButtonComponent[] GetConfirmationButtons()
        {
            DiscordButtonComponent confirmButton = new DiscordButtonComponent(DiscordButtonStyle.Success, "confirm", "Confirm");
            DiscordButtonComponent cancelButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, "cancel", "Cancel");
            DiscordButtonComponent[] buttons = { confirmButton, cancelButton };

            return buttons;
        }
    }
}
