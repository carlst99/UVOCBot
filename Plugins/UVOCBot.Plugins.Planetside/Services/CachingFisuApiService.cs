using DbgCensus.Core.Objects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Remora.Results;
using System.Text.Json;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.Fisu;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Services
{
    /// <inheritdoc cref="IFisuApiService"/>
    public class CachingFisuApiService : IFisuApiService
    {
        private readonly PlanetsidePluginOptions _options;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;

        public CachingFisuApiService(
            IOptions<PlanetsidePluginOptions> options,
            IMemoryCache cache,
            HttpClient httpClient)
        {
            _options = options.Value;
            _cache = cache;
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<Result<Population>> GetWorldPopulationAsync(ValidWorldDefinition world, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKeyHelpers.GetFisuPopulationKey((WorldDefinition)world), out Population pop))
                return pop;

            Result<Population> popResult = await QueryPopulationAsync(world, ct).ConfigureAwait(false);
            if (!popResult.IsSuccess)
                return popResult;

            _cache.Set
            (
                CacheKeyHelpers.GetFisuPopulationKey(popResult.Entity),
                popResult.Entity,
                CacheEntryHelpers.GetFisuPopulationOptions()
            );

            return popResult;
        }

        private async Task<Result<Population>> QueryPopulationAsync(ValidWorldDefinition world, CancellationToken ct = default)
        {
            string queryUrl = $"{ _options.FisuApiEndpoint }/population?world={ (int)world }";

            using HttpResponseMessage response = await _httpClient.GetAsync(queryUrl, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new HttpRequestException(response.ReasonPhrase ?? "No reason provided.", null, response.StatusCode);

            string jsonString = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            Population? pop = JsonSerializer.Deserialize<Population>(jsonString, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (pop is null)
                return new InvalidOperationError("Population cannot be returned for that world.");

            return pop;
        }
    }
}
