﻿using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Core;

public static class DbContextExtensions
{
    public static async ValueTask<TEntity> FindOrDefaultAsync<TEntity>(this DbContext context, ulong guildId, CancellationToken ct = default) where TEntity : class, IGuildObject, new()
    {
        TEntity? value = await context.FindAsync<TEntity>(new object[] { guildId }, ct).ConfigureAwait(false);

        if (value is null)
        {
            TEntity e = new()
            {
                GuildId = guildId
            };

            context.Add(e);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            return e;
        }
        else
        {
            return value;
        }
    }
}
