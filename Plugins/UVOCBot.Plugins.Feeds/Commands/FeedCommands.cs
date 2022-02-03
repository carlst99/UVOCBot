using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public FeedCommands
    (
        ICommandContext context,
        IDiscordRestGuildAPI guildApi,
        IPermissionChecksService permissionChecksService,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context;
        _guildApi = guildApi;
        _permissionChecksService = permissionChecksService;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    [Command("global-toggle")]
    [Description("Enables or disables the feed functionality.")]
    public async Task<IResult> EnabledCommandAsync(bool isEnabled)
    {
        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        if (isEnabled)
        {
            Result validChannel = await CheckValidFeedChannelAsync(settings);
            if (!validChannel.IsSuccess)
                return validChannel;
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
        Result canPostToChannel = await CheckFeedChannelPermissionsAsync(channel.ID).ConfigureAwait(false);
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

    [Command("list")]
    [Description("Lists the available feeds, and their enabled status.")]
    public async Task<IResult> ListFeedsCommandAsync()
    {
        static string GetEnabledEmoji(bool value)
               => value
                   ? ":ballot_box_with_check:"
                   : ":x:";

        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
        Feed[] values = Enum.GetValues<Feed>();

        string message = Formatter.Bold("Globally enabled: ") + GetEnabledEmoji(settings.IsEnabled);

        message += "\n\n" + Formatter.Bold("Feed channel: ") + (settings.FeedChannelID.HasValue ? Formatter.ChannelMention(settings.FeedChannelID.Value) : "not set");

        message += "\n\n" + Formatter.Bold("Feeds:");
        foreach (Feed f in values)
        {
            string emoji = GetEnabledEmoji(((Feed)settings.Feeds & f) != 0);
            message += $"\n- {FeedDescriptions.Get[f]} {emoji}";
        }

        return await _feedbackService.SendContextualInfoAsync
        (
            message,
            ct: CancellationToken
        );
    }

    [Command("status")]
    [Description("Gets the current status of the feed function.")]
    public async Task<IResult> GetFeedStatusCommandAsync()
        => await ListFeedsCommandAsync();

    [Command("toggle")]
    [Description("Enables or disables a particular feed.")]
    public async Task<IResult> ToggleFeedCommandAsync()
    {
        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        Result validChannel = await CheckValidFeedChannelAsync(settings);
        if (!validChannel.IsSuccess)
            return validChannel;

        Feed[] feedValues = Enum.GetValues<Feed>();
        List<SelectOption> selectOptions = feedValues.Select
            (
                f => new SelectOption
                (
                    FeedDescriptions.Get[f],
                    f.ToString(),
                    IsDefault: ((Feed)settings.Feeds & f) != 0
                )
            )
            .ToList();

        SelectMenuComponent menu = new
        (
            FeedComponentKeys.ToggleFeed,
            selectOptions,
            MinValues: 0,
            MaxValues: feedValues.Length
        );

        return await _feedbackService.SendContextualInfoAsync
        (
            "Select feeds to toggle",
            options: new FeedbackMessageOptions
            (
                MessageComponents: new[] { new ActionRowComponent(new[] { menu }) }
            ),
            ct: CancellationToken
        );
    }

    private async Task<Result> CheckFeedChannelPermissionsAsync(Snowflake channelId)
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

    private async Task<Result> CheckValidFeedChannelAsync(GuildFeedsSettings settings)
    {
        if (settings.FeedChannelID is null)
            return new GenericCommandError("You must set a feed channel to enable this feature.");

        Result<IReadOnlyList<IChannel>> channelsResult = await _guildApi.GetGuildChannelsAsync(DiscordSnowflake.New(settings.GuildId), CancellationToken);
        if (!channelsResult.IsDefined(out IReadOnlyList<IChannel>? channels))
            return Result.FromError(channelsResult);

        if (!channels.Any(c => c.ID.Value == settings.FeedChannelID))
            return new GenericCommandError("Your selected feed channel no longer exists. Please reset it.");

        Snowflake channelSnowflake = DiscordSnowflake.New(settings.FeedChannelID.Value);
        return await CheckFeedChannelPermissionsAsync(channelSnowflake).ConfigureAwait(false);
    }
}
