using System.Threading.Tasks;

namespace UVOCBot.Services
{
    public interface IPrefixService
    {
        string? GetPrefix(ulong guildId);
        Task RemovePrefixAsync(ulong guildId);
        Task SetupAsync();
        Task UpdatePrefixAsync(ulong guildId, string newPrefix);
    }
}
