using Mandible.Abstractions.Manifest;
using Mandible.Manifest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Feeds.Objects;

namespace UVOCBot.Plugins.Feeds.Workers;

public class PatchManifestWorker : BackgroundService
{
    private static readonly Manifest[] ManifestTypes = Enum.GetValues<Manifest>();

    private readonly ILogger<PatchManifestWorker> _logger;
    private readonly IManifestService _manifestService;
    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;
    private readonly IDiscordRestChannelAPI _channelApi;

    private readonly Dictionary<Manifest, DateTimeOffset> _manifestLastUpdateTimes;

    public PatchManifestWorker
    (
        ILogger<PatchManifestWorker> logger,
        IManifestService manifestService,
        IDbContextFactory<DiscordContext> dbContextFactory,
        IDiscordRestChannelAPI channelApi
    )
    {
        _logger = logger;
        _manifestService = manifestService;
        _dbContextFactory = dbContextFactory;
        _channelApi = channelApi;

        _manifestLastUpdateTimes = new Dictionary<Manifest, DateTimeOffset>();
        foreach (Manifest value in ManifestTypes)
            _manifestLastUpdateTimes[value] = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (Manifest manifest in ManifestTypes)
            {
                try
                {
                    Digest digest = await _manifestService.GetDigestAsync(manifest.GetUrl(), ct);
                    if (digest.Timestamp <= _manifestLastUpdateTimes[manifest])
                        continue;

                    _manifestLastUpdateTimes[manifest] = digest.Timestamp;

                    await using DiscordContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
                    foreach (GuildFeedsSettings feedsSettings in dbContext.GuildFeedsSettings)
                    {
                        if (!feedsSettings.IsEnabled || feedsSettings.FeedChannelID is null)
                            continue;

                        await PostManifestToChannelAsync
                        (
                            feedsSettings,
                            manifest,
                            ct
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get manifest");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(5), ct).ConfigureAwait(false);
        }
    }

    private async Task PostManifestToChannelAsync
    (
        GuildFeedsSettings settings,
        Manifest updatedManifest,
        CancellationToken ct
    )
    {
        string message = updatedManifest switch
        {
            Manifest.Live => "An update has been released for the live client",
            Manifest.LiveNext => $"An {Formatter.Italic("upcoming")} update has been detected for the live client",
            Manifest.PTS => "An update has been released for the PTS client",
            Manifest.PTSNext => $"An {Formatter.Italic("upcoming")} update has been detected for the PTS client",
            _ => throw new ArgumentException("Invalid manifest type", nameof(updatedManifest))
        };

        if (!settings.IsEnabled || settings.FeedChannelID is null)
            return;

        if (((Feed)settings.Feeds & Feed.PatchNotifications) == 0)
            return;

        Snowflake channelID = DiscordSnowflake.New(settings.FeedChannelID.Value);

        Embed embed = new
        (
            "Client Update Detected " + Formatter.Emoji("eyes"),
            Description: message,
            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR
        );

        await _channelApi.CreateMessageAsync
        (
            channelID,
            embeds: new[] { embed },
            ct: ct
        ).ConfigureAwait(false);
    }
}
