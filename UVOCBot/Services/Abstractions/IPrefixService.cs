using Remora.Results;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IPrefixService
    {
        string? GetPrefix(ulong guildId);
        Task<Result> RemovePrefixAsync(ulong guildId);
        Task<Result> SetupAsync();
        Task<Result> UpdatePrefixAsync(ulong guildId, string newPrefix);
    }
}
