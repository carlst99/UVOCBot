using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Dto;

namespace UVOCBot.Services.Abstractions
{
    public interface IDbApiService
    {
        Task<Result> ScaffoldDbEntries(IEnumerable<ulong> guildIds, CancellationToken ct = default);

        #region TwitterUser

        Task<Result<List<TwitterUserDto>>> ListTwitterUsersAsync(CancellationToken ct = default);

        Task<Result<TwitterUserDto>> GetTwitterUserAsync(long id, CancellationToken ct = default);

        Task<Result<bool>> TwitterUserExistsAsync(long id, CancellationToken ct = default);

        Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDto user, CancellationToken ct = default);

        Task<Result<TwitterUserDto>> CreateTwitterUserAsync(TwitterUserDto user, CancellationToken ct = default);

        Task<Result> DeleteTwitterUserAsync(long id, CancellationToken ct = default);

        #endregion

        #region GuildTwitterSettings

        /// <summary>
        /// Lists guild twitter settings.
        /// </summary>
        /// <param name="filterByEnabled">If true, only returns guilds that have tweet relaying enabled, and are relaying from at least one user.</param>
        /// <param name="ct">A token with which to cancel any asynchronous operations.</param>
        /// <returns>A list of <see cref="GuildTwitterSettingsDTO"/> objects.</returns>
        Task<Result<List<GuildTwitterSettingsDto>>> ListGuildTwitterSettingsAsync(bool filterByEnabled, CancellationToken ct = default);

        Task<Result<GuildTwitterSettingsDto>> GetGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default);

        Task<Result<bool>> GuildTwitterSettingsExistsAsync(ulong id, CancellationToken ct = default);

        Task<Result> UpdateGuildTwitterSettingsAsync(ulong id, GuildTwitterSettingsDto settings, CancellationToken ct = default);

        Task<Result<GuildTwitterSettingsDto>> CreateGuildTwitterSettingsAsync(GuildTwitterSettingsDto settings, CancellationToken ct = default);

        Task<Result> DeleteGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default);

        #endregion

        #region GuildTwitterLinks

        Task<Result> CreateGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default);

        Task<Result> DeleteGuildTwitterLinkAsync(ulong guildTwitterSettingsId, long twitterUserId, CancellationToken ct = default);

        #endregion
    }
}
