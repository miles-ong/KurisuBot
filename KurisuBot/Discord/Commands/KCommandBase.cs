using DSharpPlus.Entities;
using KurisuCore.Models;

namespace KurisuBot.Discord.Commands
{
    internal abstract class KCommandBase
    {
        protected static async Task<KUser?> GetUserAsync(DiscordUser dUser)
        {
            KUser? user = await Program.APIClient.User.GetOrCreateAsync((long)dUser.Id);

            if (user is not null) user.Username = dUser.Username;

            return user;
        }
    }
}
