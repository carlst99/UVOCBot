using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Commands
{
    [Group("outfit")]
    public class OutfitTrackingCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly ICensusApiService _censusApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly FeedbackService _feedbackService;

        public OutfitTrackingCommands(
            ICommandContext context,
            DiscordContext dbContext,
            ICensusApiService censusApi,
            IPermissionChecksService permissionChecksService,
            FeedbackService feedbackService)
        {
            _context = context;
            _dbContext = dbContext;
            _feedbackService = feedbackService;
            _censusApi = censusApi;
            _permissionChecksService = permissionChecksService;
        }

        [Command("track")]
        [Description("Tracks an outfit's base captures.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> TrackOutfitCommandAsync(string outfitTag)
        {
            Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(outfitTag, CancellationToken).ConfigureAwait(false);

            if (!getOutfitResult.IsSuccess)
                return await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);
            if (getOutfitResult.Entity is null)
                return await _feedbackService.SendContextualErrorAsync("That outfit doesn't exist.", ct: CancellationToken).ConfigureAwait(false);

            Outfit outfit = getOutfitResult.Entity;
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            if (!settings.TrackedOutfits.Contains(outfit.OutfitId))
                settings.TrackedOutfits.Add(outfit.OutfitId);

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _feedbackService.SendContextualSuccessAsync(
                $"Now tracking [{ outfit.Alias }] { outfit.Name }",
                ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("untrack")]
        [Description("Removes a tracked outfit.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> UntrackOutfitCommandAsync(
            [Description("The 1-4 letter tag of the outfit to track.")] string outfitTag)
        {
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(outfitTag, CancellationToken).ConfigureAwait(false);
            if (!getOutfitResult.IsSuccess)
                return await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);

            /* We're not worrying about the outfit not existing here, as it isn't a huge concern to leave them sitting around
             * if the in-game outfit has been deleted.
             */
            if (getOutfitResult.Entity is not null && settings.TrackedOutfits.Contains(getOutfitResult.Entity.OutfitId))
                settings.TrackedOutfits.Remove(getOutfitResult.Entity.OutfitId);

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _feedbackService.SendContextualSuccessAsync("That outfit is no longer being tracked.", ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("base-capture-channel")]
        [Description("Sets the channel to post base capture notifications in for any tracked outfits.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> SetBaseCaptureChannelCommandAsync(
            [Description("The channel. Leave empty to disable base capture notifications.")] IChannel? channel = null)
        {
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            settings.BaseCaptureChannelId = null;

            if (channel is not null)
            {
                if (channel.Type != ChannelType.GuildText)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        Formatter.ChannelMention(channel) + " must be a text channel.",
                        ct: CancellationToken).ConfigureAwait(false);
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
}
