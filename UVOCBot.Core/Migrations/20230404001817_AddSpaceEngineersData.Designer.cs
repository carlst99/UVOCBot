﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UVOCBot.Core;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    [DbContext(typeof(DiscordContext))]
    [Migration("20230404001817_AddSpaceEngineersData")]
    partial class AddSpaceEngineersData
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

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

            modelBuilder.Entity("UVOCBot.Core.Model.GuildFeedsSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("FeedChannelID")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Feeds")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("GuildId");

                    b.ToTable("GuildFeedsSettings");
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

                    b.HasIndex("RoleId");

                    b.ToTable("GuildRoleMenuRole");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildWelcomeMessage", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("AlternateRolesets")
                        .IsRequired()
                        .HasColumnType("longtext");

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

                    b.Property<ulong>("OutfitId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("GuildId");

                    b.ToTable("GuildWelcomeMessages");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.PlanetsideSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("BaseCaptureChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<int?>("DefaultWorld")
                        .HasColumnType("int");

                    b.Property<byte[]>("TrackedOutfits")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.HasKey("GuildId");

                    b.ToTable("PlanetsideSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.SpaceEngineersData", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("ServerAddress")
                        .HasColumnType("longtext");

                    b.Property<string>("ServerKey")
                        .HasColumnType("longtext");

                    b.Property<int?>("ServerPort")
                        .HasColumnType("int");

                    b.Property<ulong?>("StatusMessageId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("GuildId");

                    b.ToTable("SpaceEngineersDatas");
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
