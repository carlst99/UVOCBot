using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Feeds.Objects;

namespace UVOCBot.Plugins.Feeds.Workers;

public class PatchManifestWorker : BackgroundService
{
    private static readonly Manifest[] ManifestTypes = Enum.GetValues<Manifest>();

    private readonly ILogger<PatchManifestWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;
    private readonly IDiscordRestChannelAPI _channelApi;

    private readonly Dictionary<Manifest, DateTimeOffset> _manifestLastUpdateTimes;

    public PatchManifestWorker
    (
        ILogger<PatchManifestWorker> logger,
        HttpClient httpClient,
        IDbContextFactory<DiscordContext> dbContextFactory,
        IDiscordRestChannelAPI channelApi
    )
    {
        _logger = logger;
        _httpClient = httpClient;
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
                string resourceUrl = manifest.GetUrl();

                try
                {
                    string manifestXml = await _httpClient.GetStringAsync(resourceUrl, ct).ConfigureAwait(false);

                    DateTimeOffset manifestUpdateTime = GetManifestUpdateTime(manifestXml);
                    if (manifestUpdateTime <= _manifestLastUpdateTimes[manifest])
                        continue;

                    _manifestLastUpdateTimes[manifest] = manifestUpdateTime;

                    DiscordContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
                    foreach (GuildFeedsSettings feedsSettings in dbContext.GuildFeedsSettings)
                    {
                        if (!feedsSettings.IsEnabled || feedsSettings.FeedChannelID is null)
                            continue;

                        await PostTweetsToChannelAsync
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

            await Task.Delay(TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        }
    }

    private static DateTimeOffset GetManifestUpdateTime(string manifestXml)
    {
        using StringReader sr = new(manifestXml);
        using XmlReader reader = XmlReader.Create(sr);

        while (reader.Read())
        {
            if (reader.NodeType is not XmlNodeType.Element)
                continue;

            if (reader.Name != "digest")
                continue;

            string? timestampAttribute = reader.GetAttribute("timestamp");

            if (ulong.TryParse(timestampAttribute, out ulong timestamp))
                return new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(timestamp);

            throw new Exception("Timestamp attribute contained an invalid value: " + timestampAttribute);
        }

        throw new Exception("Failed to find the timestamp");
    }

    private async Task PostTweetsToChannelAsync
    (
        GuildFeedsSettings settings,
        Manifest updatedManifest,
        CancellationToken ct
    )
    {
        string message = updatedManifest switch
        {
            Manifest.Live => "An update has been detected for the live client",
            Manifest.LiveNext => $"An {Formatter.Italic("upcoming")} update has been detected for the live client",
            Manifest.PTS => "An update has been detected for the PTS client",
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
