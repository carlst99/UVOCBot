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
using UVOCBot.Core.Extensions;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;
using UVOCBot.Plugins.SpaceEngineers.Extensions;
using UVOCBot.Plugins.SpaceEngineers.Objects;
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
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public SpaceEngineersCommands
    (
        IInteractionContext context,
        IVRageRemoteApi remoteApi,
        IPermissionChecksService permissionChecksService,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
        _remoteApi = remoteApi;
        _permissionChecksService = permissionChecksService;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    [Command("connect")]
    [RequireGuildPermission(DiscordPermission.ManageGuild)]
    [Ephemeral]
    public async Task<Result> ConnectToServerCommandAsync(string serverAddress, int serverPort, string serverKey)
    {
        serverAddress = serverAddress.Replace("http://", string.Empty);
        SEServerConnectionDetails connectionDetails = new(serverAddress, serverPort, serverKey);
        Result<bool> pingResult = await _remoteApi.PingAsync(connectionDetails, CancellationToken);

        if (!pingResult.IsSuccess || !pingResult.Entity)
        {
            return new GenericCommandError
            (
                "Could not connect to the server. Ensure you have entered the correct details"
            );
        }

        SpaceEngineersData data = await _dbContext.FindOrDefaultAsync<SpaceEngineersData>
        (
            _context.GuildID.Value.Value,
            addIfNotPresent: true,
            ct: CancellationToken
        );

        data.ServerAddress = serverAddress;
        data.ServerPort = serverPort;
        data.ServerKey = serverKey;

        _dbContext.Update(data);
        await _dbContext.SaveChangesAsync(CancellationToken);

        return (Result)await _feedbackService.SendContextualSuccessAsync
        (
            "Successfully connected to the Space Engineers server",
            ct: CancellationToken
        );
    }

    [Command("ping")]
    [Description("Pings the server")]
    [Ephemeral]
    public async Task<Result> PingCommandAsync()
    {
        SpaceEngineersData? data = await _dbContext.SpaceEngineersDatas
            .FindAsync(_context.GuildID.Value.Value, CancellationToken);

        if (data is null || !data.TryGetConnectionDetails(out SEServerConnectionDetails connectionDetails))
            return new GenericCommandError("Please connect to a Space Engineers server before using this command");

        Result<bool> pingResult = await _remoteApi.PingAsync(connectionDetails, CancellationToken);

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
        SpaceEngineersData? data = await _dbContext.SpaceEngineersDatas
            .FindAsync(_context.GuildID.Value.Value, CancellationToken);

        if (data is null || !data.TryGetConnectionDetails(out SEServerConnectionDetails connectionDetails))
            return new GenericCommandError("Please connect to a Space Engineers server before using this command");

        Result<IReadOnlyList<Player>> playersResult = await _remoteApi.GetPlayersAsync(connectionDetails, CancellationToken);
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

    [Command("status-message")]
    [Description("Creates an auto-updating server status message")]
    [RequireGuildPermission(DiscordPermission.ManageGuild)]
    [Ephemeral]
    public async Task<Result> CreateStatusMessageCommandAsync
    (
        [ChannelTypes(ChannelType.GuildText, ChannelType.PublicThread, ChannelType.PrivateThread)] IChannel channel
    )
    {
        Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel
        (
            channel,
            DiscordConstants.UserId,
            CancellationToken
        ).ConfigureAwait(false);

        if (!getPermissionSet.IsSuccess)
            return (Result)getPermissionSet;

        if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
            return new PermissionError(DiscordPermission.ViewChannel, DiscordConstants.UserId, channel.ID);

        if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
            return new PermissionError(DiscordPermission.SendMessages, DiscordConstants.UserId, channel.ID);

        SpaceEngineersData? data = await _dbContext.SpaceEngineersDatas
            .FindAsync(_context.GuildID.Value.Value, CancellationToken);

        if (data is null || !data.TryGetConnectionDetails(out _))
            return new GenericCommandError("Please connect to a Space Engineers server before using this command");

        Result<IMessage> createMessage = await _feedbackService.SendEmbedAsync
        (
            channel.ID,
            new Embed("Server State: Unknown"),
            ct: CancellationToken
        );

        if (!createMessage.IsDefined(out IMessage? message))
            return (Result)createMessage;

        data.StatusMessageId = message.ID.Value;
        data.StatusMessageChannelId = channel.ID.Value;

        _dbContext.Update(data);
        await _dbContext.SaveChangesAsync(CancellationToken);

        return (Result)await _feedbackService.SendContextualSuccessAsync
        (
            "Status message created!",
            ct: CancellationToken
        );
    }
}
