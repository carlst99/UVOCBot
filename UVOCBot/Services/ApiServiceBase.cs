using Microsoft.Extensions.Logging;
using Remora.Results;
using RestSharp;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot.Services
{
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
                _logger.LogError(response.ErrorException, "Failed to execute database API operation: {exception}", response.ErrorMessage);
                return Result<T>.FromError(response.ErrorException);
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
                _logger.LogError(response.ErrorException, "Failed to execute database API operation: {exception}", response.ErrorMessage);
                return Result.FromError(new ExceptionError(response.ErrorException));
            }

            if (response.StatusCode != HttpStatusCode.NoContent)
                return Result.FromError(new HttpStatusCodeError(response.StatusCode));

            return Result.FromSuccess();
        }
    }
}
