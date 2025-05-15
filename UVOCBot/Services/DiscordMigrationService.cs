using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Plugins.Roles.Abstractions.Services;

namespace UVOCBot.Services;

public class DiscordMigrationService
{
    private readonly ILogger<DiscordMigrationService> _logger;
    private readonly DiscordContext _dbContext;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IRoleMenuService _roleMenuService;

    private readonly Dictionary<int, Func<CancellationToken, Task>> _migrationList;

    public DiscordMigrationService
    (
        ILogger<DiscordMigrationService> logger,
        DiscordContext dbContext,
        IDiscordRestChannelAPI channelApi,
        IRoleMenuService roleMenuService
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _channelApi = channelApi;
        _roleMenuService = roleMenuService;

        _migrationList = new Dictionary<int, Func<CancellationToken, Task>>
        {
            { 1, Migration_RoleMenus_Update },
            { 2, Migration_RoleMenus_Update },
            { 3, Migrate_RoleMenus_ClearComponentV1 },
            { 4, Migration_RoleMenus_Update },
        };
}

    public async Task RunMigrations(CancellationToken ct)
    {
        DiscordMigrationHistory? lastMigration = _dbContext.DiscordMigrationHistory.OrderByDescending(x => x.MigrationId)
            .FirstOrDefault();

        if (lastMigration is null)
        {
            // This is a new instance of the bot, so we can assume there's no need to run any migrations.
            // Let's store the ID of the most recent migration as it will be our starting point

            lastMigration = new DiscordMigrationHistory(_migrationList.Keys.Max());
            _dbContext.DiscordMigrationHistory.Add(lastMigration);
            await _dbContext.SaveChangesAsync(ct);
            return;
        }

        IEnumerable<int> migrationsToRun = _migrationList.Keys.Order()
            .Where(x => x > lastMigration.MigrationId);

        try
        {
            foreach (int migration in migrationsToRun)
            {
                _logger.LogInformation("Running discord migration {MigrationId}", migration);
                await _migrationList[migration](ct);

                DiscordMigrationHistory migrationRecord = new(migration);
                _dbContext.DiscordMigrationHistory.Add(migrationRecord);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run the migration");
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task Migration_RoleMenus_Update(CancellationToken ct)
    {
        foreach (GuildRoleMenu menu in _dbContext.RoleMenus.Include(x => x.Roles))
        {
            _logger.LogDebug("Migrating role menu {Id}", menu.Id);
            Result<IMessage> result = await _roleMenuService.UpdateRoleMenuMessageAsync(menu, false, ct);

            if (!result.IsSuccess)
                _logger.LogError("Failed to migrate a role menu: {Message}", result.Error!.Message);
        }
    }

    private async Task Migrate_RoleMenus_ClearComponentV1(CancellationToken ct)
    {
        foreach (GuildRoleMenu menu in _dbContext.RoleMenus)
        {
            Result<IMessage> message = await _channelApi.GetChannelMessageAsync
            (
                DiscordSnowflake.New(menu.ChannelId),
                DiscordSnowflake.New(menu.MessageId),
                ct
            );
            if (message.IsSuccess && message.Entity.Flags.Value.HasFlag(MessageFlags.IsComponentsV2))
                continue;

            _logger.LogDebug("Clearing component V1 components from role menu {Id}", menu.Id);
            await _channelApi.EditMessageAsync
            (
                DiscordSnowflake.New(menu.ChannelId),
                DiscordSnowflake.New(menu.MessageId),
                "",
                embeds: Array.Empty<IEmbed>(),
                ct: ct
            );
        }
    }
}
