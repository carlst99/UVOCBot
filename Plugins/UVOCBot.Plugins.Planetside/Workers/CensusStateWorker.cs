﻿using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    private readonly ILogger<CensusStateWorker> _logger;
    private readonly IMemoryCache _cache;
    private readonly ICensusApiService _censusApi;
    private readonly IFacilityCaptureService _facilityCaptureService;
    private readonly IPopulationService _populationService;

    private Task? _facilityCaptureServiceTask;

    public CensusStateWorker
    (
        ILogger<CensusStateWorker> logger,
        IMemoryCache cache,
        ICensusApiService censusApi,
        IFacilityCaptureService facilityCaptureService,
        IPopulationService populationService
    )
    {
        _logger = logger;
        _cache = cache;
        _censusApi = censusApi;
        _facilityCaptureService = facilityCaptureService;
        _populationService = populationService;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _facilityCaptureServiceTask = _facilityCaptureService.RunAsync(ct);

        // Assume these calls to be caching
        foreach (ValidWorldDefinition world in ValidWorlds)
        {
            await _censusApi.GetMapsAsync(world, Enum.GetValues<ValidZoneDefinition>(), ct);
            await PrecacheMetagameEvents(world, ct);
        }

        // Keep population data up-to-date
        TimeSpan? popUpdateFrequency = CacheEntryHelpers.PopulationOptions.AbsoluteExpirationRelativeToNow;
        if (popUpdateFrequency is null)
            throw new InvalidOperationException("Cache expiration for Population objects must be configured");

        using PeriodicTimer timer = new(popUpdateFrequency.Value.Subtract(TimeSpan.FromSeconds(15)));
        do
        {
            // Check on and restart the facility capture task if required
            if (_facilityCaptureServiceTask.IsCompleted)
            {
                if (_facilityCaptureServiceTask.Exception is not null)
                {
                    foreach (Exception ex in _facilityCaptureServiceTask.Exception.InnerExceptions)
                        _logger.LogError(ex, "An exception occured in the FacilityCaptureService");
                }

                _facilityCaptureServiceTask = _facilityCaptureService.RunAsync(ct);
            }

            try
            {
                // Assume this call to be caching
                foreach (ValidWorldDefinition world in ValidWorlds)
                    await _populationService.GetWorldPopulationAsync(world, true, ct);
            }
            catch (OperationCanceledException)
            {
                // This is fine
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run iteration of CensusStateWorker");
            }
        } while (await timer.WaitForNextTickAsync(ct));

        await _facilityCaptureServiceTask;
    }

    private async Task PrecacheMetagameEvents(ValidWorldDefinition world, CancellationToken ct)
    {
        HashSet<ZoneDefinition> seenZones = [];

        Result<List<MetagameEvent>> events = await _censusApi.GetMetagameEventsAsync(world, ct: ct);
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
