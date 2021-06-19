using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IPermissionChecksService
    {
        Task<IResult> CanManipulateRoles(Snowflake guildId, IEnumerable<ulong> roleIds, CancellationToken ct = default);
        Task<Result<IDiscordPermissionSet>> GetPermissionsInChannel(Snowflake channelId, Snowflake userId, CancellationToken ct = default);
    }
}
