using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Model;
using UVOCBot.Model;

namespace UVOCBot.Services
{
    public class APIService : IAPIService
    {
        private readonly ILogger<APIService> _logger;
        private readonly IRestClient _client;

        public APIService(ILogger<APIService> logger, IOptions<GeneralOptions> options)
        {
            _logger = logger;
            _client = new RestClient(options.Value.ApiEndpoint);
        }

        public async Task<Result<T>> ExecuteAsync<T>(IRestRequest request, CancellationToken ct = default) where T : new()
        {
            IRestResponse<T> response = await _client.ExecuteAsync<T>(request, ct).ConfigureAwait(false);

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                _logger.LogError(response.ErrorException, "Failed to execute API operation: {exception}", response.ErrorMessage);
                return Result<T>.FromError(response.ErrorException);
            }

            if (response.StatusCode != HttpStatusCode.OK)
                return Result<T>.FromError(new HTTPStatusCodeError(response.StatusCode));

            return response.Data;
        }

        public async Task<Result> ExecuteAsync(IRestRequest request, CancellationToken ct = default)
        {
            IRestResponse response = await _client.ExecuteAsync(request, ct).ConfigureAwait(false);

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                _logger.LogError(response.ErrorException, "Failed to execute API operation: {exception}", response.ErrorMessage);
                return Result.FromError(new ExceptionError(response.ErrorException));
            }

            if (response.StatusCode != HttpStatusCode.NoContent)
                return Result.FromError(new HTTPStatusCodeError(response.StatusCode));

            return Result.FromSuccess();
        }

        #region TwitterUser

        public async Task<Result<List<TwitterUserDTO>>> ListTwitterUsersAsync(CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser", Method.GET);

            return await ExecuteAsync<List<TwitterUserDTO>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<TwitterUserDTO>> GetTwitterUserAsync(long id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser/{id}", Method.GET);
            request.AddParameter("id", id);

            return await ExecuteAsync<TwitterUserDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<bool>> TwitterUserExists(long id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser/exists/{id}", Method.GET);
            request.AddParameter("id", id);

            return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDTO user, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser/{id}", Method.PUT);
            request.AddParameter("id", id);

            request.AddJsonBody(user);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<TwitterUserDTO>> CreateTwitterUserAsync(TwitterUserDTO user, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser", Method.POST);

            return await ExecuteAsync<TwitterUserDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeleteTwitterUserAsync(long id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion

        #region GuildTwitterSettings

        public async Task<Result<List<GuildTwitterSettingsDTO>>> ListGuildTwitterSettingsAsync(bool filterByEnabled, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildtwittersettings", Method.GET);
            request.AddParameter("filterByEnabled", filterByEnabled, ParameterType.QueryString);

            return await ExecuteAsync<List<GuildTwitterSettingsDTO>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<GuildTwitterSettingsDTO>> GetGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildtwittersettings/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<GuildTwitterSettingsDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<bool>> GuildTwitterSettingsExistsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildtwittersettings/exists/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateGuildTwitterSettingAsync(ulong id, GuildTwitterSettingsDTO settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildtwittersettings/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);
            request.AddJsonBody(settings);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<GuildTwitterSettingsDTO>> CreateGuildTwitterSettingsAsync(GuildTwitterSettingsDTO settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildtwittersettings", Method.POST);
            request.AddJsonBody(settings);

            return await ExecuteAsync<GuildTwitterSettingsDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeleteGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildtwittersettings/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion
    }
}
