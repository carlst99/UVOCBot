using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Model;

namespace UVOCBot.Services
{
    public class PrefixService : IPrefixService
    {
        private readonly IDbApiService _dbApi;
        private readonly GeneralOptions _generalOptions;
        private readonly Dictionary<ulong, string> _guildPrefixPairs;

        public bool IsSetup { get; protected set; }

        public PrefixService(IDbApiService dbApi, IOptions<GeneralOptions> generalOptions)
        {
            _dbApi = dbApi;
            _generalOptions = generalOptions.Value;
            _guildPrefixPairs = new Dictionary<ulong, string>();
        }

        public string? GetPrefix(ulong guildId)
        {
            if (!IsSetup)
                throw new InvalidOperationException("Please call SetupAsync() before using the " + nameof(PrefixService));

            if (_guildPrefixPairs.ContainsKey(guildId))
                return _guildPrefixPairs[guildId];
            else
                return _generalOptions.CommandPrefix;
        }

        public async Task RemovePrefixAsync(ulong guildId)
        {
            if (!IsSetup)
                throw new InvalidOperationException("Please call SetupAsync() before using the " + nameof(PrefixService));

            _guildPrefixPairs.Remove(guildId);
            await UpdateDbPrefix(guildId, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Preloads custom prefixes set by any guilds
        /// </summary>
        /// <returns></returns>
        public async Task SetupAsync()
        {
            List<GuildSettingsDTO> guildSettings = await _dbApi.GetAllGuildSettings(true).ConfigureAwait(false);
            foreach (GuildSettingsDTO dto in guildSettings)
                _guildPrefixPairs.Add(dto.GuildId, dto.Prefix);

            IsSetup = true;
        }

        public async Task UpdatePrefixAsync(ulong guildId, string newPrefix)
        {
            if (!IsSetup)
                throw new InvalidOperationException("Please call SetupAsync() before using the " + nameof(PrefixService));

            _guildPrefixPairs[guildId] = newPrefix;
            await UpdateDbPrefix(guildId, newPrefix).ConfigureAwait(false);
        }

        private async Task UpdateDbPrefix(ulong guildId, string? newPrefix)
        {
            GuildSettingsDTO settings = await _dbApi.GetGuildSettingsAsync(guildId).ConfigureAwait(false);
            settings.Prefix = newPrefix;
            await _dbApi.UpdateGuildSettings(guildId, settings).ConfigureAwait(false);
        }
    }
}
