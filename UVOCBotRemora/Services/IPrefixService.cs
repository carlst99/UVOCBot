using System.Threading.Tasks;

namespace UVOCBotRemora.Services
{
    public interface IPrefixService
    {
        string? GetPrefix(ulong guildId);
        Task RemovePrefixAsync(ulong guildId);
        Task SetupAsync();
        Task UpdatePrefixAsync(ulong guildId, string newPrefix);
    }
}
