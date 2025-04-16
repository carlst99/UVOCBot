using Microsoft.Extensions.Logging;
using Remora.Results;
using System;
using System.Collections.Generic;
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
using UVOCBot.Plugins.SpaceEngineers.Objects;
using UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

namespace UVOCBot.Plugins.SpaceEngineers.Services;

public class VRageRemoteApi : IVRageRemoteApi
{
    private const string NEWLINE = "\r\n";
    private static readonly TimeSpan API_TIMEOUT = TimeSpan.FromSeconds(3);

    private readonly ILogger<VRageRemoteApi> _logger;
    private readonly HttpClient _client;

    public VRageRemoteApi(ILogger<VRageRemoteApi> logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> PingAsync
    (
        SEServerConnectionDetails connectionDetails,
        CancellationToken ct = default
    )
    {
        Result<RemoteResponseBase<PingData>?> result = await GetAsync<PingData>
        (
            connectionDetails,
            "v1/server/ping",
            ct
        );

        if (!result.IsSuccess)
            return Result<bool>.FromError(result);

        return result.Entity?.Data.Result is "Pong";
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Player>>> GetPlayersAsync
    (
        SEServerConnectionDetails connectionDetails,
        CancellationToken ct = default
    )
    {
        Result<RemoteResponseBase<WorldPlayersData>?> result = await GetAsync<WorldPlayersData>
        (
            connectionDetails,
            "v1/session/players",
            ct
        );

        if (!result.IsSuccess)
            return Result<IReadOnlyList<Player>>.FromError(result);

        return result.IsDefined(out RemoteResponseBase<WorldPlayersData>? data)
            ? Result<IReadOnlyList<Player>>.FromSuccess(data.Data.Players)
            : Array.Empty<Player>();
    }

    private async Task<Result<RemoteResponseBase<T>?>> GetAsync<T>
    (
        SEServerConnectionDetails connectionDetails,
        string resource,
        CancellationToken ct = default,
        [CallerMemberName] string? callerName = null
    )
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(API_TIMEOUT);

        try
        {
            HttpRequestMessage request = CreateRequest(connectionDetails, resource, HttpMethod.Get);
            HttpResponseMessage response = await _client.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RemoteResponseBase<T>>(cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            // This is fine
            return new VRageTimeoutError();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VRage Remote API query failed ({Query})", callerName);
            return ex;
        }
    }

    private static HttpRequestMessage CreateRequest
    (
        SEServerConnectionDetails connectionDetails,
        string resourceLink,
        HttpMethod method,
        params (string Parameter, string Value)[] queryParams
    )
    {
        string methodUrl = $"/vrageremote/{resourceLink}";
        string parameters = string.Join('&', queryParams.Select(x => $"{x.Parameter}={x.Value}"));
        DateTimeOffset date = DateTimeOffset.UtcNow;
        string nonce = Random.Shared.Next().ToString();

        UriBuilder builder = new("http", connectionDetails.Address, connectionDetails.Port, methodUrl)
        {
            Query = parameters
        };

        HttpRequestMessage request = new(method, builder.Uri);
        request.Headers.Date = date;

        StringBuilder message = new();
        message.Append(methodUrl);

        if (parameters.Length > 0)
            message.Append('?').Append(parameters);

        message.Append(NEWLINE);
        message.Append(nonce).Append(NEWLINE);
        message.Append(date.ToString("R", CultureInfo.InvariantCulture)).Append(NEWLINE);

        byte[] messageBuffer = Encoding.UTF8.GetBytes(message.ToString());
        byte[] key = Convert.FromBase64String(connectionDetails.Key);
        byte[] computedHash = HMACSHA1.HashData(key, messageBuffer);
        string hash = Convert.ToBase64String(computedHash);

        bool added = request.Headers.TryAddWithoutValidation("Authorization", $"{nonce}:{hash}");
        if (!added)
            throw new Exception("Failed to add the authorization header");

        return request;
    }
}
