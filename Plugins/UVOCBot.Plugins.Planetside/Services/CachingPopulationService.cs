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
internal sealed class CachingPopulationService : IPopulationService
{
    private readonly ILogger<CachingPopulationService> _logger;
    private readonly IPopulationService _basePopService;
    private readonly IMemoryCache _cache;

    public CachingPopulationService
    (
        ILogger<CachingPopulationService> logger,
        IPopulationService basePopService,
        IMemoryCache cache
    )
    {
        _logger = logger;
        _basePopService = basePopService;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<IPopulation>> GetWorldPopulationAsync
    (
        ValidWorldDefinition world,
        bool skipCacheRetrieval = false,
        CancellationToken ct = default
    )
    {
        if (!skipCacheRetrieval &&
            _cache.TryGetValue(CacheKeyHelpers.GetPopulationKey((WorldDefinition)world), out IPopulation? pop))
            return Result<IPopulation>.FromSuccess(pop!);

        if (!skipCacheRetrieval)
        {
            _logger.LogWarning
            (
                "Population for {World} was not retrieved from cache despite requesting it to be! Is the Census "
                    + "state worker not operating?",
                world
            );
        }

        Result<IPopulation> popResult = await _basePopService.GetWorldPopulationAsync(world, skipCacheRetrieval, ct);
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
}
