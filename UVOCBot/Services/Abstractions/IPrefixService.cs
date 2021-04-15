using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IPrefixService
    {
        string? GetPrefix(ulong guildId);
        Task<Result> RemovePrefixAsync(ulong guildId, CancellationToken ct = default);
        Task<Result> SetupAsync(CancellationToken ct = default);
        Task<Result> UpdatePrefixAsync(ulong guildId, string newPrefix, CancellationToken ct = default);
    }
}
