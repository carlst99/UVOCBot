using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Events.Characters;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Polly.CircuitBreaker;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using Channel = System.Threading.Channels.Channel;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="IFacilityCaptureService" />
public sealed class FacilityCaptureService : IFacilityCaptureService
{
    private static readonly Uri AuraxiumImageUri = GetResourceImageUri("957264037048115242");
    private static readonly Uri SynthiumImageUri = GetResourceImageUri("957264081465786388");
    private static readonly Uri PolystellariteImageUri = GetResourceImageUri("957264140093763604");

    private readonly ICensusApiService _censusApiService;
    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;
    private readonly ICensusApiService _censusApi;
    private readonly IDiscordRestChannelAPI _channelApi;

    private readonly Channel<IFacilityControl> _facilityControls;
    private readonly ConcurrentDictionary<ulong, HashSet<IPlayerFacilityCapture>> _playerFacilityCaptures;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    public FacilityCaptureService
    (
        ICensusApiService censusApiService,
        IMemoryCache cache,
        IDbContextFactory<DiscordContext> dbContextFactory,
        ICensusApiService censusApi,
        IDiscordRestChannelAPI channelApi
    )
    {
        _censusApiService = censusApiService;
        _cache = cache;
        _dbContextFactory = dbContextFactory;
        _censusApi = censusApi;
        _channelApi = channelApi;

        _facilityControls = Channel.CreateUnbounded<IFacilityControl>();
        _playerFacilityCaptures = new ConcurrentDictionary<ulong, HashSet<IPlayerFacilityCapture>>();
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (IsRunning)
            throw new InvalidOperationException("Already running");

        IsRunning = true;

        try
        {
            await foreach (IFacilityControl facilityControl in _facilityControls.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                // Wait for player facility captures to be processed,
                // and also back off Census
                await Task.Delay(200, ct).ConfigureAwait(false);
                if (facilityControl.Timestamp.AddSeconds(10) > DateTimeOffset.UtcNow)
                {
                    await _facilityControls.Writer.WriteAsync(facilityControl, ct).ConfigureAwait(false);
                    continue;
                }

                Result<MapRegion?> regionResult = await _censusApiService.GetFacilityRegionAsync(facilityControl.FacilityID, ct).ConfigureAwait(false);
                if (!regionResult.IsSuccess)
                {
                    // Sigh... thanks Census
                    if (regionResult.Error is ExceptionError { Exception: BrokenCircuitException })
                        await Task.Delay(TimeSpan.FromSeconds(15), ct);

                    await _facilityControls.Writer.WriteAsync(facilityControl, ct).ConfigureAwait(false);
                    continue;
                }

                if (regionResult.Entity is null)
                {
                    // We've encountered a facility that Census doesn't know about
                    _playerFacilityCaptures.TryRemove(facilityControl.FacilityID, out _);
                    continue;
                }

                MapRegion region = regionResult.Entity;
                UpdateMapCache(facilityControl, region);

                // We don't really care about the result of this
                _ = SendFacilityCaptureMessages(facilityControl, region, ct).ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            // This is fine
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <inheritdoc />
    public void RegisterPlayerFacilityCaptureEvent(IPlayerFacilityCapture playerFacilityCapture)
    {
        // We're only interested in captures involving an outfit
        if (playerFacilityCapture.OutfitID == 0)
            return;

        HashSet<IPlayerFacilityCapture> captureSet = _playerFacilityCaptures.GetOrAdd
        (
            playerFacilityCapture.FacilityID,
            _ => new HashSet<IPlayerFacilityCapture>()
        );
        captureSet.Add(playerFacilityCapture);
    }

    /// <inheritdoc />
    public async ValueTask RegisterFacilityControlEventAsync(IFacilityControl facilityControl, CancellationToken ct = default)
    {
        // No need to bother with same-faction control events (i.e. point defenses).
        if (facilityControl.OldFactionID == facilityControl.NewFactionID)
            return;

        await _facilityControls.Writer.WriteAsync(facilityControl, ct);
    }

    private async Task SendFacilityCaptureMessages
    (
        IFacilityControl facilityControl,
        MapRegion region,
        CancellationToken ct
    )
    {
        _playerFacilityCaptures.TryRemove
        (
            facilityControl.FacilityID,
            out HashSet<IPlayerFacilityCapture>? playerCaptures
        );

        // We're only interested in captures involving an outfit
        if (facilityControl.OutfitID == 0)
            return;

        DiscordContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        List<PlanetsideSettings> validPSettings = dbContext.PlanetsideSettings
            .Where(s => s.BaseCaptureChannelId != null)
            .AsEnumerable()
            .Where(s => s.TrackedOutfits.Contains(facilityControl.OutfitID))
            .ToList();

        if (validPSettings.Count == 0)
            return;

        Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(facilityControl.OutfitID, ct).ConfigureAwait(false);
        if (!getOutfitResult.IsDefined(out Outfit? outfit))
            return;

        List<IEmbedField> embedFields = new();

        // Attempt to get the names of every player involved in the capture
        if (playerCaptures is not null)
        {
            Result<List<MinimalCharacter>> getPlayerInfo = await _censusApi.GetMinimalCharactersAsync
            (
                playerCaptures.Where(p => p.OutfitID == facilityControl.OutfitID).Select(p => p.CharacterID),
                ct
            ).ConfigureAwait(false);

            if (getPlayerInfo.IsSuccess)
            {
                embedFields.Add
                (
                    new EmbedField
                    (
                        "Players Involved",
                        string.Join('\n', getPlayerInfo.Entity.Select(c => c.Name.First))
                    )
                );
            }
        }

        FacilityResource resourceType = FacilityTypeToBaseResource(region.FacilityTypeID);
        Uri? thumbnailUri = BaseResourceToImageUri(resourceType);

        embedFields.Add
        (
            new EmbedField
            (
                "Resources",
                $"{resourceType}"
            )
        );

        Embed embed = new
        (
            "Base Captured",
            Description: $"{Formatter.Bold(outfit.Alias)} has captured {Formatter.Italic(region.FacilityName)} from the {facilityControl.OldFactionID}",
            Colour: facilityControl.NewFactionID.ToColor(),
            Thumbnail: thumbnailUri is null
                ? default(Remora.Rest.Core.Optional<IEmbedThumbnail>)
                : new EmbedThumbnail(thumbnailUri.ToString()),
            Fields: embedFields
        );

        foreach (PlanetsideSettings pSettings in validPSettings)
        {
            await _channelApi.CreateMessageAsync
            (
                new Snowflake(pSettings.BaseCaptureChannelId!.Value, Constants.DiscordEpoch),
                embeds: new IEmbed[] { embed },
                ct: ct
            ).ConfigureAwait(false);
        }
    }

    private void UpdateMapCache(IFacilityControl controlEvent, MapRegion facility)
    {
        if (!_cache.TryGetValue(CacheKeyHelpers.GetMapKey(controlEvent.WorldID, controlEvent.ZoneID.Definition), out Map map))
            return;

        int index = map.Regions.Row.FindIndex(r => r.RowData.RegionID == facility.MapRegionID);
        if (index == -1)
            return;

        Map.RowDataModel rowData = map.Regions.Row[index].RowData;
        map.Regions.Row[index] = new Map.RowModel(rowData with { FactionID = controlEvent.NewFactionID });

        _cache.Set
        (
            CacheKeyHelpers.GetMapKey(controlEvent.WorldID, map),
            map,
            CacheEntryHelpers.MapOptions
        );
    }

    // Note: This is the primary UVOCBot app ID. We don't expect other apps to have the same resources.
    private static Uri GetResourceImageUri(string assetID)
        => CDN.GetApplicationAssetUrl(DiscordSnowflake.New(747683069737041970), assetID, CDNImageFormat.PNG, 128).Entity;

    private static Uri? BaseResourceToImageUri(FacilityResource resource)
        => resource switch
        {
            FacilityResource.Auraxium => AuraxiumImageUri,
            FacilityResource.Synthium => SynthiumImageUri,
            FacilityResource.Polystellarite => PolystellariteImageUri,
            _ => default
        };

    private static FacilityResource FacilityTypeToBaseResource(FacilityType? type)
        => type switch
        {
            FacilityType.SmallOutpost => FacilityResource.Auraxium,
            FacilityType.ConstructionOutpost => FacilityResource.Synthium,
            FacilityType.LargeOutpost => FacilityResource.Synthium,
            FacilityType.AmpStation => FacilityResource.Polystellarite,
            FacilityType.BioLab => FacilityResource.Polystellarite,
            FacilityType.InterlinkFacility => FacilityResource.Polystellarite,
            FacilityType.TechPlant => FacilityResource.Polystellarite,
            FacilityType.Default => FacilityResource.None,
            FacilityType.RelicOutpost => FacilityResource.None,
            FacilityType.Warpgate => FacilityResource.None,
            FacilityType.ContainmentSite => FacilityResource.Synthium,
            FacilityType.TridentSpire => FacilityResource.Polystellarite,
            _ => FacilityResource.None
        };
}
