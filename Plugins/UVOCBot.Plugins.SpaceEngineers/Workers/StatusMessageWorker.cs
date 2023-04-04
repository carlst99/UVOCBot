using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;
using UVOCBot.Plugins.SpaceEngineers.Extensions;
using UVOCBot.Plugins.SpaceEngineers.Objects;
using UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

namespace UVOCBot.Plugins.SpaceEngineers.Workers;

public class StatusMessageWorker : BackgroundService
{
    private readonly ILogger<StatusMessageWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<ulong> _editFailures;

    public StatusMessageWorker
    (
        ILogger<StatusMessageWorker> logger,
        IServiceScopeFactory scopeFactory
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _editFailures = new HashSet<ulong>();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunLoopAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // This is fine
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run an iteration of the StatusMessageWorker");

                // Cool off for one minute after error
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

        while (!ct.IsCancellationRequested)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            DiscordContext dbContext = scope.ServiceProvider.GetRequiredService<DiscordContext>();
            IVRageRemoteApi remoteApi = scope.ServiceProvider.GetRequiredService<IVRageRemoteApi>();
            IDiscordRestChannelAPI channelApi = scope.ServiceProvider.GetRequiredService<IDiscordRestChannelAPI>();

            IQueryable<SpaceEngineersData> statusMessages = dbContext.SpaceEngineersDatas
                .Where(d => d.StatusMessageId != null && d.StatusMessageChannelId != null);

            foreach (SpaceEngineersData data in statusMessages)
            {
                if (!data.TryGetConnectionDetails(out SEServerConnectionDetails connectionDetails))
                    continue;

                Embed embed = new
                (
                    "Server State Unknown",
                    Footer: new EmbedFooter("Last updated"),
                    Timestamp: DateTimeOffset.UtcNow
                );

                Result<IReadOnlyList<Player>> playersResult = await remoteApi.GetPlayersAsync(connectionDetails, ct);
                if (!playersResult.IsDefined(out IReadOnlyList<Player>? players))
                {
                    embed = embed with
                    {
                        Title = Formatter.Emoji("red_circle") + " Server Offline"
                    };
                }
                else
                {
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

                    embed = embed with {
                        Title = Formatter.Emoji("green_circle") + $" Players Online: {playerCount}",
                        Description = message.ToString()
                    };
                }

                ulong channelId = data.StatusMessageChannelId!.Value;
                ulong messageId = data.StatusMessageId!.Value;

                Result<IMessage> editResult = await channelApi.EditMessageAsync
                (
                    DiscordSnowflake.New(channelId),
                    DiscordSnowflake.New(messageId),
                    embeds: new IEmbed[] { embed },
                    ct: ct
                );

                // Check if we've failed to edit the message twice in a row. Probably deleted
                if (!editResult.IsSuccess && !_editFailures.Add(messageId))
                {
                    data.StatusMessageChannelId = null;
                    data.StatusMessageId = null;

                    dbContext.Update(data);
                    await dbContext.SaveChangesAsync(ct);
                }

                _editFailures.Remove(messageId);
            }

            await timer.WaitForNextTickAsync(ct);
        }
    }
}
