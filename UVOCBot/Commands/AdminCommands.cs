using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("admin")]
    [Description("Administorial commands.")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
    [Ephemeral]
    public class AdminCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _replyService;

        public AdminCommands(ICommandContext context, DiscordContext dbContext, IPermissionChecksService permissionsCheckService, IReplyService replyService)
        {
            _context = context;
            _dbContext = dbContext;
            _permissionChecksService = permissionsCheckService;
            _replyService = replyService;
        }

        [Command("enable-logging")]
        [Description("Enables or disables admin logging.")]
        public async Task<IResult> EnabledCommand(bool isEnabled)
        {
            GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            if (isEnabled)
            {
                if (settings.LoggingChannelId is null)
                    return await _replyService.RespondWithUserErrorAsync("You must set a logging channel to enable admin logging.", CancellationToken).ConfigureAwait(false);

                Result canLogToChannel = await CheckLoggingChannelPermissions(new Snowflake(settings.LoggingChannelId.Value)).ConfigureAwait(false);
                if (!canLogToChannel.IsSuccess)
                    return Result.FromSuccess();
            }

            settings.IsLoggingEnabled = isEnabled;

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync("Admin logs have been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."), CancellationToken).ConfigureAwait(false);
        }

        [Command("logging-channel")]
        [Description("Sets the channel to post admin logs to.")]
        public async Task<IResult> SetLoggingChannelCommandAsync(IChannel channel)
        {
            GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            Result canLogToChannel = await CheckLoggingChannelPermissions(channel.ID).ConfigureAwait(false);
            if (!canLogToChannel.IsSuccess)
                return Result.FromSuccess();

            settings.LoggingChannelId = channel.ID.Value;

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync("I will now post admin logs to " + Formatter.ChannelMention(channel.ID), CancellationToken).ConfigureAwait(false);
        }

        [Command("toggle-log")]
        [Description("Enables or disables a particular log.")]
        public async Task<IResult> ToggleLog(
            [Description("The type of log to toggle.")] AdminLogTypes logType,
            [Description("Enables or disabled this log.")] bool isEnabled)
        {
            GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            if (isEnabled)
                settings.LogTypes |= (ulong)logType;
            else
                settings.LogTypes &= ~(ulong)logType;

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync($"Logging for the {logType} event has been " + (isEnabled ? "enabled" : "disabled"), CancellationToken).ConfigureAwait(false);
        }

        private async Task<Result> CheckLoggingChannelPermissions(Snowflake channelId)
        {
            Result<IDiscordPermissionSet> permissions = await _permissionChecksService.GetPermissionsInChannel(channelId, DiscordConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!permissions.IsSuccess)
            {
                await _replyService.RespondWithUserErrorAsync(permissions.Error.Message, CancellationToken).ConfigureAwait(false);
                return new Exception();
            }

            if (!permissions.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
            {
                await _replyService.RespondWithUserErrorAsync("I need permission to view the logging channel.", CancellationToken).ConfigureAwait(false);
                return new Exception();
            }

            if (!permissions.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
            {
                await _replyService.RespondWithUserErrorAsync("I need permission to send messages in the logging channel.", CancellationToken).ConfigureAwait(false);
                return new Exception();
            }

            return Result.FromSuccess();
        }
    }
}
