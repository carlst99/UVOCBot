using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public static class IApiServiceExtensions
    {
        public static async Task<GuildTwitterSettingsDTO> GetGuildTwitterSettingsAsync(this IApiService service, ulong id)
        {
            GuildTwitterSettingsDTO settings;
            try
            {
                settings = await service.GetGuildTwitterSetting(id).ConfigureAwait(false);
            }
            catch
            {
                settings = new GuildTwitterSettingsDTO(id);
                await service.CreateGuildTwitterSettings(settings).ConfigureAwait(false);
            }

            return settings;
        }

        public static async Task<TwitterUserDTO> GetDbTwitterUserAsync(this IApiService service, long id)
        {
            TwitterUserDTO user;
            try
            {
                user = await service.GetTwitterUser(id).ConfigureAwait(false);
            }
            catch
            {
                user = new TwitterUserDTO(id);
                await service.CreateTwitterUser(user).ConfigureAwait(false);
            }

            return user;
        }

        public static async Task<GuildSettingsDTO> GetGuildSettingsAsync(this IApiService service, ulong id)
        {
            GuildSettingsDTO settings;
            try
            {
                settings = await service.GetGuildSetting(id).ConfigureAwait(false);
            }
            catch
            {
                settings = new GuildSettingsDTO(id);
                await service.CreateGuildSettings(settings).ConfigureAwait(false);
            }

            return settings;
        }
    }
}
