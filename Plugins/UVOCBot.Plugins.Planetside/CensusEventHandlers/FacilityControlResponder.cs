using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
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
    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;
    private readonly IMemoryCache _cache;
    private readonly ICensusApiService _censusApi;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IMapRegionResolverService _mapRegionResolver;

    public FacilityControlResponder
    (
        IDbContextFactory<DiscordContext> dbContextFactory,
        IMemoryCache cache,
        ICensusApiService censusApi,
        IDiscordRestChannelAPI channelApi,
        IMapRegionResolverService mapRegionResolver
    )
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _censusApi = censusApi;
        _channelApi = channelApi;
        _mapRegionResolver = mapRegionResolver;
    }

    public async Task HandleAsync(IFacilityControl censusEvent, CancellationToken ct = default)
    {
        // No need to bother with same-faction control events (i.e. point defenses).
        if (censusEvent.OldFactionID == censusEvent.NewFactionID)
            return;

        await _mapRegionResolver.EnqueueAsync
        (
            censusEvent,
            (r, c) => OnRegionResolved(censusEvent, r, c),
            CancellationToken.None
        );
    }

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

        await SendBaseCaptureMessages(validPSettings, outfit, region, ct).ConfigureAwait(false);
    }

    private async Task SendBaseCaptureMessages
    (
        IEnumerable<PlanetsideSettings> planetSideSettings,
        Outfit outfit,
        MapRegion facility,
        CancellationToken ct = default
    )
    {
        foreach (PlanetsideSettings pSettings in planetSideSettings)
        {
            Embed embed = new
            (
                "Base Captured",
                Description: $"{ Formatter.Bold($"[{ outfit.Alias }]") } has captured { Formatter.Italic(facility.FacilityName) }",
                Colour: DiscordConstants.DEFAULT_EMBED_COLOUR
            );

            await _channelApi.CreateMessageAsync
            (
                new Snowflake(pSettings.BaseCaptureChannelId!.Value, Remora.Discord.API.Constants.DiscordEpoch),
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

        Map.RowModel mapRow = map.Regions.Row[index];
        map.Regions.Row[index] = mapRow with { RowData = mapRow.RowData with { FactionID = controlEvent.NewFactionID } };

        _cache.Set
        (
            CacheKeyHelpers.GetMapKey(controlEvent.WorldID, map),
            map,
            CacheEntryHelpers.MapOptions
        );
    }
}
