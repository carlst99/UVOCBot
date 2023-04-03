using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;
using UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

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

    [Command("online")]
    [Description("Lists any online players")]
    [Ephemeral]
    public async Task<Result> OnlineCommandAsync()
    {
        Result<IReadOnlyList<Player>> playersResult = await _remoteApi.GetPlayersAsync(CancellationToken);
        if (!playersResult.IsDefined(out IReadOnlyList<Player>? players))
            return Result.FromError(playersResult);

        StringBuilder message = new();
        int playerCount = 0;
        IEnumerable<Player> filteredPlayers = players.Where(p => !string.IsNullOrEmpty(p.DisplayName))
            .OrderBy(p => p.FactionTag ?? "z")
            .ThenBy(p => p.DisplayName);

        foreach (Player player in filteredPlayers)
        {
            if (!string.IsNullOrEmpty(player.FactionTag))
                message.Append('[').Append(player.FactionTag).Append("] ");

            message.Append(Formatter.Bold(player.DisplayName!));

            if (player.PromoteLevel > 0)
            {
                message.Append(" (");
                for (int i = 0; i < player.PromoteLevel; i++)
                    message.Append("\\*");
                message.Append(')');
            }

            message.AppendLine();
            playerCount++;
        }

        Embed embed = new
        (
            $"Players Online: {playerCount}",
            Description: message.ToString()
        );

        return (Result)await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }
}
