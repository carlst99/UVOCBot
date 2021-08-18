using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("admin")]
    [Description("Administorial commands.")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
    public class AdminCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _replyService;

        public AdminCommands(ICommandContext context, DiscordContext dbContext, IPermissionChecksService permissionsCheckService, IReplyService replyService)
        {
            _context = context;
            _dbContext = dbContext;
            _permissionChecksService = permissionsCheckService;
            _replyService = replyService;
        }

        [Command("logging-channel")]
        [Description("Sets the channel to post admin logs to.")]
        public async Task<IResult> SetLoggingChannelCommandAsync(IChannel channel)
        {
            GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            Result<IDiscordPermissionSet> permissions = await _permissionChecksService.GetPermissionsInChannel(channel, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!permissions.IsSuccess)
                return await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);

            if (!permissions.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
                return await _replyService.RespondWithUserErrorAsync("I need permission to view that channel.", CancellationToken).ConfigureAwait(false);

            if (!permissions.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
                return await _replyService.RespondWithUserErrorAsync("I need permission to send messages in that channel.", CancellationToken).ConfigureAwait(false);

            settings.LoggingChannelId = channel.ID.Value;

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync("I will now post admin logs to " + Formatter.ChannelMention(channel.ID), CancellationToken).ConfigureAwait(false);
        }

        [Command("toggle-log")]
        [Description("Enables or disables a particular log.")]
        public async Task<IResult> ToggleLog(
            [Description("The type of log to toggle.")] AdminLogTypes logType,
            [Description("Enables or disabled this log.")] bool isEnabled)
        {
            GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            if (isEnabled)
                settings.LogTypes |= (ulong)logType;
            else
                settings.LogTypes &= ~(ulong)logType;

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync($"Logging for the {logType} event has been " + (isEnabled ? "enabled" : "disabled"), CancellationToken).ConfigureAwait(false);
        }
    }
}
