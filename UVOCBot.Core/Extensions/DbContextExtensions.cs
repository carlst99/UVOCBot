﻿using Microsoft.EntityFrameworkCore;
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
        CancellationToken ct = default
    ) where TEntity : class, IGuildObject, new()
    {
        TEntity? value = await context.FindAsync<TEntity>(new object[] { guildId }, ct).ConfigureAwait(false);

        if (value is null)
        {
            value = new TEntity { GuildId = guildId };
            context.Add(value);
        }

        return value;
    }
}
