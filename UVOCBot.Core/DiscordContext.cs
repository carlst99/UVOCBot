using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UVOCBot.Core.Model;

namespace UVOCBot.Core;

public sealed class DiscordContext : DbContext
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new();

    public DbSet<GuildAdminSettings> GuildAdminSettings { get; set; }
    public DbSet<GuildFeedsSettings> GuildFeedsSettings { get; set; }
    public DbSet<GuildWelcomeMessage> GuildWelcomeMessages { get; set; }
    public DbSet<PlanetsideSettings> PlanetsideSettings { get; set; }
    public DbSet<GuildRoleMenu> RoleMenus { get; set; }
    public DbSet<SpaceEngineersData> SpaceEngineersDatas { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public DiscordContext(DbContextOptions<DiscordContext> options)
        : base(options)
    {
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ValueComparer<List<ulong>> idListComparer = new
        (
            (l1, l2) => l1 != null && l2 != null && l1.SequenceEqual(l2),
            l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            l => l
        );

        ValueComparer<List<GuildGreetingAlternateRoleSet>> garsComparer = new
        (
            (l1, l2) => l1 != null && l2 != null && l1.SequenceEqual(l2),
            l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            l => l
        );

        modelBuilder.Entity<GuildWelcomeMessage>()
            .Property(p => p.AlternateRolesets)
            .HasConversion
            (
                v => JsonSerializer.Serialize(v, JSON_OPTIONS),
                v => v.Length == 0
                    ? new List<GuildGreetingAlternateRoleSet>()
                    : JsonSerializer.Deserialize<List<GuildGreetingAlternateRoleSet>>(v, JSON_OPTIONS)!,
                garsComparer
            );

        modelBuilder.Entity<GuildWelcomeMessage>()
                    .Property(p => p.DefaultRoles)
                    .HasConversion
                    (
                        v => IdListToBytes(v),
                        v => BytesToIdList(v),
                        idListComparer
                    );

        modelBuilder.Entity<PlanetsideSettings>()
                    .Property(p => p.TrackedOutfits)
                    .HasConversion
                    (
                        v => IdListToBytes(v),
                        v => BytesToIdList(v),
                        idListComparer
                    );
    }

    private static byte[] IdListToBytes(IReadOnlyList<ulong> idList)
    {
        byte[] buffer = new byte[idList.Count * sizeof(ulong)];

        for (int i = 0; i < idList.Count; i++)
        {
            Span<byte> s = new(buffer, i * sizeof(ulong), sizeof(ulong));
            BinaryPrimitives.WriteUInt64LittleEndian(s, idList[i]);
        }

        return buffer;
    }

    private static List<ulong> BytesToIdList(byte[] buffer)
    {
        List<ulong> idList = new();

        for (int i = 0; i < buffer.Length; i += sizeof(ulong))
        {
            Span<byte> s = new(buffer, i, sizeof(ulong));
            idList.Add(BinaryPrimitives.ReadUInt64LittleEndian(s));
        }

        return idList;
    }
}
