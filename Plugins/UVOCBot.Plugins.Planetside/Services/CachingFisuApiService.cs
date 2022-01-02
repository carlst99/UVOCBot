using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Remora.Results;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.Services;

/// <summary>
/// <inheritdoc cref="IPopulationService"/>
/// Data is collected from fisu.
/// </summary>
public class CachingFisuApiService : CachingPopulationService
{
    private readonly HttpClient _httpClient;

    public CachingFisuApiService
    (
        IOptions<PlanetsidePluginOptions> options,
        IMemoryCache cache,
        HttpClient httpClient
    ) : base(options, cache)
    {
        _httpClient = httpClient;
    }

    protected override async Task<Result<IPopulation>> QueryPopulationAsync(ValidWorldDefinition world, CancellationToken ct = default)
    {
        string queryUrl = $"{ _options.FisuApiEndpoint }/population?world={ (int)world }";

        using HttpResponseMessage response = await _httpClient.GetAsync(queryUrl, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return new HttpRequestException(response.ReasonPhrase ?? "No reason provided.", null, response.StatusCode);

        string jsonString = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        FisuPopulation? pop = JsonSerializer.Deserialize<FisuPopulation>(jsonString, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (pop is null)
            return new InvalidOperationError("Population cannot be returned for that world.");

        return pop;
    }
}
