using DbgCensus.Rest;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Services;

public class SanctuaryPopulationService : BaseCachingPopulationService
{
    private readonly IQueryService _queryService;
    private readonly CensusQueryOptions _sanctuaryOptions;
    private readonly HonuPopulationService _honuPopulationService;

    public SanctuaryPopulationService
    (
        ILogger<SanctuaryPopulationService> logger,
        IMemoryCache cache,
        IQueryService queryService,
        IOptionsMonitor<CensusQueryOptions> queryOptions,
        HonuPopulationService honuPopulationService
    ) : base(logger, cache)
    {
        _queryService = queryService;
        _sanctuaryOptions = queryOptions.Get("sanctuary");
        _honuPopulationService = honuPopulationService;
    }

    /// <inheritdoc />
    protected override async Task<Result<IPopulation>> QueryPopulationAsync
    (
        ValidWorldDefinition world,
        CancellationToken ct
    )
    {
        IQueryBuilder query = _queryService.CreateQuery(_sanctuaryOptions)
            .OnCollection("world_population")
            .Where("world_id", SearchModifier.Equals, (int)world);

        WorldPopulation? population = await _queryService.GetAsync<WorldPopulation>(query, ct);
        if (population is null)
            return await _honuPopulationService.GetWorldPopulationAsync(world, ct: ct);

        return population.Timestamp.AddMinutes(5) < DateTimeOffset.UtcNow
            ? await _honuPopulationService.GetWorldPopulationAsync(world, ct: ct)
            : population;
    }
}
