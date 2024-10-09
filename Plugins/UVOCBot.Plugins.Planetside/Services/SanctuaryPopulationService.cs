using DbgCensus.Rest;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Services;

internal sealed class SanctuaryPopulationService : IPopulationService
{
    private readonly ILogger<SanctuaryPopulationService> _logger;
    private readonly IQueryService _queryService;
    private readonly CensusQueryOptions _sanctuaryOptions;
    private readonly IPopulationService _fallbackPopService;

    public SanctuaryPopulationService
    (
        ILogger<SanctuaryPopulationService> logger,
        IQueryService queryService,
        IOptionsMonitor<CensusQueryOptions> queryOptions,
        IPopulationService fallbackPopService
    )
    {
        _logger = logger;
        _queryService = queryService;
        _sanctuaryOptions = queryOptions.Get("sanctuary");
        _fallbackPopService = fallbackPopService;
    }

    public async Task<Result<IPopulation>> GetWorldPopulationAsync
    (
        ValidWorldDefinition world,
        bool skipCacheRetrieval = false,
        CancellationToken ct = default
    )
    {
        IQueryBuilder query = _queryService.CreateQuery(_sanctuaryOptions)
            .OnCollection("world_population")
            .Where("world_id", SearchModifier.Equals, (int)world);

        WorldPopulation? population = await _queryService.GetAsync<WorldPopulation>(query, ct);
        if (population is not null && population.Timestamp.AddMinutes(5) >= DateTimeOffset.UtcNow)
            return population;

        _logger.LogWarning
        (
            "Sanctuary population for {World} is out-of-date (last updated at {Time}); using fallback pop service",
            world,
            population?.Timestamp
        );
        return await _fallbackPopService.GetWorldPopulationAsync(world, skipCacheRetrieval, ct: ct);

    }
}
