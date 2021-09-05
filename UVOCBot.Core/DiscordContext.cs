using Microsoft.EntityFrameworkCore;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using UVOCBot.Core.Model;

namespace UVOCBot.Core
{
    public sealed class DiscordContext : DbContext
    {
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<GuildWelcomeMessage> GuildWelcomeMessages { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }
        public DbSet<PlanetsideSettings> PlanetsideSettings { get; set; }
        public DbSet<MemberGroup> MemberGroups { get; set; }
        public DbSet<GuildRoleMenu> RoleMenus { get; set; }
        public DbSet<GuildAdminSettings> GuildAdminSettings { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public DiscordContext(DbContextOptions<DiscordContext> options)
            : base(options)
        {
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuildWelcomeMessage>()
                        .Property(p => p.AlternateRoles)
                        .HasConversion(
                            v => IdListToBytes(v),
                            v => BytesToIdList(v));

            modelBuilder.Entity<GuildWelcomeMessage>()
                        .Property(p => p.DefaultRoles)
                        .HasConversion(
                            v => IdListToBytes(v),
                            v => BytesToIdList(v));

            modelBuilder.Entity<MemberGroup>()
                        .Property(p => p.UserIds)
                        .HasConversion(
                            v => IdListToBytes(v),
                            v => BytesToIdList(v));
        }

        private static byte[] IdListToBytes(IList<ulong> idList)
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
}
