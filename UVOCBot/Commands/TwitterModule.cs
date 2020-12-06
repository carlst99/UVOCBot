using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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

            // Find or create the guild twitter settings record
            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings.FindAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettings(ctx.Guild.Id);
                DbContext.GuildTwitterSettings.Add(settings);
            }

            long twitterUserId = long.Parse(user.User.Id);

            // Find or create the twitter user record
            TwitterUser twitterUser = await DbContext.TwitterUsers.FindAsync(twitterUserId).ConfigureAwait(false);
            if (twitterUser == default)
            {
                twitterUser = new TwitterUser(twitterUserId);
                DbContext.TwitterUsers.Add(twitterUser);
            }

            settings.TwitterUsers.Add(twitterUser);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            await ctx.RespondAsync($"Now relaying tweets from {username}!").ConfigureAwait(false);

            if (settings.RelayChannelId is null)
                await ctx.RespondAsync($"You haven't set a channel to relay tweets to. Please use the `{Program.PREFIX}twitter relay-channel` command").ConfigureAwait(false);
        }

        [Command("relay-channel")]
        [Description("Sets the channel to which tweets will be relayed")]
        public async Task RelayChannelCommand(CommandContext ctx, [Description("The channel that tweets should be relayed to")] DiscordChannel channel)
        {
            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings.FindAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettings(ctx.Guild.Id);
                DbContext.Add(settings);
            }

            settings.RelayChannelId = channel.Id;
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            await ctx.RespondAsync("Tweets will now be relayed to " + channel.Mention).ConfigureAwait(false);
        }
    }
}
