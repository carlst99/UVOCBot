using Microsoft.Extensions.Logging;
using Remora.Results;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;
using UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

namespace UVOCBot.Plugins.SpaceEngineers.Services;

public class VRageRemoteApi : IVRageRemoteApi
{
    private const string ADDRESS = "***REMOVED***";
    private const int PORT = 9898;
    private static readonly byte[] KEY = Convert.FromBase64String("***REMOVED***");

    private readonly ILogger<VRageRemoteApi> _logger;
    private readonly HttpClient _client;

    public VRageRemoteApi(ILogger<VRageRemoteApi> logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<Result<bool>> PingAsync(CancellationToken ct = default)
    {
        Result<RemoteResponseBase<PingData>?> result = await GetAsync<PingData>("v1/server/ping", ct);
        if (!result.IsSuccess)
            return Result<bool>.FromError(result);

        return result.Entity?.Data.Result is "Pong";
    }

    private async Task<Result<RemoteResponseBase<T>?>> GetAsync<T>
    (
        string resource,
        CancellationToken ct = default,
        [CallerMemberName] string? callerName = null
    )
    {
        try
        {
            HttpRequestMessage request = CreateRequest(resource, HttpMethod.Get);
            HttpResponseMessage response = await _client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RemoteResponseBase<T>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VRage Remote API query failed ({Query})", callerName);
            return ex;
        }
    }

    private static HttpRequestMessage CreateRequest
    (
        string resourceLink,
        HttpMethod method,
        params (string Parameter, string Value)[] queryParams
    )
    {
        string methodUrl = $"/vrageremote/{resourceLink}";
        string parameters = string.Join('&', queryParams.Select(x => $"{x.Parameter}={x.Value}"));
        DateTimeOffset date = DateTimeOffset.UtcNow;
        string nonce = Random.Shared.Next().ToString();

        UriBuilder builder = new("http", ADDRESS, PORT, methodUrl)
        {
            Query = parameters
        };

        HttpRequestMessage request = new(method, builder.Uri);
        request.Headers.Date = date;

        StringBuilder message = new();
        message.Append(methodUrl);

        if (parameters.Length > 0)
            message.Append('?').Append(parameters);

        message.AppendLine();
        message.AppendLine(nonce);
        message.AppendLine(date.ToString("R", CultureInfo.InvariantCulture));

        byte[] messageBuffer = Encoding.UTF8.GetBytes(message.ToString());
        byte[] computedHash = HMACSHA1.HashData(KEY, messageBuffer);
        string hash = Convert.ToBase64String(computedHash);

        bool added = request.Headers.TryAddWithoutValidation("Authorization", $"{nonce}:{hash}");
        if (!added)
            throw new Exception("Failed to add the authorization header");

        return request;
    }
}
