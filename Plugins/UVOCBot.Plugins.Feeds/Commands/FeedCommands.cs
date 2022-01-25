using Remora.Commands.Attributes;
using Remora.Commands.Groups;
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
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Plugins.Feeds.Commands;

[Group("feed")]
[Description("Commands that manage external feeds")]
[RequireContext(ChannelContext.Guild)]
[RequireGuildPermission(DiscordPermission.ManageGuild, false)]
public class FeedCommands : CommandGroup
{
    private readonly ICommandContext _context;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public FeedCommands
    (
        ICommandContext context,
        IPermissionChecksService permissionChecksService,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context;
        _permissionChecksService = permissionChecksService;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    [Command("global-toggle")]
    [Description("Enables or disables all feeds.")]
    public async Task<IResult> EnabledCommandAsync(bool isEnabled)
    {
        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        if (isEnabled)
        {
            if (settings.FeedChannelID is null)
            {
                return await _feedbackService.SendContextualErrorAsync
                (
                    "You must set a feed channel to enable this feature.",
                    ct: CancellationToken
                ).ConfigureAwait(false);
            }

            Snowflake channelSnowflake = DiscordSnowflake.New(settings.FeedChannelID.Value);
            Result canLogToChannel = await CheckFeedChannelPermissions(channelSnowflake).ConfigureAwait(false);

            if (!canLogToChannel.IsSuccess)
                return canLogToChannel;
        }

        settings.IsEnabled = isEnabled;

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            "Feeds have been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("channel")]
    [Description("Selects the channel to which feeds will be relayed")]
    public async Task<IResult> SetFeedChannelCommandAsync
    (
        [ChannelTypes(ChannelType.GuildText, ChannelType.GuildPublicThread)] IChannel channel
    )
    {
        Result canPostToChannel = await CheckFeedChannelPermissions(channel.ID).ConfigureAwait(false);
        if (!canPostToChannel.IsSuccess)
            return canPostToChannel;

        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
        settings.FeedChannelID = channel.ID.Value; _dbContext.Update(settings);

        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            "I will now post feeds to " + Formatter.ChannelMention(channel.ID),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("toggle")]
    [Description("Enables or disables a particular feed.")]
    public async Task<IResult> ToggleFeedCommandAsync
    (
        [Description("The feed to toggle.")] Feed feed,
        [Description("Whether to enable or disable this feed.")] bool isEnabled
    )
    {
        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        if (isEnabled)
            settings.Feeds |= (ulong)feed;
        else
            settings.Feeds &= ~(ulong)feed;

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"Logging for the {feed} event has been " + (isEnabled ? "enabled" : "disabled"),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<Result> CheckFeedChannelPermissions(Snowflake channelId)
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
