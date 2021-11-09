using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using RestSharp;
using RestSharp.Serializers.SystemTextJson;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Dto;
using UVOCBot.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services;

public sealed class DbApiService : ApiServiceBase<DbApiService>, IDbApiService
{
    public DbApiService(ILogger<DbApiService> logger, IOptions<GeneralOptions> options)
        : base(logger, () => new RestClient(options.Value.ApiEndpoint).UseSystemTextJson(new JsonSerializerOptions(JsonSerializerDefaults.Web)))
    { }

    public async Task<Result> ScaffoldDbEntries(IEnumerable<ulong> guildIds, CancellationToken ct = default)
    {
        foreach (ulong guild in guildIds)
        {
            Result<GuildTwitterSettingsDto> guildTwitterSettingsResult = await CreateGuildTwitterSettingsAsync(new GuildTwitterSettingsDto(guild), ct).ConfigureAwait(false);
            if (!guildTwitterSettingsResult.IsSuccess && !(guildTwitterSettingsResult.Error is HttpStatusCodeError er2 && er2.StatusCode == HttpStatusCode.Conflict))
            {
                _logger.LogCritical("Could not scaffold guild twitter settings database objects: {error}", guildTwitterSettingsResult.Error);
                return Result.FromError(guildTwitterSettingsResult);
            }
        }

        return Result.FromSuccess();
    }

    #region TwitterUser

    public async Task<Result<List<TwitterUserDto>>> ListTwitterUsersAsync(CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("twitteruser", Method.GET);

        return await ExecuteAsync<List<TwitterUserDto>>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<TwitterUserDto>> GetTwitterUserAsync(long id, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("twitteruser/{id}", Method.GET);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<TwitterUserDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<bool>> TwitterUserExistsAsync(long id, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("twitteruser/exists/{id}", Method.GET);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDto user, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("twitteruser/{id}", Method.PUT);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        request.AddJsonBody(user);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<TwitterUserDto>> CreateTwitterUserAsync(TwitterUserDto user, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("twitteruser", Method.POST);
        request.AddJsonBody(user);

        return await ExecuteAsync<TwitterUserDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> DeleteTwitterUserAsync(long id, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("twitteruser/{id}", Method.DELETE);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    #endregion

    #region GuildTwitterSettings

    /// <inheritdoc/>
    public async Task<Result<List<GuildTwitterSettingsDto>>> ListGuildTwitterSettingsAsync(bool filterByEnabled, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwittersettings", Method.GET);
        request.AddParameter("filterByEnabled", filterByEnabled, ParameterType.QueryString);

        return await ExecuteAsync<List<GuildTwitterSettingsDto>>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<GuildTwitterSettingsDto>> GetGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwittersettings/{id}", Method.GET);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<GuildTwitterSettingsDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<bool>> GuildTwitterSettingsExistsAsync(ulong id, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwittersettings/exists/{id}", Method.GET);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> UpdateGuildTwitterSettingsAsync(ulong id, GuildTwitterSettingsDto settings, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwittersettings/{id}", Method.PUT);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        request.AddJsonBody(settings);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<GuildTwitterSettingsDto>> CreateGuildTwitterSettingsAsync(GuildTwitterSettingsDto settings, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwittersettings", Method.POST);
        request.AddJsonBody(settings);

        return await ExecuteAsync<GuildTwitterSettingsDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> DeleteGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwittersettings/{id}", Method.DELETE);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    #endregion

    #region GuildTwitterLinks

    public async Task<Result> CreateGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwitterlinks", Method.POST);
        request.AddParameter("guildTwitterSettingsId", guildTwitterSettingsId, ParameterType.QueryString);
        request.AddParameter("twitterUserId", twitterUserId, ParameterType.QueryString);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> DeleteGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default)
    {
        IRestRequest request = new RestRequest("guildtwitterlinks", Method.DELETE);
        request.AddParameter("guildTwitterSettingsId", guildTwitterSettingsId, ParameterType.QueryString);
        request.AddParameter("twitterUserId", twitterUserId, ParameterType.QueryString);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    #endregion
}
