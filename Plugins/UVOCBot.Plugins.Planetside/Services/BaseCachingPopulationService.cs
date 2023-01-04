using DbgCensus.Core.Objects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="IPopulationService" />
public abstract class BaseCachingPopulationService : IPopulationService
{
    private readonly ILogger<BaseCachingPopulationService> _logger;
    protected readonly IMemoryCache _cache;

    protected BaseCachingPopulationService(ILogger<BaseCachingPopulationService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<IPopulation>> GetWorldPopulationAsync(ValidWorldDefinition world, bool skipCacheRetrieval = false, CancellationToken ct = default)
    {
        if (!skipCacheRetrieval &&
            _cache.TryGetValue(CacheKeyHelpers.GetPopulationKey((WorldDefinition)world), out IPopulation? pop))
            return Result<IPopulation>.FromSuccess(pop!);

        if (!skipCacheRetrieval)
            _logger.LogWarning("Population was not retrieved from cache despite requesting it to be! Is the Census state worker not operating?");

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
