using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services.Abstractions
{
    public interface IDbApiService
    {
        Task<Result> ScaffoldDbEntries(IEnumerable<ulong> guildIds, CancellationToken ct = default);

        #region TwitterUser

        Task<Result<List<TwitterUserDTO>>> ListTwitterUsersAsync(CancellationToken ct = default);

        Task<Result<TwitterUserDTO>> GetTwitterUserAsync(long id, CancellationToken ct = default);

        Task<Result<bool>> TwitterUserExistsAsync(long id, CancellationToken ct = default);

        Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDTO user, CancellationToken ct = default);

        Task<Result<TwitterUserDTO>> CreateTwitterUserAsync(TwitterUserDTO user, CancellationToken ct = default);

        Task<Result> DeleteTwitterUserAsync(long id, CancellationToken ct = default);

        #endregion

        #region GuildTwitterSettings

        /// <summary>
        /// Lists guild twitter settings.
        /// </summary>
        /// <param name="filterByEnabled">If true, only returns guilds that have tweet relaying enabled, and are relaying from at least one user.</param>
        /// <param name="ct">A token with which to cancel any asynchronous operations.</param>
        /// <returns>A list of <see cref="GuildTwitterSettingsDTO"/> objects.</returns>
        Task<Result<List<GuildTwitterSettingsDTO>>> ListGuildTwitterSettingsAsync(bool filterByEnabled, CancellationToken ct = default);

        Task<Result<GuildTwitterSettingsDTO>> GetGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default);

        Task<Result<bool>> GuildTwitterSettingsExistsAsync(ulong id, CancellationToken ct = default);

        Task<Result> UpdateGuildTwitterSettingsAsync(ulong id, GuildTwitterSettingsDTO settings, CancellationToken ct = default);

        Task<Result<GuildTwitterSettingsDTO>> CreateGuildTwitterSettingsAsync(GuildTwitterSettingsDTO settings, CancellationToken ct = default);

        Task<Result> DeleteGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default);

        #endregion

        #region GuildTwitterLinks

        Task<Result> CreateGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default);

        Task<Result> DeleteGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default);

        #endregion

        #region GuildSettings

        Task<Result<List<GuildSettingsDTO>>> ListGuildSettingsAsync(bool hasPrefix = false, CancellationToken ct = default);

        Task<Result<GuildSettingsDTO>> GetGuildSettingsAsync(ulong id, CancellationToken ct = default);

        Task<Result> UpdateGuildSettingsAsync(ulong id, GuildSettingsDTO settings, CancellationToken ct = default);

        Task<Result<GuildSettingsDTO>> CreateGuildSettingsAsync(GuildSettingsDTO settings, CancellationToken ct = default);

        Task<Result> DeleteGuildSettingsAsync(ulong id, CancellationToken ct = default);

        #endregion

        #region PlanetsideSettings

        Task<Result<List<PlanetsideSettingsDTO>>> ListPlanetsideSettingsAsync(CancellationToken ct = default);

        Task<Result<PlanetsideSettingsDTO>> GetPlanetsideSettingsAsync(ulong id, CancellationToken ct = default);

        Task<Result> UpdatePlanetsideSettingsAsync(ulong id, PlanetsideSettingsDTO settings, CancellationToken ct = default);

        Task<Result<PlanetsideSettingsDTO>> CreatePlanetsideSettingsAsync(PlanetsideSettingsDTO settings, CancellationToken ct = default);

        Task<Result> DeletePlanetsideSettingsAsync(ulong id, CancellationToken ct = default);

        #endregion

        #region MemberGroups

        Task<Result<MemberGroupDTO>> GetMemberGroupAsync(ulong id, CancellationToken ct = default);

        Task<Result<MemberGroupDTO>> GetMemberGroupAsync(ulong guildId, string groupName, CancellationToken ct = default);

        Task<Result<List<MemberGroupDTO>>> ListGuildMemberGroupsAsync(ulong guildId, CancellationToken ct = default);

        Task<Result> UpdateMemberGroupAsync(ulong id, MemberGroupDTO group, CancellationToken ct = default);

        Task<Result<MemberGroupDTO>> CreateMemberGroupAsync(MemberGroupDTO group, CancellationToken ct = default);

        Task<Result> DeleteMemberGroupAsync(ulong id, CancellationToken ct = default);

        Task<Result> DeleteMemberGroupAsync(ulong guildId, string groupName, CancellationToken ct = default);

        #endregion

        #region GuildWelcomeMessage

        Task<Result<GuildWelcomeMessageDto>> GetGuildWelcomeMessageAsync(ulong id, CancellationToken ct = default);

        Task<Result> UpdateGuildWelcomeMessageAsync(ulong id, GuildWelcomeMessageDto welcomeMessage, CancellationToken ct = default);

        Task<Result<GuildWelcomeMessageDto>> CreateGuildWelcomeMessageAsync(GuildWelcomeMessageDto welcomeMessage, CancellationToken ct = default);

        Task<Result> DeleteGuildWelcomeMessageAsync(ulong id, CancellationToken ct = default);

        #endregion
    }
}
