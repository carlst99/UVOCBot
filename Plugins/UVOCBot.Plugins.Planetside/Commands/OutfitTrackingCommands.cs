using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Extensions;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.Commands;

[Group("outfit")]
[RequireContext(ChannelContext.Guild)]
public class OutfitTrackingCommands : CommandGroup
{
    private readonly IInteraction _context;
    private readonly DiscordContext _dbContext;
    private readonly ICensusApiService _censusApi;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly FeedbackService _feedbackService;

    public OutfitTrackingCommands
    (
        IInteractionContext context,
        DiscordContext dbContext,
        ICensusApiService censusApi,
        IPermissionChecksService permissionChecksService,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
        _censusApi = censusApi;
        _permissionChecksService = permissionChecksService;
    }

    [Command("track")]
    [Description("Tracks an outfit's base captures.")]
    [RequireGuildPermission(DiscordPermission.ManageGuild, IncludeSelf = false)]
    [Deferred]
    public async Task<Result> TrackOutfitCommandAsync(string outfitTag)
    {
        Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(outfitTag, CancellationToken);
        if (!getOutfitResult.IsSuccess)
            return Result.FromError(getOutfitResult);

        if (getOutfitResult.Entity is null)
            return await _feedbackService.SendContextualErrorAsync("That outfit doesn't exist.", ct: CancellationToken);

        Outfit outfit = getOutfitResult.Entity;
        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>
        (
            _context.GuildID.Value.Value,
            ct: CancellationToken
        );

        if (!settings.TrackedOutfits.Contains(outfit.OutfitId))
            settings.TrackedOutfits.Add(outfit.OutfitId);

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken);

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"Now tracking [{ outfit.Alias }] { outfit.Name }",
            ct: CancellationToken
        );
    }

    [Command("untrack")]
    [Description("Removes a tracked outfit.")]
    [RequireGuildPermission(DiscordPermission.ManageGuild, IncludeSelf = false)]
    [Deferred]
    public async Task<Result> UntrackOutfitCommandAsync
    (
        [Description("The 1-4 letter tag of the outfit to track.")] string outfitTag
    )
    {
        PlanetsideSettings? settings = await _dbContext.FindAsync<PlanetsideSettings>
        (
            _context.GuildID.Value.Value,
            CancellationToken
        ).ConfigureAwait(false);

        if (settings is null)
        {
            return await _feedbackService.SendContextualInfoAsync
            (
                "You are not tracking any guilds",
                ct: CancellationToken
            );
        }

        Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(outfitTag, CancellationToken).ConfigureAwait(false);
        if (!getOutfitResult.IsSuccess)
            return Result.FromError(getOutfitResult);

        if (getOutfitResult.Entity is null)
            return await _feedbackService.SendContextualErrorAsync("That outfit doesn't exist.", ct: CancellationToken);

        /* We're not worrying about the outfit not existing here, as it isn't a huge concern to leave them sitting around
         * if the in-game outfit has been deleted.
         */
        if (settings.TrackedOutfits.Contains(getOutfitResult.Entity.OutfitId))
            settings.TrackedOutfits.Remove(getOutfitResult.Entity.OutfitId);

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            "That outfit is no longer being tracked.",
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("list-tracked")]
    [Description("Lists the outfits that are being tracked.")]
    [Ephemeral]
    public async Task<Result> ListTrackedOutfitsCommandAsync()
    {
        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>
        (
            _context.GuildID.Value.Value,
            false,
            CancellationToken
        );

        Result<List<Outfit>> outfitsResult = await _censusApi.GetOutfitsAsync(settings.TrackedOutfits, CancellationToken);
        if (!outfitsResult.IsDefined(out List<Outfit>? outfits))
            return Result.FromError(outfitsResult);

        StringBuilder outfitListBuilder = new StringBuilder()
            .AppendLine("Currently tracked outfits:")
            .AppendLine();

        foreach (Outfit outfit in outfits)
        {
            outfitListBuilder.Append(Formatter.Bold('[' + outfit.Alias + ']'))
                .Append(' ')
                .AppendLine(outfit.Name);
        }

        return await _feedbackService.SendContextualInfoAsync
        (
            outfitListBuilder.ToString(),
            ct: CancellationToken
        );
    }

    [Command("base-capture-channel")]
    [Description("Sets the channel to post base capture notifications in for any tracked outfits.")]
    [RequireGuildPermission(DiscordPermission.ManageGuild, IncludeSelf = false)]
    public async Task<IResult> SetBaseCaptureChannelCommandAsync
    (
        [Description("The channel. Leave empty to disable base capture notifications.")]
        [ChannelTypes(ChannelType.GuildText, ChannelType.PublicThread)]
        IChannel? channel = null
    )
    {
        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>
        (
            _context.GuildID.Value.Value,
            ct: CancellationToken
        ).ConfigureAwait(false);

        settings.BaseCaptureChannelId = null;

        if (channel is not null)
        {
            if (channel.Type != ChannelType.GuildText)
            {
                return await _feedbackService.SendContextualErrorAsync
                (
                    Formatter.ChannelMention(channel) + " must be a text channel.",
                    ct: CancellationToken
                );
            }

            Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel(
                channel,
                DiscordConstants.UserId,
                CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.IsSuccess)
            {
                await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);
                return getPermissionSet;
            }

            if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "I do not have permission to view " + Formatter.ChannelMention(channel),
                    ct: CancellationToken).ConfigureAwait(false);
            }

            if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "I do not have permission to send messages in " + Formatter.ChannelMention(channel),
                    ct: CancellationToken).ConfigureAwait(false);
            }

            settings.BaseCaptureChannelId = channel.ID.Value;
        }

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        string message = channel is null ? "Base capture notifications have been disabled" : "Base capture notifications will now be sent to " + Formatter.ChannelMention(channel);
        return await _feedbackService.SendContextualSuccessAsync(message, ct: CancellationToken).ConfigureAwait(false);
    }
}
