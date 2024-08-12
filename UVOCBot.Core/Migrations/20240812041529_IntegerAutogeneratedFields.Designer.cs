﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using UVOCBot.Core;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    [DbContext(typeof(DiscordContext))]
    [Migration("20240812041529_IntegerAutogeneratedFields")]
    partial class IntegerAutogeneratedFields
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("UVOCBot.Core.Model.GuildAdminSettings", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("IsLoggingEnabled")
                        .HasColumnType("boolean");

                    b.Property<decimal>("LogTypes")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("LoggingChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("GuildId");

                    b.ToTable("GuildAdminSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildFeedsSettings", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("FeedChannelID")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("Feeds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("boolean");

                    b.HasKey("GuildId");

                    b.ToTable("GuildFeedsSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildRoleMenu", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("AuthorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("GuildId", "MessageId");

                    b.ToTable("RoleMenus");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildRoleMenuRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Emoji")
                        .HasColumnType("text");

                    b.Property<int?>("GuildRoleMenuId")
                        .HasColumnType("integer");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildRoleMenuId");

                    b.HasIndex("RoleId");

                    b.ToTable("GuildRoleMenuRole");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.GuildWelcomeMessage", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("AlternateRolesets")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<byte[]>("DefaultRoles")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<bool>("DoIngameNameGuess")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("OutfitId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("GuildId");

                    b.ToTable("GuildWelcomeMessages");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.PlanetsideSettings", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("BaseCaptureChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int?>("DefaultWorld")
                        .HasColumnType("integer");

                    b.Property<byte[]>("TrackedOutfits")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.HasKey("GuildId");

                    b.ToTable("PlanetsideSettings");
                });

            modelBuilder.Entity("UVOCBot.Core.Model.SpaceEngineersData", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("ServerAddress")
                        .HasColumnType("text");

                    b.Property<string>("ServerKey")
                        .HasColumnType("text");

                    b.Property<int?>("ServerPort")
                        .HasColumnType("integer");

                    b.Property<decimal?>("StatusMessageChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("StatusMessageId")
                        .HasColumnType("numeric(20,0)");

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
