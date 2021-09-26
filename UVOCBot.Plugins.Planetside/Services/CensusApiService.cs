using DbgCensus.Core.Exceptions;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System.Runtime.CompilerServices;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.Census;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Services
{
    /// <inheritdoc cref="ICensusApiService"/>
    public class CensusApiService : ICensusApiService
    {
        private readonly ILogger<CensusApiService> _logger;
        private readonly IQueryService _queryService;

        public CensusApiService(ILogger<CensusApiService> logger, IQueryService queryService)
        {
            _logger = logger;
            _queryService = queryService;
        }

        /// <inheritdoc />
        public virtual async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("map_region")
                .Where("facility_id", SearchModifier.Equals, facilityID);

            return await GetAsync<MapRegion>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual async Task<Result<List<Map>>> GetMaps(ValidWorldDefinition world, IEnumerable<ValidZoneDefinition> zones, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/map?world_id=1&zone_ids=2,4,6,8

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("map")
                .Where("world_id", SearchModifier.Equals, (int)world)
                .WhereAll("zone_ids", SearchModifier.Equals, zones.Select(z => (int)z));

            return await GetListAsync<Map>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual async Task<Result<List<MetagameEvent>>> GetMetagameEventsAsync(ValidWorldDefinition world, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/world_event?type=METAGAME&world_id=1&c:limit=20&c:sort=timestamp&c:join=world%5Einject_at:world

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("world_event")
                .Where("type", SearchModifier.Equals, "METAGAME")
                .Where("world_id", SearchModifier.Equals, (int)world)
                .WithLimit(20)
                .WithSortOrder("timestamp")
                .AddJoin("world", j => j.InjectAt("world"));

            return await GetListAsync<MetagameEvent>(query, ct).ConfigureAwait(false);
        }

        protected async Task<Result<T?>> GetAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
        {
            try
            {
                return await _queryService.GetAsync<T>(query, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Census query failed for query {query}.", callerName);
                return ex;
            }
        }

        protected async Task<Result<List<T>>> GetListAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
        {
            try
            {
                List<T>? result = await _queryService.GetAsync<List<T>>(query, ct).ConfigureAwait(false);

                if (result is null)
                    return new CensusException($"Census returned no data for query { callerName }.");
                else
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Census query failed for query {query}.", callerName);
                return ex;
            }
        }
    }
}
