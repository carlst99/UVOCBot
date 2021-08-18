using Microsoft.Extensions.Options;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Dto;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class PrefixService : IPrefixService
    {
        private readonly IDbApiService _dbApi;
        private readonly GeneralOptions _generalOptions;
        private readonly Dictionary<ulong, string> _guildPrefixPairs;

        public PrefixService(IDbApiService dbApi, IOptions<GeneralOptions> generalOptions)
        {
            _dbApi = dbApi;
            _generalOptions = generalOptions.Value;
            _guildPrefixPairs = new Dictionary<ulong, string>();
        }

        public string? GetPrefix(ulong guildId)
        {
            if (_guildPrefixPairs.ContainsKey(guildId))
                return _guildPrefixPairs[guildId];
            else
                return _generalOptions.CommandPrefix;
        }

        public async Task<Result> RemovePrefixAsync(ulong guildId, CancellationToken ct = default)
        {
            _guildPrefixPairs.Remove(guildId);
            return await UpdateDbPrefix(guildId, null, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Preloads custom prefixes set by any guilds.
        /// </summary>
        /// <returns>A result indicating if the operation was successful.</returns>
        public async Task<Result> InitialiseAsync(CancellationToken ct = default)
        {
            Result<List<GuildSettingsDto>> guildSettings = await _dbApi.ListGuildSettingsAsync(true, ct).ConfigureAwait(false);
            if (!guildSettings.IsSuccess)
                return Result.FromError(guildSettings);

            foreach (GuildSettingsDto dto in guildSettings.Entity)
            {
                if (dto.Prefix is not null && !_guildPrefixPairs.ContainsKey(dto.GuildId))
                    _guildPrefixPairs.Add(dto.GuildId, dto.Prefix);
            }

            return Result.FromSuccess();
        }

        public async Task<Result> UpdatePrefixAsync(ulong guildId, string newPrefix, CancellationToken ct = default)
        {
            _guildPrefixPairs[guildId] = newPrefix;
            return await UpdateDbPrefix(guildId, newPrefix, ct).ConfigureAwait(false);
        }

        private async Task<Result> UpdateDbPrefix(ulong guildId, string? newPrefix, CancellationToken ct = default)
        {
            Result<GuildSettingsDto> settings = await _dbApi.GetGuildSettingsAsync(guildId, ct).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return Result.FromError(settings);

            settings.Entity.Prefix = newPrefix;
            return await _dbApi.UpdateGuildSettingsAsync(guildId, settings.Entity, ct).ConfigureAwait(false);
        }
    }
}
