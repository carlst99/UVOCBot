using Microsoft.Extensions.Logging;
using Remora.Results;
using RestSharp;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot.Services;

public abstract class ApiServiceBase<TLoggerType>
{
    protected readonly ILogger<TLoggerType> _logger;
    protected readonly IRestClient _client;

    protected ApiServiceBase(ILogger<TLoggerType> logger, Func<IRestClient> configureClient)
    {
        _logger = logger;
        _client = configureClient();
    }

    protected async Task<Result<T>> ExecuteAsync<T>(IRestRequest request, CancellationToken ct = default) where T : new()
    {
        IRestResponse<T> response = await _client.ExecuteAsync<T>(request, ct).ConfigureAwait(false);

        if (response.ResponseStatus != ResponseStatus.Completed)
        {
            Exception ex = new($"API query failed with message { response.ErrorMessage }. Response status: { response.ResponseStatus }");
            if (response.ErrorException is not null)
                ex = response.ErrorException;

            _logger.LogError(ex, "Failed to execute API operation with status {status} and reason {message}", response.ResponseStatus, response.ErrorMessage);
            return ex;
        }

        if (request.Method == Method.GET && response.StatusCode == HttpStatusCode.OK)
            return response.Data;

        if (request.Method == Method.POST && response.StatusCode == HttpStatusCode.Created)
            return response.Data;

        return Result<T>.FromError(new HttpStatusCodeError(response.StatusCode));
    }

    protected async Task<Result> ExecuteAsync(IRestRequest request, CancellationToken ct = default)
    {
        IRestResponse response = await _client.ExecuteAsync(request, ct).ConfigureAwait(false);

        if (response.ResponseStatus != ResponseStatus.Completed)
        {
            Exception ex = new($"API query failed with message { response.ErrorMessage }. Response status: { response.ResponseStatus }");
            if (response.ErrorException is not null)
                ex = response.ErrorException;

            _logger.LogError(ex, "Failed to execute API operation with {status} and reason {message}", response.ResponseStatus, response.ErrorMessage);
            return ex;
        }

        if (response.StatusCode != HttpStatusCode.NoContent)
            return Result.FromError(new HttpStatusCodeError(response.StatusCode));

        return Result.FromSuccess();
    }
}
