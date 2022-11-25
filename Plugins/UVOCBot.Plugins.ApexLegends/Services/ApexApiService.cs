using Remora.Results;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Abstractions.Services;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Services;

public class ApexApiService : IApexApiService
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApexApiService(HttpClient client)
    {
        _client = client;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public virtual async Task<Result<MapRotationBundle>> GetMapRotationsAsync(CancellationToken ct = default)
    {
        try
        {
            Dictionary<string, MapRotation>? rotations = await _client
                .GetFromJsonAsync<Dictionary<string, MapRotation>>("maprotation", _jsonOptions, ct)
                .ConfigureAwait(false);

            if (rotations is null)
                return new ApexApiError("Failed to retrieve map rotations");

            if (!rotations.TryGetValue("current", out MapRotation? currentRotation))
                return new ApexApiError("Result did not contain the current map rotation");

            rotations.TryGetValue("next", out MapRotation? nextRotation);

            return new MapRotationBundle(currentRotation, nextRotation);
        }
        catch (Exception ex)
        {
            return Result<MapRotationBundle>.FromError(ex);
        }
    }

    public virtual async Task<Result<List<CraftingBundle>>> GetCraftingBundlesAsync(CancellationToken ct = default)
    {
        try
        {
            List<CraftingBundle>? bundles = await _client
                .GetFromJsonAsync<List<CraftingBundle>>("crafting", _jsonOptions, ct)
                .ConfigureAwait(false);

            if (bundles is null)
                return new ApexApiError("Failed to retrieve crafting information");

            return bundles;
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }
}
