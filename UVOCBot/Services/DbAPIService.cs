﻿using Microsoft.Extensions.Logging;
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
using UVOCBot.Core.Model;
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
                Result<GuildSettingsDTO> guildSettingsResult = await CreateGuildSettingsAsync(new GuildSettingsDTO(guild), ct).ConfigureAwait(false);
                if (!guildSettingsResult.IsSuccess && !(guildSettingsResult.Error is HttpStatusCodeError er && er.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold guild settings database objects: {error}", guildSettingsResult.Error);
                    return Result.FromError(guildSettingsResult);
                }

                Result<GuildTwitterSettingsDTO> guildTwitterSettingsResult = await CreateGuildTwitterSettingsAsync(new GuildTwitterSettingsDTO(guild), ct).ConfigureAwait(false);
                if (!guildTwitterSettingsResult.IsSuccess && !(guildTwitterSettingsResult.Error is HttpStatusCodeError er2 && er2.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold guild twitter settings database objects: {error}", guildSettingsResult.Error);
                    return Result.FromError(guildTwitterSettingsResult);
                }

                Result<PlanetsideSettingsDTO> planetsideSettingsResult = await CreatePlanetsideSettingsAsync(new PlanetsideSettingsDTO(guild), ct).ConfigureAwait(false);
                if (!planetsideSettingsResult.IsSuccess && !(planetsideSettingsResult.Error is HttpStatusCodeError er3 && er3.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold PlanetSide settings database objects: {error}", guildSettingsResult.Error);
                    return Result.FromError(planetsideSettingsResult);
                }

                Result<GuildWelcomeMessageDto> guildWelcomeMessageResult = await CreateGuildWelcomeMessageAsync(
                    new GuildWelcomeMessageDto(guild)
                    {
                        IsEnabled = false
                    },
                    ct).ConfigureAwait(false);

                if (!guildWelcomeMessageResult.IsSuccess && !(planetsideSettingsResult.Error is HttpStatusCodeError er4 && er4.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not initialise guild welcome message database objects: {error}", guildWelcomeMessageResult.Error);
                    return Result.FromError(guildWelcomeMessageResult);
                }
            }

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
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<TwitterUserDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<bool>> TwitterUserExistsAsync(long id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser/exists/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<bool>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDTO user, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(user);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<TwitterUserDTO>> CreateTwitterUserAsync(TwitterUserDTO user, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("twitteruser", Method.POST);
            request.AddJsonBody(user);

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

        /// <inheritdoc/>
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

        public async Task<Result> UpdateGuildTwitterSettingsAsync(ulong id, GuildTwitterSettingsDTO settings, CancellationToken ct = default)
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

        #region GuildSettings

        public async Task<Result<List<GuildSettingsDTO>>> ListGuildSettingsAsync(bool hasPrefix = false, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildsettings", Method.GET);
            request.AddParameter("hasPrefix", hasPrefix, ParameterType.QueryString);

            return await ExecuteAsync<List<GuildSettingsDTO>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<GuildSettingsDTO>> GetGuildSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildsettings/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<GuildSettingsDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateGuildSettingsAsync(ulong id, GuildSettingsDTO settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildsettings/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(settings);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<GuildSettingsDTO>> CreateGuildSettingsAsync(GuildSettingsDTO settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildsettings", Method.POST);
            request.AddJsonBody(settings);

            return await ExecuteAsync<GuildSettingsDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeleteGuildSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildsettings/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion

        #region PlanetsideSettings

        public async Task<Result<List<PlanetsideSettingsDTO>>> ListPlanetsideSettingsAsync(CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings", Method.GET);

            return await ExecuteAsync<List<PlanetsideSettingsDTO>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<PlanetsideSettingsDTO>> GetPlanetsideSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<PlanetsideSettingsDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdatePlanetsideSettingsAsync(ulong id, PlanetsideSettingsDTO settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(settings);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<PlanetsideSettingsDTO>> CreatePlanetsideSettingsAsync(PlanetsideSettingsDTO settings, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings", Method.POST);
            request.AddJsonBody(settings);

            return await ExecuteAsync<PlanetsideSettingsDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeletePlanetsideSettingsAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("planetsidesettings/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion

        #region MemberGroups

        public async Task<Result<MemberGroupDTO>> GetMemberGroupAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<MemberGroupDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<MemberGroupDTO>> GetMemberGroupAsync(ulong guildId, string groupName, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup", Method.GET);
            request.AddParameter("guildId", guildId, ParameterType.QueryString);
            request.AddParameter("groupName", groupName, ParameterType.QueryString);

            return await ExecuteAsync<MemberGroupDTO>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<List<MemberGroupDTO>>> ListGuildMemberGroupsAsync(ulong guildId, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/guildgroups/{guildId}", Method.GET);
            request.AddParameter("guildId", guildId, ParameterType.UrlSegment);

            return await ExecuteAsync<List<MemberGroupDTO>>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateMemberGroupAsync(ulong id, MemberGroupDTO group, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(group);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<MemberGroupDTO>> CreateMemberGroupAsync(MemberGroupDTO group, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("membergroup", Method.POST);

            request.AddJsonBody(group);

            return await ExecuteAsync<MemberGroupDTO>(request, ct).ConfigureAwait(false);
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

        #region GuildWelcomeMessage

        public async Task<Result<GuildWelcomeMessageDto>> GetGuildWelcomeMessageAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildwelcomemessage/{id}", Method.GET);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync<GuildWelcomeMessageDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> UpdateGuildWelcomeMessageAsync(ulong id, GuildWelcomeMessageDto welcomeMessage, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildwelcomemessage/{id}", Method.PUT);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            request.AddJsonBody(welcomeMessage);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        public async Task<Result<GuildWelcomeMessageDto>> CreateGuildWelcomeMessageAsync(GuildWelcomeMessageDto welcomeMessage, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildwelcomemessage", Method.POST);
            request.AddJsonBody(welcomeMessage);

            return await ExecuteAsync<GuildWelcomeMessageDto>(request, ct).ConfigureAwait(false);
        }

        public async Task<Result> DeleteGuildWelcomeMessageAsync(ulong id, CancellationToken ct = default)
        {
            IRestRequest request = new RestRequest("guildwelcomemessage/{id}", Method.DELETE);
            request.AddParameter("id", id, ParameterType.UrlSegment);

            return await ExecuteAsync(request, ct).ConfigureAwait(false);
        }

        #endregion
    }
}
