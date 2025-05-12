using System;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

public class DiscordMigrationHistory
{
    [Key]
    public int MigrationId { get; init; }

    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    public DiscordMigrationHistory(int migrationId)
    {
        MigrationId = migrationId;
    }
}
