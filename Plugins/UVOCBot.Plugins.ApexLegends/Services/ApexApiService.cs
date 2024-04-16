using Remora.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
            HttpResponseMessage result = await _client.GetAsync("maprotation", ct)
                .ConfigureAwait(false);

            Result<Dictionary<string, MapRotation>> getRotations = await ParseApiResult<Dictionary<string, MapRotation>>(result, ct)
                .ConfigureAwait(false);

            if (!getRotations.IsDefined(out Dictionary<string, MapRotation>? rotations))
                return Result<MapRotationBundle>.FromError(getRotations);

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

    private async Task<Result<T>> ParseApiResult<T>(HttpResponseMessage response, CancellationToken ct)
    {
        response.EnsureSuccessStatusCode();

        Stream contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        JsonDocument document = await JsonDocument.ParseAsync(contentStream, default, ct).ConfigureAwait(false);

        JsonElement errorElement = default;
        bool hasErrorValue = document.RootElement.ValueKind is JsonValueKind.Object
            && document.RootElement.TryGetProperty("Error", out errorElement);

        if (hasErrorValue)
            return new ApexApiError(errorElement.GetString() ?? "Unknown error response");

        return document.Deserialize<T>(_jsonOptions);
    }
}
