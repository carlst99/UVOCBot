using Microsoft.Extensions.Options;
using Remora.Results;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.Services;

/// <summary>
/// <inheritdoc cref="CachingPopulationService"/>
/// Data is collected from <see href="https://ps2.fisu.pw/api/population"/>
/// </summary>
internal sealed class FisuPopulationService : IPopulationService
{
    private readonly PlanetsidePluginOptions _options;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FisuPopulationService(IOptions<PlanetsidePluginOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<Result<IPopulation>> GetWorldPopulationAsync
    (
        ValidWorldDefinition world,
        bool skipCacheRetrieval = false,
        CancellationToken ct = default
    )
    {
        string queryUrl = $"{_options.FisuApiEndpoint}/population?world={(int)world}";

        using HttpResponseMessage response = await _httpClient.GetAsync(queryUrl, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return new HttpRequestException(response.ReasonPhrase ?? "No reason provided.", null, response.StatusCode);

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        FisuPopulation? pop = await JsonSerializer.DeserializeAsync<FisuPopulation>(contentStream, _jsonOptions, ct);

        if (pop is null)
            return new InvalidOperationError($"Population cannot be returned for the world {world}.");

        return pop;
    }
}
