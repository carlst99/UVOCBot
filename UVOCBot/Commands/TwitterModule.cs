using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models.V2;
using UVOCBot.Model;

namespace UVOCBot.Commands
{
    // TODO: Limit who can send commands
    [Group("twitter")]
    [Description("Commands pertinent to the twitter relay functionality")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class TwitterModule : BaseCommandModule
    {
        public ITwitterClient TwitterClient { private get; set; }
        public BotContext DbContext { private get; set; }

        [Command("add-user")]
        [Description("Starts relaying tweets from a twitter user")]
        public async Task AddUserCommand(CommandContext ctx, [Description("The person's twitter username, e.g. 'Wrel'")] string username)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Check that the username conforms to Twitters standards
            if (!Regex.IsMatch(username, "^[A-Za-z0-9_]{1,15}$"))
            {
                await ctx.RespondAsync("Invalid username! Twitter usernames can only contain letters and numbers, and must be between 1-15 characters long").ConfigureAwait(false);
                return;
            }

            // Get the user info from Twitter
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

        [Command("remove-user")]
        [Description("Stops relaying tweets from a twitter user")]
        public async Task RemoveUserCommand(CommandContext ctx, [Description("The person's twitter username, e.g. 'Wrel'")] string username)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Check that the username conforms to Twitter's standards
            if (!Regex.IsMatch(username, "^[A-Za-z0-9_]{1,15}$"))
            {
                await ctx.RespondAsync("Invalid username! Twitter usernames can only contain letters and numbers, and must be between 1-15 characters long").ConfigureAwait(false);
                return;
            }

            // Get the user info from Twitter
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

            // Get the stored Twitter user
            long twitterUserId = long.Parse(user.User.Id);
            TwitterUser dbUser = await DbContext.TwitterUsers
                .Include(e => e.Guilds)
                .FirstOrDefaultAsync(e => e.UserId == twitterUserId).ConfigureAwait(false);

            if (dbUser == default)
            {
                await ctx.RespondAsync("You aren't relaying tweets from this user").ConfigureAwait(false);
                return;
            }

            if (settings.TwitterUsers.Contains(dbUser))
            {
                settings.TwitterUsers.Remove(dbUser);
                await ctx.RespondAsync("Tweets from this user are no longer being relayed").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync("You aren't relaying tweets from this user").ConfigureAwait(false);
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            // Remove the user if there are no longer any guild relaying tweets from them
            if (dbUser.Guilds.Count == 0)
                DbContext.Remove(dbUser);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [Command("list-users")]
        [Description("Lists all the users who's tweets are being relayed into your guild")]
        public async Task ListUsersCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Find the settings record for the calling guild
            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings
                .Include(e => e.TwitterUsers)
                .FirstOrDefaultAsync(e => e.GuildId == ctx.Guild.Id)
                .ConfigureAwait(false);

            if (settings == default || settings.TwitterUsers.Count == 0)
            {
                await ctx.RespondAsync("You aren't relaying tweets from any users").ConfigureAwait(false);
            } else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Tweets are being relayed from the following users:");

                foreach (TwitterUser user in settings.TwitterUsers)
                {
                    UserV2Response actualUser = await TwitterClient.UsersV2.GetUserByIdAsync(user.UserId).ConfigureAwait(false);
                    if (actualUser.User is null)
                        sb.Append("Invalid User (Recorded ID: ").Append(user.UserId).AppendLine(")");
                    else
                        sb.Append(actualUser.User.Username).Append(" (ID: ").Append(user.UserId).AppendLine(")");
                }

                await ctx.RespondAsync(sb.ToString()).ConfigureAwait(false);
            }
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
