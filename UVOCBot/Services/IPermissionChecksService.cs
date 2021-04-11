using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBotRemora.Services
{
    public interface IPermissionChecksService
    {
        Task<Result<IDiscordPermissionSet>> PermissionsInChannel(Snowflake channelID, Snowflake userID, CancellationToken ct);
    }
}
