using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public static class IAPIServiceExtensions
    {
        public static async Task<GuildTwitterSettingsDTO> GetGuildTwitterSettingsAsync(this IAPIService service, ulong id)
        {
            GuildTwitterSettingsDTO settings;
            try
            {
                settings = await service.GetGuildTwitterSettings(id).ConfigureAwait(false);
            }
            catch
            {
                settings = new GuildTwitterSettingsDTO(id);
                await service.CreateGuildTwitterSettings(settings).ConfigureAwait(false);
            }

            return settings;
        }

        public static async Task<TwitterUserDTO> GetDbTwitterUserAsync(this IAPIService service, long id)
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

        public static async Task<GuildSettingsDTO> GetGuildSettingsAsync(this IAPIService service, ulong id)
        {
            GuildSettingsDTO settings;
            try
            {
                settings = await service.GetGuildSettings(id).ConfigureAwait(false);
            }
            catch
            {
                settings = new GuildSettingsDTO(id);
                await service.CreateGuildSettings(settings).ConfigureAwait(false);
            }

            return settings;
        }

        public static async Task<PlanetsideSettingsDTO> GetPlanetsideSettingsAsync(this IAPIService service, ulong id)
        {
            PlanetsideSettingsDTO settings;
            try
            {
                settings = await service.GetPlanetsideSettings(id).ConfigureAwait(false);
            }
            catch
            {
                settings = new PlanetsideSettingsDTO(id);
                await service.CreatePlanetsideSettings(settings).ConfigureAwait(false);
            }

            return settings;
        }
    }
}
