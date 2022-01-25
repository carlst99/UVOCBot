using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
        : base(logger, new RestClient(options.Value.ApiEndpoint).UseJson())
    {
    }

    public async Task<Result> ScaffoldDbEntries(IEnumerable<ulong> guildIds, CancellationToken ct = default)
    {
        foreach (ulong guild in guildIds)
        {
            Result<GuildTwitterSettingsDto> guildTwitterSettingsResult = await CreateGuildTwitterSettingsAsync(new GuildTwitterSettingsDto(guild), ct).ConfigureAwait(false);

            bool requestFailed = !guildTwitterSettingsResult.IsSuccess
                && !(guildTwitterSettingsResult.Error is HttpStatusCodeError er2 && er2.StatusCode is HttpStatusCode.Conflict)
                && !(guildTwitterSettingsResult.Error is ExceptionError exe && exe.Exception is HttpRequestException hex && hex.StatusCode is HttpStatusCode.Conflict);

            if (requestFailed)
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
        RestRequest request = new("twitteruser", Method.Get);

        return await ExecuteAsync<List<TwitterUserDto>>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<TwitterUserDto>> GetTwitterUserAsync(long id, CancellationToken ct = default)
    {
        RestRequest request = new("twitteruser/{id}", Method.Get);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<TwitterUserDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<bool>> TwitterUserExistsAsync(long id, CancellationToken ct = default)
    {
        RestRequest request = new("twitteruser/exists/{id}", Method.Get);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDto user, CancellationToken ct = default)
    {
        RestRequest request = new("twitteruser/{id}", Method.Put);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        request.AddJsonBody(user);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<TwitterUserDto>> CreateTwitterUserAsync(TwitterUserDto user, CancellationToken ct = default)
    {
        RestRequest request = new("twitteruser", Method.Post);
        request.AddJsonBody(user);

        return await ExecuteAsync<TwitterUserDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> DeleteTwitterUserAsync(long id, CancellationToken ct = default)
    {
        RestRequest request = new("twitteruser/{id}", Method.Delete);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    #endregion

    #region GuildTwitterSettings

    /// <inheritdoc/>
    public async Task<Result<List<GuildTwitterSettingsDto>>> ListGuildTwitterSettingsAsync(bool filterByEnabled, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwittersettings", Method.Get);
        request.AddParameter("filterByEnabled", filterByEnabled, ParameterType.QueryString);

        return await ExecuteAsync<List<GuildTwitterSettingsDto>>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<GuildTwitterSettingsDto>> GetGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwittersettings/{id}", Method.Get);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<GuildTwitterSettingsDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<bool>> GuildTwitterSettingsExistsAsync(ulong id, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwittersettings/exists/{id}", Method.Get);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> UpdateGuildTwitterSettingsAsync(ulong id, GuildTwitterSettingsDto settings, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwittersettings/{id}", Method.Put);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        request.AddJsonBody(settings);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<Result<GuildTwitterSettingsDto>> CreateGuildTwitterSettingsAsync(GuildTwitterSettingsDto settings, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwittersettings", Method.Post);
        request.AddJsonBody(settings);

        return await ExecuteAsync<GuildTwitterSettingsDto>(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> DeleteGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwittersettings/{id}", Method.Delete);
        request.AddParameter("id", id, ParameterType.UrlSegment);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    #endregion

    #region GuildTwitterLinks

    public async Task<Result> CreateGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwitterlinks", Method.Post);
        request.AddParameter("guildTwitterSettingsId", guildTwitterSettingsId, ParameterType.QueryString);
        request.AddParameter("twitterUserId", twitterUserId, ParameterType.QueryString);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<Result> DeleteGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default)
    {
        RestRequest request = new("guildtwitterlinks", Method.Delete);
        request.AddParameter("guildTwitterSettingsId", guildTwitterSettingsId, ParameterType.QueryString);
        request.AddParameter("twitterUserId", twitterUserId, ParameterType.QueryString);

        return await ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    #endregion
}
