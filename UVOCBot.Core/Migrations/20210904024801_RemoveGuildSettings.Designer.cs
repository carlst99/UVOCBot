﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UVOCBot.Core;

namespace UVOCBot.Core.Migrations
{
    [DbContext(typeof(DiscordContext))]
    [Migration("20210904024801_RemoveGuildSettings")]
    partial class RemoveGuildSettings
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("GuildTwitterSettingsTwitterUser", b =>
                {
                    b.Property<ulong>("GuildsGuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<long>("TwitterUsersUserId")
                        .HasColumnType("bigint");

                    b.HasKey("GuildsGuildId", "TwitterUsersUserId");

                    b.HasIndex("TwitterUsersUserId");

                    b.ToTable("GuildTwitterSettingsTwitterUser");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildAdminSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("IsLoggingEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("LogTypes")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("LoggingChannelId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("GuildId");

                    b.ToTable("GuildAdminSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildRoleMenu", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("AuthorId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("GuildId", "MessageId");

                    b.ToTable("RoleMenus");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildRoleMenuRole", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<string>("Emoji")
                        .HasColumnType("longtext");

                    b.Property<ulong?>("GuildRoleMenuId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("RoleId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("GuildRoleMenuId");

                    b.ToTable("GuildRoleMenuRole");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildTwitterSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong?>("RelayChannelId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("GuildId");

                    b.ToTable("GuildTwitterSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildWelcomeMessage", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("AlternateRoleLabel")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<byte[]>("AlternateRoles")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<byte[]>("DefaultRoles")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<bool>("DoIngameNameGuess")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("OfferAlternateRoles")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("OutfitId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("GuildId");

                    b.ToTable("GuildWelcomeMessages");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.MemberGroup", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("CreatorId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("GroupName")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("UserIds")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("GroupName");

                    b.ToTable("MemberGroups");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.PlanetsideSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<int?>("DefaultWorld")
                        .HasColumnType("int");

                    b.HasKey("GuildId");

                    b.ToTable("PlanetsideSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.TwitterUser", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<long?>("LastRelayedTweetId")
                        .HasColumnType("bigint");

                    b.HasKey("UserId");

                    b.ToTable("TwitterUsers");
                });

            modelBuilder.Entity("GuildTwitterSettingsTwitterUser", b =>
                {
                    b.HasOne("UVOCBot.Core.Model.GuildTwitterSettings", null)
                        .WithMany()
                        .HasForeignKey("GuildsGuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("UVOCBot.Core.Model.TwitterUser", null)
                        .WithMany()
                        .HasForeignKey("TwitterUsersUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildRoleMenuRole", b =>
                {
                    b.HasOne("UVOCBot.Core.Model.GuildRoleMenu", null)
                        .WithMany("Roles")
                        .HasForeignKey("GuildRoleMenuId");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildRoleMenu", b =>
                {
                    b.Navigation("Roles");
                });
#pragma warning restore 612, 618
        }
    }
}
