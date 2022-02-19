using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Model;

namespace UVOCBot.Commands;

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
    private readonly FeedbackService _feedbackService;

    public AdminCommands
    (
        ICommandContext context,
        DiscordContext dbContext,
        IPermissionChecksService permissionsCheckService,
        FeedbackService feedbackService
    )
    {
        _context = context;
        _dbContext = dbContext;
        _permissionChecksService = permissionsCheckService;
        _feedbackService = feedbackService;
    }

    [Command("enable-logging")]
    [Description("Enables or disables admin logging.")]
    public async Task<IResult> EnabledCommand(bool isEnabled)
    {
        GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        if (isEnabled)
        {
            if (settings.LoggingChannelId is null)
            {
                return await _feedbackService.SendContextualErrorAsync
                (
                    "You must set a logging channel to enable admin logging.",
                    ct: CancellationToken
                ).ConfigureAwait(false);
            }

            Snowflake channelSnowflake = DiscordSnowflake.New(settings.LoggingChannelId.Value);
            Result canLogToChannel = await CheckLoggingChannelPermissions(channelSnowflake).ConfigureAwait(false);

            if (!canLogToChannel.IsSuccess)
                return canLogToChannel;
        }

        settings.IsLoggingEnabled = isEnabled;

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            "Admin logs have been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("logging-channel")]
    [Description("Sets the channel to post admin logs to.")]
    public async Task<IResult> SetLoggingChannelCommandAsync([ChannelTypes(ChannelType.GuildText)] IChannel channel)
    {
        Result canLogToChannel = await CheckLoggingChannelPermissions(channel.ID).ConfigureAwait(false);
        if (!canLogToChannel.IsSuccess)
            return canLogToChannel;

        GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
        settings.LoggingChannelId = channel.ID.Value;

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            "I will now post admin logs to " + Formatter.ChannelMention(channel.ID),
            ct: CancellationToken
        ).ConfigureAwait(false);
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

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"Logging for the {logType} event has been " + (isEnabled ? "enabled" : "disabled"),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<Result> CheckLoggingChannelPermissions(Snowflake channelId)
    {
        Result<IDiscordPermissionSet> permissions = await _permissionChecksService.GetPermissionsInChannel
        (
            channelId,
            DiscordConstants.UserId,
            CancellationToken
        ).ConfigureAwait(false);

        if (!permissions.IsSuccess)
            return Result.FromError(permissions);

        if (!permissions.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
            return new PermissionError(DiscordPermission.ViewChannel, DiscordConstants.UserId, channelId);

        if (!permissions.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
            return new PermissionError(DiscordPermission.SendMessages, DiscordConstants.UserId, channelId);

        return Result.FromSuccess();
    }
}
