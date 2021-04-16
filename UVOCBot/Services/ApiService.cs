﻿using Microsoft.Extensions.Logging;
using Remora.Results;
using RestSharp;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot.Services
{
    public abstract class ApiService<TLoggerType>
    {
        protected readonly ILogger<TLoggerType> _logger;
        protected readonly IRestClient _client;

        protected ApiService(ILogger<TLoggerType> logger, Func<IRestClient> configureClient)
        {
            _logger = logger;
            _client = configureClient();
        }

        protected async Task<Result<T>> ExecuteAsync<T>(IRestRequest request, CancellationToken ct = default) where T : new()
        {
            IRestResponse<T> response = await _client.ExecuteAsync<T>(request, ct).ConfigureAwait(false);

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                _logger.LogError(response.ErrorException, "Failed to execute API operation: {exception}", response.ErrorMessage);
                return Result<T>.FromError(response.ErrorException);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return Result<T>.FromError(new Model.HttpStatusCode((System.Net.HttpStatusCode)response.StatusCode));

            return response.Data;
        }

        protected async Task<Result> ExecuteAsync(IRestRequest request, CancellationToken ct = default)
        {
            IRestResponse response = await _client.ExecuteAsync(request, ct).ConfigureAwait(false);

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                _logger.LogError(response.ErrorException, "Failed to execute API operation: {exception}", response.ErrorMessage);
                return Result.FromError(new ExceptionError(response.ErrorException));
            }

            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
                return Result.FromError(new Model.HttpStatusCode((System.Net.HttpStatusCode)response.StatusCode));

            return Result.FromSuccess();
        }
    }
}