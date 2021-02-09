using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public class PrefixService : IPrefixService
    {
        private readonly IApiService _dbApi;
        private readonly GeneralOptions _generalOptions;
        private readonly Dictionary<ulong, string> _guildPrefixPairs;

        public bool IsSetup { get; protected set; }

        public PrefixService(IApiService dbApi, IOptions<GeneralOptions> generalOptions)
        {
            _dbApi = dbApi;
            _generalOptions = generalOptions.Value;
            _guildPrefixPairs = new Dictionary<ulong, string>();
        }

        public string GetPrefix(ulong guildId)
        {
            if (_guildPrefixPairs.ContainsKey(guildId))
                return _guildPrefixPairs[guildId];
            else
                return _generalOptions.CommandPrefix;
        }

        public async Task RemovePrefixAsync(ulong guildId)
        {
            _guildPrefixPairs.Remove(guildId);
            await UpdateDbPrefix(guildId, null).ConfigureAwait(false);
        }

        public async Task SetupAsync()
        {
            List<GuildSettingsDTO> guildSettings = await _dbApi.GetGuildSettings(true).ConfigureAwait(false);
            foreach (GuildSettingsDTO dto in guildSettings)
                _guildPrefixPairs.Add(dto.GuildId, dto.Prefix);

            IsSetup = true;
        }

        public async Task UpdatePrefixAsync(ulong guildId, string newPrefix)
        {
            _guildPrefixPairs[guildId] = newPrefix;
            await UpdateDbPrefix(guildId, newPrefix).ConfigureAwait(false);
        }

        private async Task UpdateDbPrefix(ulong guildId, string newPrefix)
        {
            GuildSettingsDTO settings = await _dbApi.GetGuildSetting(guildId).ConfigureAwait(false);
            settings.Prefix = newPrefix;
            await _dbApi.UpdateGuildSettings(guildId, settings).ConfigureAwait(false);
        }
    }
}
