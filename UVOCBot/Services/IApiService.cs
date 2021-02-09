using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public interface IApiService
    {
        #region TwitterUser

        [Get("/twitteruser")]
        Task<List<TwitterUserDTO>> GetTwitterUsers();

        [Get("/twitteruser/{id}")]
        Task<TwitterUserDTO> GetTwitterUser(long id);

        [Get("/twitteruser/exists/{id}")]
        Task<bool> TwitterUserExists(long id);

        [Put("/twitteruser/{id}")]
        Task UpdateTwitterUser(long id, TwitterUserDTO user);

        [Post("/twitteruser")]
        Task<TwitterUserDTO> CreateTwitterUser(TwitterUserDTO user);

        [Delete("/twitteruser/{id}")]
        Task DeleteTwitterUser(long id);

        #endregion

        #region GuildTwitterSettings

        [Get("/guildtwittersettings")]
        Task<List<GuildTwitterSettingsDTO>> GetGuildTwitterSettings([Query] bool filterByEnabled);

        [Get("/guildtwittersettings/{id}")]
        Task<GuildTwitterSettingsDTO> GetGuildTwitterSetting(ulong id);

        [Get("/guildtwittersettings/exists/{id}")]
        Task<bool> GuildTwitterSettingsExists(ulong id);

        [Put("/guildtwittersettings/{id}")]
        Task UpdateGuildTwitterSetting(ulong id, GuildTwitterSettingsDTO settings);

        [Post("/guildtwittersettings")]
        Task<GuildTwitterSettingsDTO> CreateGuildTwitterSettings(GuildTwitterSettingsDTO settings);

        [Delete("/guildtwittersettings/{id}")]
        Task DeleteGuildTwitterSettings(ulong id);

        #endregion

        #region GuildTwitterLinks

        [Post("/guildtwitterlinks")]
        Task CreateGuildTwitterLink(ulong guildTwitterSettingsId, long twitterUserId);

        [Delete("/guildtwitterlinks")]
        Task DeleteGuildTwitterLink(ulong guildTwitterSettingsId, long twitterUserId);

        #endregion

        #region GuildSettings

        [Get("/guildsettings")]
        Task<List<GuildSettingsDTO>> GetGuildSettings([Query] bool hasPrefix = false);

        [Get("/guildsettings/{id}")]
        Task<GuildSettingsDTO> GetGuildSetting(ulong id);

        [Put("/guildsettings/{id}")]
        Task UpdateGuildSettings(ulong id, GuildSettingsDTO settings);

        [Post("/guildsettings")]
        Task<GuildSettingsDTO> CreateGuildSettings(GuildSettingsDTO settings);

        [Delete("/guildsettings/{id}")]
        Task DeleteGuildSettings(ulong id);

        #endregion
    }
}
