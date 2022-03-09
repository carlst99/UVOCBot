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
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;

namespace UVOCBot.Plugins.Planetside.Workers;

public class CensusStateWorker : BackgroundService
{
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

        foreach (ValidWorldDefinition world in Enum.GetValues<ValidWorldDefinition>())
        {
            // Pre-cache map states
            await _censusApi.GetMapsAsync(world, Enum.GetValues<ValidZoneDefinition>(), ct);

            Result<List<QueryMetagameEvent>> events = await _censusApi.GetMetagameEventsAsync(world, ct: ct).ConfigureAwait(false);
            if (!events.IsDefined())
                continue;

            if (events.Entity.Count == 0)
                continue;

            // Pre-cache metagame events.
            // Assume events are ordered by timestamp, as we select this in the query.
            MetagameEvent eventStreamConversion = events.Entity[0].ToEventStreamMetagameEvent();
            object key = CacheKeyHelpers.GetMetagameEventKey(eventStreamConversion);
            _cache.Set(key, eventStreamConversion);
        }

        TimeSpan? popUpdateFrequency = CacheEntryHelpers.PopulationOptions.AbsoluteExpirationRelativeToNow;
        if (popUpdateFrequency is null)
            return;

        popUpdateFrequency = popUpdateFrequency.Value.Subtract(TimeSpan.FromSeconds(15));

        while (!ct.IsCancellationRequested)
        {
            // Assume this is caching
            foreach (ValidWorldDefinition world in Enum.GetValues<ValidWorldDefinition>())
                await _populationService.GetWorldPopulationAsync(world, ct);

            await Task.Delay(popUpdateFrequency.Value, ct);
        }
    }
}
