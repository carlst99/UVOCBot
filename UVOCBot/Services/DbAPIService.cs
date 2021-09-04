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

namespace UVOCBot.Services
{
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

                Result<PlanetsideSettingsDto> planetsideSettingsResult = await CreatePlanetsideSettingsAsync(new PlanetsideSettingsDto(guild), ct).ConfigureAwait(false);
                if (!planetsideSettingsResult.IsSuccess && !(planetsideSettingsResult.Error is HttpStatusCodeError er3 && er3.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold PlanetSide settings database objects: {error}", planetsideSettingsResult.Error);
                    return Result.FromError(planetsideSettingsResult);
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

        #region PlanetsideSettings

        public async Task<Result<List<PlanetsideSettingsDto>>> ListPlanetsideSettingsAsync(CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings", Method.GET);

            return await ExecuteAsync<List<PlanetsideSettingsDto>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<PlanetsideSettingsDto>> GetPlanetsideSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<PlanetsideSettingsDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdatePlanetsideSettingsAsync(ulong id, PlanetsideSettingsDto settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(settings);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<PlanetsideSettingsDto>> CreatePlanetsideSettingsAsync(PlanetsideSettingsDto settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings", Method.POST);
            request.AddJsonBody(settings);

            return await ExecuteAsync<PlanetsideSettingsDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeletePlanetsideSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion

        #region MemberGroups

        public async Task<Result<MemberGroupDto>> GetMemberGroupAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<MemberGroupDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<MemberGroupDto>> GetMemberGroupAsync(ulong guildId, string groupName, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup", Method.GET);
            request.AddParameter("guildId", guildId, ParameterType.QueryString);
            request.AddParameter("groupName", groupName, ParameterType.QueryString);

            return await ExecuteAsync<MemberGroupDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<List<MemberGroupDto>>> ListGuildMemberGroupsAsync(ulong guildId, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/guildgroups/{guildId}", Method.GET);
            request.AddParameter("guildId", guildId, ParameterType.UrlSegment);

            return await ExecuteAsync<List<MemberGroupDto>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateMemberGroupAsync(ulong id, MemberGroupDto group, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(group);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<MemberGroupDto>> CreateMemberGroupAsync(MemberGroupDto group, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup", Method.POST);

            request.AddJsonBody(group);

            return await ExecuteAsync<MemberGroupDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeleteMemberGroupAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeleteMemberGroupAsync(ulong guildId, string groupName, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup", Method.DELETE);
            request.AddParameter("guildId", guildId, ParameterType.QueryString);
            request.AddParameter("groupName", groupName, ParameterType.QueryString);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion
    }
}
