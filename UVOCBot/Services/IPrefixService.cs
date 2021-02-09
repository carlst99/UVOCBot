using System.Threading.Tasks;

namespace UVOCBot.Services
{
    public interface IPrefixService
    {
        public const string DEFAULT_PREFIX = "ub!";

        string GetPrefix(ulong guildId);
        Task RemovePrefixAsync(ulong guildId);
        Task SetupAsync();
        Task UpdatePrefixAsync(ulong guildId, string newPrefix);
    }
}
