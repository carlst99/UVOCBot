using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
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
    [RequireUserPermissions(Permissions.ManageGuild)]
    public class TwitterModule : BaseCommandModule
    {
        private const string ENV_CLIENT_ID = "UVOCBOT_CLIENT_ID";

        public ITwitterClient TwitterClient { private get; set; }
        public BotContext DbContext { private get; set; }

        [Command("add-user")]
        [Aliases("add")]
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

            await ctx.RespondAsync($"Now relaying tweets from **{username}!**").ConfigureAwait(false);

            if (settings.RelayChannelId is null)
                await ctx.RespondAsync($"You haven't set a channel to relay tweets to. Please use the `{Program.PREFIX}twitter relay-channel` command").ConfigureAwait(false);
        }

        [Command("remove-user")]
        [Aliases("remove")]
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
                await ctx.RespondAsync($"You aren't relaying tweets from **{username}**").ConfigureAwait(false);
                return;
            }

            if (settings.TwitterUsers.Contains(dbUser))
            {
                settings.TwitterUsers.Remove(dbUser);
                await ctx.RespondAsync($"Tweets from **{username}** are no longer being relayed").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync($"You aren't relaying tweets from **{username}**").ConfigureAwait(false);
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            // Remove the user if there are no longer any guild relaying tweets from them
            if (dbUser.Guilds.Count == 0)
                DbContext.Remove(dbUser);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [Command("list-users")]
        [Aliases("list", "users", "all")]
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
        [Aliases("channel", "relay")]
        [Description("Displays the channel to which tweets will be relayed")]
        public async Task RelayChannelCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings.FindAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                await ctx.RespondAsync("A relay channel has not yet been set").ConfigureAwait(false);
            }
            else
            {
                try
                {
                    DiscordChannel channel = ctx.Guild.GetChannel((ulong)settings.RelayChannelId);
                    await ctx.RespondAsync($"Tweets are being relayed to {channel.Mention}").ConfigureAwait(false);
                } catch (NotFoundException)
                {
                    await ctx.RespondAsync("An invalid channel is currently set as the relay endpoint. Please reset it").ConfigureAwait(false);
                }
            }
        }

        [Command("relay-channel")]
        [Description("Sets the channel to which tweets will be relayed")]
        public async Task RelayChannelCommand(CommandContext ctx, [Description("The channel that tweets should be relayed to")] DiscordChannel channel)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            ulong clientId = ulong.Parse(Environment.GetEnvironmentVariable(ENV_CLIENT_ID));
            DiscordMember botMember = await ctx.Guild.GetMemberAsync(clientId).ConfigureAwait(false);

            Permissions channelPerms = channel.PermissionsFor(botMember);

            if ((channelPerms & Permissions.SendMessages) == 0 || (channelPerms & Permissions.AccessChannels) == 0)
            {
                await ctx.RespondAsync($"{Program.NAME} needs permission to send messages to {channel.Mention}. Your relay channel has **not** been updated").ConfigureAwait(false);
                return;
            }

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

        [Command("disable")]
        [Description("Disables the twitter relay feature")]
        public async Task DisableCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings.FindAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettings(ctx.Guild.Id);
                DbContext.Add(settings);
            }

            settings.IsEnabled = false;
            await ctx.RespondAsync("Tweet relaying is now **disabled**").ConfigureAwait(false);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [Command("enable")]
        [Description("Enables the twitter relay feature")]
        public async Task EnableCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings.FindAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettings(ctx.Guild.Id);
                DbContext.Add(settings);
            }

            settings.IsEnabled = true;
            await ctx.RespondAsync("Tweet relaying is now **enabled**").ConfigureAwait(false);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [Command("status")]
        [Description("Gets the current status of the tweet relay feature")]
        public async Task StatusCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettings settings = await DbContext.GuildTwitterSettings.FindAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                await ctx.RespondAsync("Tweet relaying has not been setup").ConfigureAwait(false);
            } else
            {
                StringBuilder sb = new StringBuilder("Tweet relaying is ");
                if (settings.IsEnabled)
                    sb.AppendLine("**enabled**");
                else
                    sb.AppendLine("**disabled**");

                DiscordChannel relayChannel = ctx.Guild.GetChannel((ulong)settings.RelayChannelId);
                sb.Append("Tweets are being relayed to ").Append(relayChannel.Mention);

                await ctx.RespondAsync(sb.ToString()).ConfigureAwait(false);
            }
        }
    }
}
