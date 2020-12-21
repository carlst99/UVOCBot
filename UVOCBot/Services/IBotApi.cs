using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public interface IBotApi
    {
        #region TwitterUser

        [Get("/twitteruser")]
        Task<List<TwitterUserDTO>> GetTwitterUsers();

        [Get("/twitteruser/{id}")]
        Task<TwitterUserDTO> GetTwitterUser(long id);

        [Put("/twitteruser")]
        Task UpdateTwitterUser(TwitterUserDTO user);

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

        [Put("/guildtwittersettings")]
        Task UpdateGuildTwitterSetting(GuildTwitterSettingsDTO settings);

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
    }
}
