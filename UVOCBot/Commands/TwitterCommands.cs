using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models.V2;
using UVOCBot.Core.Model;
using UVOCBot.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("twitter")]
    [Description("Commands that help with Twitter post relaying")]
    [RequireContext(ChannelContext.Guild)]
    [RequireUserGuildPermission(DiscordPermission.ManageGuild)]
    public class TwitterCommands : CommandGroup
    {
        private const string TWITTER_USERNAME_REGEX = "^[A-Za-z0-9_]{1,15}$";

        private readonly ILogger<TwitterCommands> _logger;
        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IDbApiService _dbApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly ITwitterClient _twitterClient;

        public TwitterCommands(
            ILogger<TwitterCommands> logger,
            ICommandContext context,
            MessageResponseHelpers responder,
            IDbApiService dbAPI,
            IPermissionChecksService permissionChecksService,
            ITwitterClient twitterClient)
        {
            _logger = logger;
            _context = context;
            _responder = responder;
            _dbApi = dbAPI;
            _permissionChecksService = permissionChecksService;
            _twitterClient = twitterClient;
        }

        [Command("add-user")]
        [Description("Adds a Twitter user to relay tweets from")]
        public async Task<IResult> AddUserCommandAsync([Description("The person's twitter username, e.g. 'Wrel'")] string username)
        {
            // Check that the username conforms to Twitters standards
            if (!Regex.IsMatch(username, TWITTER_USERNAME_REGEX))
                return await _responder.RespondWithErrorAsync(_context, "That user doesn't exist!", CancellationToken).ConfigureAwait(false);

            Result<UserV2Response> user = await GetTwitterUserByUsernameAsync(username).ConfigureAwait(false);
            if (!user.IsSuccess)
                return user;

            // Find or create the guild twitter settings record
            Result<GuildTwitterSettingsDTO> settings = await _dbApi.GetGuildTwitterSettingsAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            // Find or create the twitter user record
            Result<TwitterUserDTO> twitterUser = await _dbApi.GetTwitterUserAsync(long.Parse(user.Entity.User.Id)).ConfigureAwait(false);
            if (!twitterUser.IsSuccess)
            {
                if (twitterUser.Error is HttpStatusCodeError er && er.StatusCode == System.Net.HttpStatusCode.NotFound)
                    twitterUser = await _dbApi.CreateTwitterUserAsync(new TwitterUserDTO(long.Parse(user.Entity.User.Id)), CancellationToken).ConfigureAwait(false);

                if (!twitterUser.IsSuccess)
                    await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);
            }

            if (!settings.Entity.TwitterUsers.Contains(twitterUser.Entity.UserId))
            {
                Result linkCreationResult = await _dbApi.CreateGuildTwitterLinkAsync(settings.Entity.GuildId, twitterUser.Entity.UserId).ConfigureAwait(false);
                if (!linkCreationResult.IsSuccess)
                    await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);
            }

            if (settings.Entity.RelayChannelId is null)
            {
                await _responder.RespondWithErrorAsync(
                    _context,
                    $"You haven't set a channel to relay tweets to. Please use the { Formatter.InlineQuote("twitter relay-channel") } command",
                    CancellationToken).ConfigureAwait(false);
            }

            return await _responder.RespondWithSuccessAsync(_context, $"Tweets from { Formatter.Bold(username) } will now be relayed", CancellationToken).ConfigureAwait(false);
        }

        [Command("remove-user")]
        [Description("Stops relaying tweets from a twitter user")]
        public async Task<IResult> RemoveUserCommandAsync([Description("The person's twitter username, e.g. 'Wrel'")] string username)
        {
            // Check that the username conforms to Twitter's standards
            if (!Regex.IsMatch(username, TWITTER_USERNAME_REGEX))
                return await _responder.RespondWithErrorAsync(_context, "That user doesn't exist!", CancellationToken).ConfigureAwait(false);

            Result<UserV2Response> user = await GetTwitterUserByUsernameAsync(username).ConfigureAwait(false);
            if (!user.IsSuccess)
                return user;

            long twitterUserId = long.Parse(user.Entity.User.Id);
            Result<GuildTwitterSettingsDTO> settings = await _dbApi.GetGuildTwitterSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            if (settings.Entity.TwitterUsers.Contains(twitterUserId))
            {
                Result linkDeletionResult = await _dbApi.DeleteGuildTwitterLinkAsync(settings.Entity.GuildId, twitterUserId).ConfigureAwait(false);
                if (!linkDeletionResult.IsSuccess)
                    return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);
                else
                    return await _responder.RespondWithSuccessAsync(_context, $"Tweets from { Formatter.Bold(username) } are no longer being relayed", CancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await _responder.RespondWithErrorAsync(_context, $"You weren't relaying tweets from { Formatter.Bold(username) }", CancellationToken).ConfigureAwait(false);
            }
        }

        [Command("list-users")]
        [Description("Lists all the users who's tweets are being relayed into your guild")]
        public async Task<IResult> ListUsersCommandAsync()
        {
            // Find the settings record for the calling guild
            Result<GuildTwitterSettingsDTO> settings = await _dbApi.GetGuildTwitterSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            if (settings.Entity.TwitterUsers.Count == 0)
            {
                return await _responder.RespondWithSuccessAsync(_context, "You aren't relaying tweets from any users", CancellationToken).ConfigureAwait(false);
            }
            else
            {
                StringBuilder sb = new();
                sb.AppendLine("Tweets are being relayed from the following users:");

                foreach (long twitterUserId in settings.Entity.TwitterUsers)
                {
                    // Get the user info from Twitter
                    UserV2Response? actualUser = null;
                    try
                    {
                        actualUser = await _twitterClient.UsersV2.GetUserByIdAsync(twitterUserId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not get twitter user information");
                    }

                    if (actualUser is null || actualUser.User is null)
                        sb.Append("Invalid User (Recorded ID: ").Append(twitterUserId).AppendLine(")");
                    else
                        sb.Append(actualUser.User.Username).Append(" (ID: ").Append(twitterUserId).AppendLine(")");
                }

                return await _responder.RespondWithSuccessAsync(_context, sb.ToString(), CancellationToken).ConfigureAwait(false);
            }
        }

        [Command("relay-channel")]
        [Description("Selects the channel to which tweets will be relayed")]
        public async Task<IResult> RelayChannelCommand([Description("The channel to relay tweets to")] IChannel channel)
        {
            Result<IDiscordPermissionSet> botPermissions = await _permissionChecksService.PermissionsInChannel(channel.ID, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!botPermissions.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again later", CancellationToken).ConfigureAwait(false);

            if (!botPermissions.Entity.HasPermission(DiscordPermission.ViewChannel) || !botPermissions.Entity.HasPermission(DiscordPermission.SendMessages))
            {
                return await _responder.RespondWithErrorAsync(
                    _context,
                    $"I need permissions to view { Formatter.ChannelMention(channel.ID) }, and send messages to it. Your relay channel has { Formatter.Bold("not") } been updated",
                    CancellationToken).ConfigureAwait(false);
            }

            Result<GuildTwitterSettingsDTO> settings = await _dbApi.GetGuildTwitterSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            settings.Entity.RelayChannelId = channel.ID.Value;
            Result updateResult = await _dbApi.UpdateGuildTwitterSettingsAsync(settings.Entity.GuildId, settings.Entity).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            return await _responder.RespondWithSuccessAsync(_context, "Tweets will now be relayed to " + Formatter.ChannelMention(channel.ID), CancellationToken).ConfigureAwait(false);
        }

        [Command("relaying-enabled")]
        [Description("Lets you enable or disable tweet relaying")]
        public async Task<IResult> RelayingEnabledCommandAsync([Description("Whether tweets should be relayed")] bool isEnabled)
        {
            Result<GuildTwitterSettingsDTO> settings = await _dbApi.GetGuildTwitterSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            settings.Entity.IsEnabled = isEnabled;
            Result updateResult = await _dbApi.UpdateGuildTwitterSettingsAsync(settings.Entity.GuildId, settings.Entity).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            return await _responder.RespondWithSuccessAsync(_context, "Tweet relaying is now " + Formatter.Bold(isEnabled ? "enabled" : "disabled"), CancellationToken).ConfigureAwait(false);
        }

        [Command("status")]
        [Description("Gets the current status of the tweet relay feature")]
        public async Task<IResult> StatusCommandAsync()
        {
            Result<GuildTwitterSettingsDTO> settings = await _dbApi.GetGuildTwitterSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again!", CancellationToken).ConfigureAwait(false);

            StringBuilder sb = new("Tweet relaying is ");

            if (settings.Entity.IsEnabled)
                sb.AppendLine(Formatter.Bold("enabled"));
            else
                sb.AppendLine(Formatter.Bold("disabled"));

            if (settings.Entity.RelayChannelId is null)
                sb.AppendLine("You have not set a relay channel");
            else
                sb.Append("Tweets are being relayed to ").AppendLine(Formatter.ChannelMention(settings.Entity.RelayChannelId.Value));

            return await _responder.RespondWithSuccessAsync(_context, sb.ToString(), CancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches a twitter user
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="username">The username of the twitter user</param>
        /// <returns></returns>
        private async Task<Result<UserV2Response>> GetTwitterUserByUsernameAsync(string username)
        {
            UserV2Response user;
            try
            {
                user = await _twitterClient.UsersV2.GetUserByNameAsync(username).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get Twitter user information");
                await _responder.RespondWithErrorAsync(_context, "Sorry, an error occurred! Please try again later", ct: CancellationToken).ConfigureAwait(false);
                return Result<UserV2Response>.FromError(ex);
            }

            if (user.User is null)
            {
                await _responder.RespondWithErrorAsync(_context, "That user doesn't exist!", ct: CancellationToken).ConfigureAwait(false);
                return Result<UserV2Response>.FromError(new ArgumentException("Username does not exist"));
            }

            return user;
        }
    }
}
