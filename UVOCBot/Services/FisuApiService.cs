using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Model.Planetside;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public sealed class FisuApiService : ApiService<FisuApiService>, IFisuApiService
    {
        private readonly IMemoryCache _cache;

        public FisuApiService(ILogger<FisuApiService> logger, IOptions<GeneralOptions> options, IMemoryCache cache)
            : base(logger, () => new RestClient(options.Value.FisuApiEndpoint))
        {
            _cache = cache;
        }

        /// <inheritdoc/>
        public async Task<Result<FisuPopulation>> GetContinentPopulationAsync(WorldType worldId, CancellationToken ct = default)
        {
            // Try and get the population for this world from the cache
            if (_cache.TryGetValue(GetCacheKey(worldId), out FisuPopulation pop))
                return pop;

            // If there was no population value cached, make a request to fisu
            IRestRequest request = new RestRequest("population");
            request.AddParameter("world", worldId, ParameterType.QueryString);

            Result<FisuPopulation> population = await ExecuteAsync<FisuPopulation>(request, ct).ConfigureAwait(false);
            if (!population.IsSuccess)
                return population;

            // Update the cached population value
            _cache.Set(
                GetCacheKey(worldId),
                population.Entity,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Low
                });

            return population.Entity;
        }

        private static object GetCacheKey(WorldType worldId)
        {
            return (typeof(FisuPopulation), (int)worldId);
        }
    }
}
