﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UVOCBot.Api;

namespace UVOCBot.Api.Migrations
{
    [DbContext(typeof(BotContext))]
    [Migration("20210228005425_MemberGroups")]
    partial class MemberGroups
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.3");

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

            modelBuilder.Entity("UVOCBot.Api.Model.GuildSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("BonkChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Prefix")
                        .HasColumnType("longtext");

                    b.HasKey("GuildId");

                    b.ToTable("GuildSettings");
                });

            modelBuilder.Entity("UVOCBot.Api.Model.GuildTwitterSettings", b =>
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

            modelBuilder.Entity("UVOCBot.Api.Model.MemberGroup", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("GroupName")
                        .HasColumnType("varchar(255)");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("UserIds")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("GroupName");

                    b.ToTable("MemberGroups");
                });

            modelBuilder.Entity("UVOCBot.Api.Model.PlanetsideSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<int?>("DefaultWorld")
                        .HasColumnType("int");

                    b.HasKey("GuildId");

                    b.ToTable("PlanetsideSettings");
                });

            modelBuilder.Entity("UVOCBot.Api.Model.TwitterUser", b =>
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
                    b.HasOne("UVOCBot.Api.Model.GuildTwitterSettings", null)
                        .WithMany()
                        .HasForeignKey("GuildsGuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("UVOCBot.Api.Model.TwitterUser", null)
                        .WithMany()
                        .HasForeignKey("TwitterUsersUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
