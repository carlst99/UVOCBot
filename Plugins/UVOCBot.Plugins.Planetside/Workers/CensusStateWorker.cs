using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.Workers;

public class CensusStateWorker : BackgroundService
{
    private static readonly ValidWorldDefinition[] ValidWorlds = Enum.GetValues<ValidWorldDefinition>();

    private readonly IMemoryCache _cache;
    private readonly ICensusApiService _censusApi;
    private readonly IPopulationService _populationService;

    public CensusStateWorker
    (
        IMemoryCache cache,
        ICensusApiService censusApi,
        IPopulationService populationService
    )
    {
        _cache = cache;
        _censusApi = censusApi;
        _populationService = populationService;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (_censusApi is not Services.CachingCensusApiService)
            throw new InvalidOperationException("Expected the " + nameof(Services.CachingCensusApiService) + " to be registered with the service provider.");

        foreach (ValidWorldDefinition world in ValidWorlds)
        {
            // Pre-cache map states
            await _censusApi.GetMapsAsync(world, Enum.GetValues<ValidZoneDefinition>(), ct);
            await PrecacheMetagameEvents(world, ct);
        }

        // Keep population data up-to-date
        TimeSpan? popUpdateFrequency = CacheEntryHelpers.PopulationOptions.AbsoluteExpirationRelativeToNow;
        if (popUpdateFrequency is null)
            return;

        popUpdateFrequency = popUpdateFrequency.Value.Subtract(TimeSpan.FromSeconds(15));

        while (!ct.IsCancellationRequested)
        {
            // Assume this to be caching
            foreach (ValidWorldDefinition world in ValidWorlds)
                await _populationService.GetWorldPopulationAsync(world, ct);

            await Task.Delay(popUpdateFrequency.Value, ct);
        }
    }

    private async Task PrecacheMetagameEvents(ValidWorldDefinition world, CancellationToken ct)
    {
        HashSet<ZoneDefinition> seenZones = new();

        Result<List<MetagameEvent>> events = await _censusApi.GetMetagameEventsAsync(world, ct: ct).ConfigureAwait(false);
        if (!events.IsDefined())
            return;

        if (events.Entity.Count == 0)
            return;

        // Pre-cache metagame events.
        // Assume events are ordered by timestamp, as we select this in the query.
        foreach (MetagameEvent mev in events.Entity)
        {
            if (seenZones.Contains(mev.ZoneID.Definition))
                continue;

            _cache.Set
            (
                CacheKeyHelpers.GetMetagameEventKey(mev),
                mev
            );

            seenZones.Add(mev.ZoneID.Definition);
        }
    }
}
