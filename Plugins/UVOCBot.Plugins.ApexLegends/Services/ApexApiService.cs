using Remora.Results;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Abstractions.Services;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Services;

public class ApexApiService : IApexApiService
{
    private readonly HttpClient _client;

    public ApexApiService(HttpClient client)
    {
        _client = client;
    }

    public virtual async Task<Result<MapRotationBundle>> GetMapRotationsAsync(CancellationToken ct = default)
    {
        try
        {
            Dictionary<string, MapRotation>? rotations = await _client
                .GetFromJsonAsync<Dictionary<string, MapRotation>>("maprotation", ct)
                .ConfigureAwait(false);

            if (rotations is null)
                throw new Exception("Failed to retrieve map rotations");

            if (!rotations.TryGetValue("current", out MapRotation? currentRotation))
                throw new Exception("Failed to retrieve current rotation");

            rotations.TryGetValue("next", out MapRotation? nextRotation);

            return new MapRotationBundle(currentRotation, nextRotation);
        }
        catch (Exception ex)
        {
            return Result<MapRotationBundle>.FromError(ex);
        }
    }
}
