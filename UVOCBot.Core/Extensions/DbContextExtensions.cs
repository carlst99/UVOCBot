using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Core.Extensions;

public static class DbContextExtensions
{
    public static async ValueTask<TEntity> FindOrDefaultAsync<TEntity>
    (
        this DbContext context,
        ulong guildId,
        bool addIfNotPresent = true,
        CancellationToken ct = default
    ) where TEntity : class, IGuildObject, new()
    {
        TEntity? value = await context.FindAsync<TEntity>(new object[] { guildId }, ct).ConfigureAwait(false);
        if (value is not null)
            return value;

        value = new TEntity { GuildId = guildId };
        if (addIfNotPresent)
            context.Add(value);

        return value;
    }
}
