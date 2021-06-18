using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Core.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("welcome-message")]
    [Description("Commands that allow the welcome message feature to be setup")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
    public class WelcomeMessageCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IDbApiService _dbApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IPermissionChecksService _permissionChecksService;

        public WelcomeMessageCommands(
            ICommandContext context,
            MessageResponseHelpers responder,
            IDbApiService dbAPI,
            IDiscordRestGuildAPI guildApi,
            IPermissionChecksService permissionChecksService)
        {
            _context = context;
            _responder = responder;
            _dbApi = dbAPI;
            _guildApi = guildApi;
            _permissionChecksService = permissionChecksService;
        }

        [Command("enabled")]
        [Description("Enables or disables the welcome message feature.")]
        public async Task<IResult> EnabledCommand([Description("True to enable the welcome message feature.")] bool isEnabled)
        {
            Result<GuildWelcomeMessageDto> welcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!welcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return welcomeMessage;
            }

            GuildWelcomeMessageDto returnValue = welcomeMessage.Entity with { IsEnabled = isEnabled };
            Result dbUpdateResult = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, returnValue, CancellationToken).ConfigureAwait(false);

            if (!dbUpdateResult.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return dbUpdateResult;
            }
            else
            {
                return await _responder.RespondWithSuccessAsync(
                    _context,
                    "The welcome message feature has been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."),
                    CancellationToken).ConfigureAwait(false);
            }
        }

        [Command("alternate-roles")]
        [Description("Provides the new member with the option to give themself an alternative set of roles.")]
        [RequireGuildPermission(DiscordPermission.ManageRoles)]
        public async Task<IResult> AlternateRolesCommand(
            [Description("The label of the alternate role button. Leave empty to disable the alternate role feature.")] string? alternateRoleButtonLabel,
            [Description("The roles to apply.")] string roles)
        {
            if (alternateRoleButtonLabel is null)
                alternateRoleButtonLabel = string.Empty;

            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            IEnumerable<ulong> roleIds = ParseRoles(roles);
            IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            GuildWelcomeMessageDto updatedWelcomeMessage = getWelcomeMessage.Entity with
            {
                AlternateRoleLabel = alternateRoleButtonLabel,
                AlternateRoles = roleIds.ToList()
            };

            Result updateWelcomeMessage = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, updatedWelcomeMessage, CancellationToken).ConfigureAwait(false);
            if (!updateWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }
            else
            {
                await _responder.RespondWithSuccessAsync(
                    _context,
                    "Success! The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                    CancellationToken).ConfigureAwait(false);
                return Result.FromSuccess();
            }
        }

        [Command("channel")]
        [Description("Sets the channel to post the welcome message in.")]
        public async Task<IResult> ChannelCommand(IChannel channel)
        {
            Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel(channel.ID, _context.User.ID, CancellationToken).ConfigureAwait(false);
            if (!getPermissionSet.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getPermissionSet;
            }

            if (!getPermissionSet.Entity.HasPermission(DiscordPermission.SendMessages))
                return await _responder.RespondWithErrorAsync(_context, "I do not have permission to send messages in this channel.", CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.Entity.HasPermission(DiscordPermission.ManageRoles))
                return await _responder.RespondWithErrorAsync(_context, "I do not have permission to manage roles in this channel.", CancellationToken).ConfigureAwait(false);

            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            GuildWelcomeMessageDto updatedWelcomeMessage = getWelcomeMessage.Entity with { ChannelId = channel.ID.Value };
            Result updateWelcomeMessage = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, updatedWelcomeMessage, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return updateWelcomeMessage;
            }
            else
            {
                await _responder.RespondWithSuccessAsync(
                    _context,
                    $"The welcome message will now be posted in { Formatter.ChannelMention(channel.ID.Value) }.",
                    CancellationToken).ConfigureAwait(false);
                return Result.FromSuccess();
            }
        }

        private static IEnumerable<ulong> ParseRoles(string roles)
        {
            foreach (string role in roles.Split("<@&", System.StringSplitOptions.RemoveEmptyEntries))
            {
                int index = role.IndexOf('>');

                if (index > 0 && ulong.TryParse(role[0..index], out ulong roleId))
                    yield return roleId;
            }
        }
    }
}
