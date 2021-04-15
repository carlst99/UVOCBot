using Microsoft.Extensions.Options;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Model;
using UVOCBot.Services.Abstractions;

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

        public async Task<Result> RemovePrefixAsync(ulong guildId, CancellationToken ct = default)
        {
            if (!IsSetup)
                throw new InvalidOperationException("Please call SetupAsync() before using the " + nameof(PrefixService));

            _guildPrefixPairs.Remove(guildId);
            return await UpdateDbPrefix(guildId, null, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Preloads custom prefixes set by any guilds
        /// </summary>
        /// <returns></returns>
        public async Task<Result> SetupAsync(CancellationToken ct = default)
        {
            Result<List<GuildSettingsDTO>> guildSettings = await _dbApi.ListGuildSettingsAsync(true, ct).ConfigureAwait(false);
            if (!guildSettings.IsSuccess)
                return Result.FromError(guildSettings);

            foreach (GuildSettingsDTO dto in guildSettings.Entity)
                _guildPrefixPairs.Add(dto.GuildId, dto.Prefix);

            IsSetup = true;
            return Result.FromSuccess();
        }

        public async Task<Result> UpdatePrefixAsync(ulong guildId, string newPrefix, CancellationToken ct = default)
        {
            if (!IsSetup)
                throw new InvalidOperationException("Please call SetupAsync() before using the " + nameof(PrefixService));

            _guildPrefixPairs[guildId] = newPrefix;
            return await UpdateDbPrefix(guildId, newPrefix, ct).ConfigureAwait(false);
        }

        private async Task<Result> UpdateDbPrefix(ulong guildId, string? newPrefix, CancellationToken ct = default)
        {
            Result<GuildSettingsDTO> settings = await _dbApi.GetGuildSettingsAsync(guildId, ct).ConfigureAwait(false);
            if (!settings.IsSuccess)
                return Result.FromError(settings);

            settings.Entity.Prefix = newPrefix;
            return await _dbApi.UpdateGuildSettingsAsync(guildId, settings.Entity, ct).ConfigureAwait(false);
        }
    }
}
