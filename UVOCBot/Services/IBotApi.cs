using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public interface IBotApi
    {
        #region TwitterUser

        public const string TWITTER_USER_ROUTE = "twitteruser";

        [Get(TWITTER_USER_ROUTE)]
        Task<List<TwitterUserDTO>> GetTwitterUsers();

        [Get(TWITTER_USER_ROUTE + "/{id}")]
        Task<TwitterUserDTO> GetTwitterUser(long id);

        [Put(TWITTER_USER_ROUTE)]
        Task UpdateTwitterUser(TwitterUserDTO user);

        [Post(TWITTER_USER_ROUTE)]
        Task<TwitterUserDTO> CreateTwitterUser(TwitterUserDTO user);

        [Delete(TWITTER_USER_ROUTE + "/{id}")]
        Task DeleteTwitterUser(long id);

        #endregion

        #region GuildTwitterSettings

        public const string GUILD_TWITTER_SETTINGS_ROUTE = "guildtwittersettings";

        [Get(GUILD_TWITTER_SETTINGS_ROUTE)]
        Task<List<GuildTwitterSettingsDTO>> GetGuildTwitterSettings([Query] bool filterByEnabled);

        [Get(GUILD_TWITTER_SETTINGS_ROUTE + "/{id}")]
        Task<GuildTwitterSettingsDTO> GetGuildTwitterSetting(ulong id);

        [Put(GUILD_TWITTER_SETTINGS_ROUTE)]
        Task UpdateGuildTwitterSetting(GuildTwitterSettingsDTO settings);

        [Post(GUILD_TWITTER_SETTINGS_ROUTE)]
        Task<GuildTwitterSettingsDTO> CreateGuildTwitterSettings(GuildTwitterSettingsDTO settings);

        [Delete(TWITTER_USER_ROUTE + "/{id}")]
        Task DeleteGuildTwitterSettings(ulong id);

        #endregion

        #region GuildTwitterLinks

        public const string GUILD_TWITTER_LINKS_ROUTE = "guildtwitterlinks";

        [Post(GUILD_TWITTER_LINKS_ROUTE)]
        Task CreateGuildTwitterLink(GuildTwitterSettingsDTO guildTwitterSettings, TwitterUserDTO twitterUser);

        [Delete(GUILD_TWITTER_LINKS_ROUTE)]
        Task DeleteGuildTwitterLink(GuildTwitterSettingsDTO guildTwitterSettings, TwitterUserDTO twitterUser);

        #endregion
    }
}
