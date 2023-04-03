using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;

namespace UVOCBot.Plugins.SpaceEngineers.Commands;

[Group("se")]
[Description("Space Engineers Commands")]
[RequireContext(ChannelContext.Guild)]
[Deferred]
public class SpaceEngineersCommands : CommandGroup
{
    private readonly IInteraction _context;
    private readonly IVRageRemoteApi _remoteApi;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public SpaceEngineersCommands
    (
        IInteractionContext context,
        IVRageRemoteApi remoteApi,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
        _remoteApi = remoteApi;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    [Command("ping")]
    [Description("Pings the server")]
    [Ephemeral]
    public async Task<Result> PingCommandAsync()
    {
        Result<bool> pingResult = await _remoteApi.PingAsync(CancellationToken);

        string message = pingResult is { IsSuccess: true, Entity: true }
            ? "online"
            : "offline";

        return (Result)await _feedbackService.SendContextualInfoAsync
        (
            $"The server is {message}",
            ct: CancellationToken
        );
    }
}
