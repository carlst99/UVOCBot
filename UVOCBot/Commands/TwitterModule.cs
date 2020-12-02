using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models.V2;
using UVOCBot.Model;

namespace UVOCBot.Commands
{
    [Group("twitter")]
    [Description("Commands pertinent to the twitter relay functionality")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class TwitterModule : BaseCommandModule
    {
        public ITwitterClient TwitterClient { private get; set; }
        public BotContext DbContext { private get; set; }

        [Command("add-user")]
        [Description("Adds a twitter user from whom tweets should be relayed")]
        public async Task AddUserCommand(CommandContext ctx, [Description("The person's twitter username, e.g. 'Wrel'")] string username)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            if (!Regex.IsMatch(username, "^[A-Za-z0-9_]{1,15}$"))
            {
                await ctx.RespondAsync("Invalid username! Twitter usernames can only contain letters and numbers, and must be between 1-15 characters long").ConfigureAwait(false);
                return;
            }

            UserV2Response user = await TwitterClient.UsersV2.GetUserByNameAsync(username).ConfigureAwait(false);
            if (user.User is null)
            {
                await ctx.RespondAsync("That Twitter user doesn't exist!").ConfigureAwait(false);
                return;
            }

            ulong guildId = ctx.Guild.Id;
            GuildTwitterSettings settings = DbContext.GuildTwitterSettings.FirstOrDefault(s => s.GuildId == guildId);
            if (settings == default)
            {
                settings = new GuildTwitterSettings
                {
                    GuildId = guildId
                };
                DbContext.GuildTwitterSettings.Add(settings);
            }
            TwitterUser twitterUser = new TwitterUser(user.User.Id);
            DbContext.Add(twitterUser);
            settings.TwitterUsers.Add(twitterUser);

            await ctx.RespondAsync($"Now relaying tweets from {username}!").ConfigureAwait(false);

            if (settings.RelayChannelId is null)
                await ctx.RespondAsync($"You haven't set a channel to relay tweets to. Please use the `{Program.PREFIX}twitter relay-channel` command").ConfigureAwait(false);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
