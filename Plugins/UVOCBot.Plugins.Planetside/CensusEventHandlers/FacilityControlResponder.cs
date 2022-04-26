using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class FacilityControlResponder : IPayloadHandler<IFacilityControl>
{
    private static readonly Uri AuraxiumImageUri = GetResourceImageUri("957264037048115242");
    private static readonly Uri SynthiumImageUri = GetResourceImageUri("957264081465786388");
    private static readonly Uri PolystellariteImageUri = GetResourceImageUri("957264140093763604");

    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;
    private readonly IMemoryCache _cache;
    private readonly ICensusApiService _censusApi;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IFacilityCaptureService _facilityCaptureService;

    public FacilityControlResponder
    (
        IDbContextFactory<DiscordContext> dbContextFactory,
        IMemoryCache cache,
        ICensusApiService censusApi,
        IDiscordRestChannelAPI channelApi,
        IFacilityCaptureService facilityCaptureService
    )
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _censusApi = censusApi;
        _channelApi = channelApi;
        _facilityCaptureService = facilityCaptureService;
    }

    public async Task HandleAsync(IFacilityControl censusEvent, CancellationToken ct = default)
    {
        // No need to bother with same-faction control events (i.e. point defenses).
        if (censusEvent.OldFactionID == censusEvent.NewFactionID)
            return;

        await _facilityCaptureService.EnqueueAsync
        (
            censusEvent,
            (r, c) => OnRegionResolved(censusEvent, r, c),
            CancellationToken.None
        );
    }

    // Note: This is the primary UVOCBot app ID. We don't expect other apps to have the same resources.
    private static Uri GetResourceImageUri(string assetID)
        => CDN.GetApplicationAssetUrl(new Snowflake(747683069737041970), assetID, CDNImageFormat.PNG, 128).Entity;

    private async Task OnRegionResolved(IFacilityControl censusEvent, MapRegion region, CancellationToken ct)
    {
        UpdateMapCache(censusEvent, region);

        // Send base capture messages
        if (censusEvent.OutfitID == 0)
            return;

        DiscordContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        List<PlanetsideSettings> validPSettings = dbContext.PlanetsideSettings
            .Where(s => s.BaseCaptureChannelId != null)
            .AsEnumerable()
            .Where(s => s.TrackedOutfits.Contains(censusEvent.OutfitID))
            .ToList();

        if (validPSettings.Count == 0)
            return;

        Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(censusEvent.OutfitID, ct).ConfigureAwait(false);
        if (!getOutfitResult.IsDefined(out Outfit? outfit))
            return;

        await SendBaseCaptureMessages(validPSettings, censusEvent, outfit, region, ct).ConfigureAwait(false);
    }

    private async Task SendBaseCaptureMessages
    (
        IEnumerable<PlanetsideSettings> planetSideSettings,
        IFacilityControl facilityControlEvent,
        Outfit outfit,
        MapRegion facility,
        CancellationToken ct = default
    )
    {
        FacilityResource resourceType = FacilityTypeToBaseResource(facility.FacilityTypeID);
        Uri? thumbnailUri = BaseResourceToImageUri(resourceType);

        EmbedField resourceField = new
        (
            "Resources",
            $"{facility.RewardAmount} {resourceType}"
        );

        Embed embed = new
        (
            "Base Captured",
            Description: $"{Formatter.Bold(outfit.Alias)} has captured {Formatter.Italic(facility.FacilityName)} from the {facilityControlEvent.OldFactionID}",
            Colour: facilityControlEvent.NewFactionID.ToColor(),
            Thumbnail: thumbnailUri is null
                ? default(Remora.Rest.Core.Optional<IEmbedThumbnail>)
                : new EmbedThumbnail(thumbnailUri.ToString()),
            Fields: new IEmbedField[] { resourceField }
        );

        foreach (PlanetsideSettings pSettings in planetSideSettings)
        {
            await _channelApi.CreateMessageAsync
            (
                new Snowflake(pSettings.BaseCaptureChannelId!.Value, Constants.DiscordEpoch),
                embeds: new IEmbed[] { embed },
                ct: ct
            ).ConfigureAwait(false);
        }
    }

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
            _ => FacilityResource.None
        };

    private static Uri? BaseResourceToImageUri(FacilityResource resource)
        => resource switch
        {
            FacilityResource.Auraxium => AuraxiumImageUri,
            FacilityResource.Synthium => SynthiumImageUri,
            FacilityResource.Polystellarite => PolystellariteImageUri,
            _ => default
        };

    private void UpdateMapCache(IFacilityControl controlEvent, MapRegion facility)
    {
        if (!_cache.TryGetValue(CacheKeyHelpers.GetMapKey(controlEvent.WorldID, controlEvent.ZoneID.Definition), out Map map))
            return;

        int index = map.Regions.Row.FindIndex(r => r.RowData.RegionID == facility.MapRegionID);
        if (index == -1)
            return;

        Map.RowModel mapRow = map.Regions.Row[index];
        map.Regions.Row[index] = new Map.RowModel(RowData: mapRow.RowData with { FactionID = controlEvent.NewFactionID });

        _cache.Set
        (
            CacheKeyHelpers.GetMapKey(controlEvent.WorldID, map),
            map,
            CacheEntryHelpers.MapOptions
        );
    }
}
