using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.EventStream;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers
{
    internal sealed class FacilityControlResponder : ICensusEventHandler<ServiceMessage<FacilityControl>>
    {
        private readonly DiscordContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ICensusApiService _censusApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public FacilityControlResponder(
            DiscordContext dbContext,
            IMemoryCache cache,
            ICensusApiService censusApi,
            IDiscordRestChannelAPI channelApi)
        {
            _dbContext = dbContext;
            _cache = cache;
            _censusApi = censusApi;
            _channelApi = channelApi;
        }

        public async Task HandleAsync(ServiceMessage<FacilityControl> censusEvent, CancellationToken ct = default)
        {
            // Shouldn't report same-faction control events (i.e. point defenses).
            if (censusEvent.Payload.OldFactionId == censusEvent.Payload.NewFactionId)
                return;

            Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(censusEvent.Payload.OutfitId, ct).ConfigureAwait(false);
            if (!getOutfitResult.IsDefined())
                return;
            Outfit outfit = getOutfitResult.Entity;

            Result<MapRegion?> getFacilityResult = await _censusApi.GetFacilityRegionAsync(censusEvent.Payload.FacilityId, ct).ConfigureAwait(false);
            if (!getFacilityResult.IsDefined())
                return;
            MapRegion facility = getFacilityResult.Entity;

            UpdateMapCache(censusEvent.Payload, facility);
            await SendBaseCaptureMessages(outfit, facility, ct).ConfigureAwait(false);
        }

        private async Task SendBaseCaptureMessages(Outfit outfit, MapRegion facility, CancellationToken ct = default)
        {
            IEnumerable<PlanetsideSettings> validPSettings = _dbContext.PlanetsideSettings
                .Where(s => s.BaseCaptureChannelId != null)
                .AsEnumerable()
                .Where(s => s.TrackedOutfits.Contains(outfit.OutfitId));

            if (!validPSettings.Any())
                return;

            foreach (PlanetsideSettings pSettings in validPSettings)
            {
                Embed embed = new(
                    "Base Captured",
                    Description: $"{ Formatter.Bold($"[{ outfit.Alias }]") } has captured { Formatter.Italic(facility.FacilityName) }",
                    Colour: DiscordConstants.DEFAULT_EMBED_COLOUR
                );

                await _channelApi.CreateMessageAsync(
                    new Snowflake(pSettings.BaseCaptureChannelId!.Value),
                    embeds: new IEmbed[] { embed },
                    ct: ct
                ).ConfigureAwait(false);
            }
        }

        private void UpdateMapCache(FacilityControl controlEvent, MapRegion facility)
        {
            if (!_cache.TryGetValue(CacheKeyHelpers.GetMapKey(controlEvent.WorldId, controlEvent.ZoneId.Definition), out Map map))
                return;

            int index = map.Regions.Row.FindIndex(r => r.RowData.RegionID == facility.MapRegionID);
            if (index == -1)
                return;

            Map.RowModel mapRow = map.Regions.Row[index];
            map.Regions.Row.RemoveAt(index);

            mapRow = mapRow with { RowData = mapRow.RowData with { FactionID = controlEvent.NewFactionId } };
            map.Regions.Row.Add(mapRow);

            _cache.Set
            (
                CacheKeyHelpers.GetMapKey(controlEvent.WorldId, map),
                map,
                CacheEntryHelpers.GetMapOptions()
            );
        }
    }
}
