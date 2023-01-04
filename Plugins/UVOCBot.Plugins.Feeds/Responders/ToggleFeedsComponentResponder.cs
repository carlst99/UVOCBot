using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using UVOCBot.Discord.Core.Commands;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Feeds.Objects;

namespace UVOCBot.Plugins.Feeds.Responders;

internal sealed class ToggleFeedComponentResponder : IComponentResponder
{
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly DiscordContext _dbContext;
    private readonly IInteraction _context;
    private readonly FeedbackService _feedbackService;

    public ToggleFeedComponentResponder
    (
        IPermissionChecksService permissionChecksService,
        DiscordContext dbContext,
        IInteractionContext context,
        FeedbackService feedbackService
    )
    {
        _permissionChecksService = permissionChecksService;
        _dbContext = dbContext;
        _context = context.Interaction;
        _feedbackService = feedbackService;
    }

    /// <inheritdoc />
    public Result<Attribute[]> GetResponseAttributes(string key)
        => Array.Empty<Attribute>();

    /// <inheritdoc />
    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
    {
        if (key != FeedComponentKeys.ToggleFeed)
            return Result.FromError(new GenericCommandError());

        if (!_context.Data.HasValue)
            return Result.FromSuccess();

        if (!_context.Data.Value.TryPickT1(out IMessageComponentData componentData, out _)
            || !componentData.Values.IsDefined(out IReadOnlyList<ISelectOption>? values))
            return Result.FromError(new GenericCommandError());

        if (!_context.TryGetUser(out IUser? user))
            return Result.FromSuccess();

        Result<IDiscordPermissionSet> permissionsResult = await _permissionChecksService
            .GetPermissionsInChannel(_context.ChannelID.Value, user.ID, ct);
        if (!permissionsResult.IsDefined(out IDiscordPermissionSet? permissions))
            return permissionsResult;

        if (!permissions.HasAdminOrPermission(DiscordPermission.ManageGuild))
            return Result.FromError(new PermissionError(DiscordPermission.ManageGuild, user.ID, _context.ChannelID.Value));

        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, ct).ConfigureAwait(false);
        Feed selectedFeeds = 0;
        string message = "The following feeds have been enabled:";

        foreach (ISelectOption value in values)
        {
            if (!Enum.TryParse(value.Value, out Feed feed))
                continue;

            selectedFeeds |= feed;
            message += "\n- " + FeedDescriptions.Get[feed];
        }

        settings.Feeds = (ulong)selectedFeeds;

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        await _feedbackService.SendContextualSuccessAsync(message, ct: ct);

        return Result.FromSuccess();
    }
}
