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
        private readonly IDiscordRestGuildAPI _guildAPI;

        public WelcomeMessageCommands(ICommandContext context, MessageResponseHelpers responder, IDbApiService dbAPI, IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _responder = responder;
            _dbApi = dbAPI;
            _guildAPI = guildAPI;
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
            [Description("The label to put on the button that lets the new member acquire the alternate roles.")] string alternateRoleButtonLabel,
            [Description("The roles to apply.")] string roles)
        {
            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            IEnumerable<ulong> roleIds = ParseRoles(roles);
            IResult canManipulateRoles = await CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
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

        // TODO: When setting channel, ensure that we have the correct permission overwrites to adjust roles

        private static IEnumerable<ulong> ParseRoles(string roles)
        {
            foreach (string role in roles.Split("<@&", System.StringSplitOptions.RemoveEmptyEntries))
            {
                int index = role.IndexOf('>');

                if (index > 0 && ulong.TryParse(role[0..index], out ulong roleId))
                    yield return roleId;
            }
        }

        private async Task<IResult> CanManipulateRoles(Snowflake guildId, IEnumerable<ulong> roleIds)
        {
            Result<IReadOnlyList<IRole>> getGuildRoles = await _guildAPI.GetGuildRolesAsync(guildId, CancellationToken).ConfigureAwait(false);
            if (!getGuildRoles.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getGuildRoles;
            }

            Result<IGuildMember> getCurrentMember = await _guildAPI.GetGuildMemberAsync(_context.GuildID.Value, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!getCurrentMember.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getCurrentMember;
            }

            // Enumerate roles in order from highest to lowest ranked, so that logically the first role in the current member's role list is its highest
            IRole? highestRole = null;
            foreach (IRole role in getGuildRoles.Entity.OrderByDescending(r => r.Position))
            {
                if (getCurrentMember.Entity.Roles.Contains(role.ID))
                {
                    highestRole = role;
                    break;
                }
            }

            if (highestRole is null)
                return await _responder.RespondWithUserErrorAsync(_context, "I cannot assign these roles, as I do not have a role myself.", CancellationToken).ConfigureAwait(false);

            // Check that each role is assignable by us
            foreach (ulong roleId in roleIds)
            {
                if (!getGuildRoles.Entity.Any(r => r.ID.Value == roleId))
                    return await _responder.RespondWithUserErrorAsync(_context, "A supplied role does not exist.", CancellationToken).ConfigureAwait(false);

                IRole role = getGuildRoles.Entity.First(r => r.ID.Value == roleId);
                if (role.Position > highestRole.Position)
                {
                    return await _responder.RespondWithUserErrorAsync(
                        _context,
                        $"I cannot assign the { Formatter.RoleMention(role.ID) } role, as it is positioned above my own highest role.",
                        CancellationToken).ConfigureAwait(false);
                }
            }

            return Result.FromSuccess();
        }
    }
}
