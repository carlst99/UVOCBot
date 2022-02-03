using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Plugins.Feeds.Responders;

internal sealed class ToggleFeedComponentResponder : IComponentResponder
{
    private readonly DiscordContext _dbContext;
    private readonly InteractionContext _context;
    private readonly FeedbackService _feedbackService;

    public ToggleFeedComponentResponder
    (
        DiscordContext dbContext,
        InteractionContext context,
        FeedbackService feedbackService
    )
    {
        _dbContext = dbContext;
        _context = context;
        _feedbackService = feedbackService;
    }

    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
    {
        if (key != FeedComponentKeys.ToggleFeed)
            return Result.FromError(new GenericCommandError());

        if (!_context.Data.Values.IsDefined(out IReadOnlyList<string>? values))
            return Result.FromError(new GenericCommandError());

        GuildFeedsSettings settings = await _dbContext.FindOrDefaultAsync<GuildFeedsSettings>(_context.GuildID.Value.Value, ct).ConfigureAwait(false);
        Feed selectedFeeds = 0;
        string message = "The following feeds have been enabled:";

        foreach (string value in values)
        {
            if (!Enum.TryParse(value, out Feed feed))
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
