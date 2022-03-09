﻿using DbgCensus.Core.Objects;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="IPopulationService" />
public abstract class CachingPopulationService : IPopulationService
{
    protected readonly IMemoryCache _cache;

    protected CachingPopulationService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<IPopulation>> GetWorldPopulationAsync(ValidWorldDefinition world, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetPopulationKey((WorldDefinition)world), out IPopulation pop))
            return Result<IPopulation>.FromSuccess(pop);

        Result<IPopulation> popResult = await QueryPopulationAsync(world, ct).ConfigureAwait(false);
        if (!popResult.IsSuccess)
            return popResult;

        _cache.Set
        (
            CacheKeyHelpers.GetPopulationKey(popResult.Entity),
            popResult.Entity,
            CacheEntryHelpers.PopulationOptions
        );

        return Result<IPopulation>.FromSuccess(popResult.Entity);
    }

    protected abstract Task<Result<IPopulation>> QueryPopulationAsync(ValidWorldDefinition world, CancellationToken ct);
}
