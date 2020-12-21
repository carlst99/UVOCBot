using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models.V2;
using UVOCBot.Core.Model;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    [Group("twitter")]
    [Aliases("tweet")]
    [Description("Commands pertinent to the twitter relay functionality")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [RequireUserPermissions(Permissions.ManageGuild)]
    [RequireGuild]
    public class TwitterModule : BaseCommandModule
    {
        private const string ENV_CLIENT_ID = "UVOCBOT_CLIENT_ID";
        public const string TWITTER_USERNAME_REGEX = "^[A-Za-z0-9_]{1,15}$";

        public ITwitterClient TwitterClient { private get; set; }

        public IBotApi DbApi { get; set; }

        [Command("add-user")]
        [Aliases("add")]
        [Description("Starts relaying tweets from a twitter user")]
        public async Task AddUserCommand(CommandContext ctx, [Description("The person's twitter username, e.g. 'Wrel'")] string username)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Check that the username conforms to Twitters standards
            if (!Regex.IsMatch(username, TWITTER_USERNAME_REGEX))
            {
                await ctx.RespondAsync("Invalid username! Twitter usernames can only contain letters and numbers, and must be between 1-15 characters long").ConfigureAwait(false);
                return;
            }

            UserV2Response user = await GetTwitterUserByUsernameAsync(ctx, username).ConfigureAwait(false);
            if (user is null)
                return;

            // Find or create the guild twitter settings record
            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);

            if (settings == default)
            {
                settings = new GuildTwitterSettingsDTO(ctx.Guild.Id);
                await DbApi.CreateGuildTwitterSettings(settings).ConfigureAwait(false);
            }

            long twitterUserId = long.Parse(user.User.Id);

            // Find or create the twitter user record
            TwitterUserDTO twitterUser = await DbApi.GetTwitterUser(twitterUserId).ConfigureAwait(false);
            if (twitterUser == default)
            {
                twitterUser = new TwitterUserDTO(twitterUserId);
                await DbApi.CreateTwitterUser(twitterUser).ConfigureAwait(false);
            }

            if (!settings.TwitterUsers.Contains(twitterUser.UserId))
                await DbApi.CreateGuildTwitterLink(settings, twitterUser).ConfigureAwait(false);

            await ctx.RespondAsync($"Now relaying tweets from **{username}**!").ConfigureAwait(false);

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
            if (!Regex.IsMatch(username, TWITTER_USERNAME_REGEX))
            {
                await ctx.RespondAsync("Invalid username! Twitter usernames can only contain letters and numbers, and must be between 1-15 characters long").ConfigureAwait(false);
                return;
            }

            UserV2Response user = await GetTwitterUserByUsernameAsync(ctx, username).ConfigureAwait(false);
            if (user is null)
                return;

            // Find or create the guild twitter settings record
            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                await ctx.RespondAsync($"You aren't relaying tweets from **{username}**").ConfigureAwait(false);
                return;
            }

            // Get the stored Twitter user info
            long twitterUserId = long.Parse(user.User.Id);
            TwitterUserDTO twitterUser = await DbApi.GetTwitterUser(twitterUserId).ConfigureAwait(false);

            if (twitterUser == default)
            {
                await ctx.RespondAsync($"You aren't relaying tweets from **{username}**").ConfigureAwait(false);
                return;
            }

            if (settings.TwitterUsers.Contains(twitterUser.UserId))
            {
                await DbApi.DeleteGuildTwitterLink(settings, twitterUser).ConfigureAwait(false);
                await ctx.RespondAsync($"Tweets from **{username}** are no longer being relayed").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync($"You aren't relaying tweets from **{username}**").ConfigureAwait(false);
            }
        }

        [Command("list-users")]
        [Aliases("list", "users", "all")]
        [Description("Lists all the users who's tweets are being relayed into your guild")]
        public async Task ListUsersCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Find the settings record for the calling guild
            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);

            if (settings == default || settings.TwitterUsers.Count == 0)
            {
                await ctx.RespondAsync("You aren't relaying tweets from any users").ConfigureAwait(false);
            } else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Tweets are being relayed from the following users:");

                foreach (long twitterUserId in settings.TwitterUsers)
                {
                    // Get the user info from Twitter
                    UserV2Response actualUser;
                    try
                    {
                        actualUser = await TwitterClient.UsersV2.GetUserByIdAsync(twitterUserId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Could not get twitter user information");
                        await ctx.RespondAsync("Sorry, an error occured! Please try again").ConfigureAwait(false);
                        return;
                    }
                    if (actualUser.User is null)
                        sb.Append("Invalid User (Recorded ID: ").Append(twitterUserId).AppendLine(")");
                    else
                        sb.Append(actualUser.User.Username).Append(" (ID: ").Append(twitterUserId).AppendLine(")");
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

            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                await ctx.RespondAsync("A relay channel has not yet been set").ConfigureAwait(false);
            }
            else
            {
                try
                {
                    DiscordChannel channel = ctx.Guild.GetChannel((ulong)settings.RelayChannelId);
                    if (channel is not null)
                        await ctx.RespondAsync($"Tweets are being relayed to {channel.Mention}").ConfigureAwait(false);
                    else
                        await ctx.RespondAsync($"Your relay channel no longer exists. Please reset it using the `{Program.PREFIX}twitter relay-channel` command").ConfigureAwait(false);
                } catch (Exception)
                {
                    await ctx.RespondAsync("Failed to get your relay channel. Perhaps try resetting it").ConfigureAwait(false);
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

            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettingsDTO(ctx.Guild.Id)
                {
                    RelayChannelId = channel.Id
                };
                await DbApi.CreateGuildTwitterSettings(settings).ConfigureAwait(false);
            }
            else
            {
                settings.RelayChannelId = channel.Id;
                await DbApi.UpdateGuildTwitterSetting(settings).ConfigureAwait(false);
            }

            await ctx.RespondAsync("Tweets will now be relayed to " + channel.Mention).ConfigureAwait(false);
        }

        [Command("disable")]
        [Description("Disables the twitter relay feature")]
        public async Task DisableCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettingsDTO(ctx.Guild.Id)
                {
                    IsEnabled = false
                };
                await DbApi.CreateGuildTwitterSettings(settings).ConfigureAwait(false);
            }
            else
            {
                settings.IsEnabled = false;
                await DbApi.UpdateGuildTwitterSetting(settings).ConfigureAwait(false);
            }

            await ctx.RespondAsync("Tweet relaying is now **disabled**").ConfigureAwait(false);
        }

        [Command("enable")]
        [Description("Enables the twitter relay feature")]
        public async Task EnableCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                settings = new GuildTwitterSettingsDTO(ctx.Guild.Id)
                {
                    IsEnabled = true
                };
                await DbApi.CreateGuildTwitterSettings(settings).ConfigureAwait(false);
            }
            else
            {
                settings.IsEnabled = true;
                await DbApi.UpdateGuildTwitterSetting(settings).ConfigureAwait(false);
            }

            await ctx.RespondAsync("Tweet relaying is now **enabled**").ConfigureAwait(false);
        }

        [Command("status")]
        [Description("Gets the current status of the tweet relay feature")]
        public async Task StatusCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            GuildTwitterSettingsDTO settings = await DbApi.GetGuildTwitterSetting(ctx.Guild.Id).ConfigureAwait(false);
            if (settings == default)
            {
                await ctx.RespondAsync("Tweet relaying has not been setup").ConfigureAwait(false);
            }
            else
            {
                StringBuilder sb = new StringBuilder("Tweet relaying is ");
                if (settings.IsEnabled)
                    sb.AppendLine("**enabled**");
                else
                    sb.AppendLine("**disabled**");

                DiscordChannel channel = ctx.Guild.GetChannel((ulong)settings.RelayChannelId);
                if (channel is not null)
                    sb.Append("Tweets are being relayed to ").Append(channel.Mention);
                else
                    sb.Append("Your relay channel no longer exists. Please reset it using the `").Append(Program.PREFIX).Append("twitter relay-channel` command");

                await ctx.RespondAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetches a twitter user
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="username">The username of the twitter user</param>
        /// <returns></returns>
        private async Task<UserV2Response> GetTwitterUserByUsernameAsync(CommandContext ctx, string username)
        {
            // Get the user info from Twitter
            UserV2Response user;
            try
            {
                user = await TwitterClient.UsersV2.GetUserByNameAsync(username).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get twitter user information");
                await ctx.RespondAsync("Sorry, an error occurred! Please try again").ConfigureAwait(false);
                return null;
            }

            if (user.User is null)
            {
                await ctx.RespondAsync("That Twitter user doesn't exist!").ConfigureAwait(false);
                return null;
            }

            return user;
        }
    }
}
