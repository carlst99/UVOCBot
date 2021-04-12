using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public interface IAPIService
    {
        #region TwitterUser

        Task<Result<List<TwitterUserDTO>>> ListTwitterUsersAsync(CancellationToken ct = default);

        Task<Result<TwitterUserDTO>> GetTwitterUserAsync(long id, CancellationToken ct = default);

        Task<Result<bool>> TwitterUserExistsAsync(long id, CancellationToken ct = default);

        Task<Result> UpdateTwitterUserAsync(long id, TwitterUserDTO user, CancellationToken ct = default);

        Task<Result<TwitterUserDTO>> CreateTwitterUserAsync(TwitterUserDTO user, CancellationToken ct = default);

        Task<Result> DeleteTwitterUserAsync(long id, CancellationToken ct = default);

        #endregion

        #region GuildTwitterSettings

        Task<Result<List<GuildTwitterSettingsDTO>>> ListGuildTwitterSettingsAsync(bool filterByEnabled, CancellationToken ct = default);

        Task<Result<GuildTwitterSettingsDTO>> GetGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default);

        Task<Result<bool>> GuildTwitterSettingsExistsAsync(ulong id, CancellationToken ct = default);

        Task<Result> UpdateGuildTwitterSettingAsync(ulong id, GuildTwitterSettingsDTO settings, CancellationToken ct = default);

        Task<Result<GuildTwitterSettingsDTO>> CreateGuildTwitterSettingsAsync(GuildTwitterSettingsDTO settings, CancellationToken ct = default);

        Task<Result> DeleteGuildTwitterSettingsAsync(ulong id, CancellationToken ct = default);

        #endregion

        #region GuildTwitterLinks

        [Post("/guildtwitterlinks")]
        Task CreateGuildTwitterLink(ulong guildTwitterSettingsId, long twitterUserId);

        [Delete("/guildtwitterlinks")]
        Task DeleteGuildTwitterLink(ulong guildTwitterSettingsId, long twitterUserId);

        #endregion

        #region GuildSettings

        [Get("/guildsettings")]
        Task<List<GuildSettingsDTO>> GetAllGuildSettings([Query] bool hasPrefix = false);

        [Get("/guildsettings/{id}")]
        Task<GuildSettingsDTO> GetGuildSettings(ulong id);

        [Put("/guildsettings/{id}")]
        Task UpdateGuildSettings(ulong id, GuildSettingsDTO settings);

        [Post("/guildsettings")]
        Task<GuildSettingsDTO> CreateGuildSettings(GuildSettingsDTO settings);

        [Delete("/guildsettings/{id}")]
        Task DeleteGuildSettings(ulong id);

        #endregion

        #region PlanetsideSettings

        [Get("/planetsidesettings")]
        Task<List<PlanetsideSettingsDTO>> GetAllPlanetsideSettings();

        [Get("/planetsidesettings/{id}")]
        Task<PlanetsideSettingsDTO> GetPlanetsideSettings(ulong id);

        [Put("/planetsidesettings/{id}")]
        Task UpdatePlanetsideSettings(ulong id, PlanetsideSettingsDTO settings);

        [Post("/planetsidesettings")]
        Task<PlanetsideSettingsDTO> CreatePlanetsideSettings(PlanetsideSettingsDTO settings);

        [Delete("/planetsidesettings/{id}")]
        Task DeletePlanetsideSettings(ulong id);

        #endregion

        #region MemberGroups

        [Get("/membergroup/{id}")]
        Task<MemberGroupDTO> GetMemberGroup(ulong id);

        [Get("/membergroup")]
        Task<MemberGroupDTO> GetMemberGroup(ulong guildId, string groupName);

        [Get("/membergroup/guildgroups/{guildId}")]
        Task<List<MemberGroupDTO>> GetAllGuildMemberGroups(ulong guildId);

        [Put("/membergroup/{id}")]
        Task UpdateMemberGroup(ulong id, MemberGroupDTO group);

        [Post("/membergroup")]
        Task<MemberGroupDTO> CreateMemberGroup(MemberGroupDTO group);

        [Delete("/membergroup/{id}")]
        Task DeleteMemberGroup(ulong id);

        [Delete("/membergroup")]
        Task DeleteMemberGroup(ulong guildId, string groupName);

        #endregion
    }
}
