using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Core.Model;
using UVOCBot.Model.Census;
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
        private readonly ICensusApiService _censusApi;
        private readonly IDbApiService _dbApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _responder;

        public WelcomeMessageCommands(
            ICommandContext context,
            ICensusApiService censusApi,
            IDbApiService dbApi,
            IDiscordRestGuildAPI guildApi,
            IPermissionChecksService permissionChecksService,
            IReplyService responder)
        {
            _context = context;
            _censusApi = censusApi;
            _responder = responder;
            _dbApi = dbApi;
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
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return welcomeMessage;
            }

            GuildWelcomeMessageDto returnValue = welcomeMessage.Entity with { IsEnabled = isEnabled };
            Result dbUpdateResult = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, returnValue, CancellationToken).ConfigureAwait(false);

            if (!dbUpdateResult.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return dbUpdateResult;
            }

            return await _responder.RespondWithSuccessAsync(
                "The welcome message feature has been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."),
                CancellationToken).ConfigureAwait(false);
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
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
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
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            return await _responder.RespondWithSuccessAsync(
                "Success! The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                CancellationToken).ConfigureAwait(false);
        }

        [Command("channel")]
        [Description("Sets the channel to post the welcome message in.")]
        public async Task<IResult> ChannelCommand(IChannel channel)
        {
            Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel(channel.ID, _context.User.ID, CancellationToken).ConfigureAwait(false);
            if (!getPermissionSet.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getPermissionSet;
            }

            if (!getPermissionSet.Entity.HasPermission(DiscordPermission.SendMessages))
                return await _responder.RespondWithUserErrorAsync("I do not have permission to send messages in this channel.", CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.Entity.HasPermission(DiscordPermission.ManageRoles))
                return await _responder.RespondWithUserErrorAsync("I do not have permission to manage roles in this channel.", CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.Entity.HasPermission(DiscordPermission.ChangeNickname))
                return await _responder.RespondWithUserErrorAsync("I do not have permission to change nicknames in this channel.", CancellationToken).ConfigureAwait(false);

            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            GuildWelcomeMessageDto updatedWelcomeMessage = getWelcomeMessage.Entity with { ChannelId = channel.ID.Value };
            Result updateWelcomeMessage = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, updatedWelcomeMessage, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return updateWelcomeMessage;
            }

            return await _responder.RespondWithSuccessAsync(
                $"The welcome message will now be posted in { Formatter.ChannelMention(channel.ID.Value) }.",
                CancellationToken).ConfigureAwait(false);
        }

        [Command("default-roles")]
        [Description("Provides the new member default roles.")]
        [RequireGuildPermission(DiscordPermission.ManageRoles)]
        public async Task<IResult> DefaultRolesCommand(
            [Description("The roles to apply. Leave empty to apply no roles.")] string? roles)
        {
            if (roles is null)
                roles = string.Empty;

            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            IEnumerable<ulong> roleIds = ParseRoles(roles);
            IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            GuildWelcomeMessageDto updatedWelcomeMessage = getWelcomeMessage.Entity with
            {
                DefaultRoles = roleIds.ToList()
            };

            Result updateWelcomeMessage = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, updatedWelcomeMessage, CancellationToken).ConfigureAwait(false);
            if (!updateWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            return await _responder.RespondWithSuccessAsync(
                "Success! The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                CancellationToken).ConfigureAwait(false);
        }

        [Command("ingame-name-guess")]
        [Description("Attempts to guess the new member's in-game name, in order to make an offer to set their nickname.")]
        [RequireGuildPermission(DiscordPermission.ChangeNickname)]
        public async Task<IResult> IngameNameGuessCommand(
            [Description("Is the nickname guess feature enabled.")] bool isEnabled,
            [Description("The tag of the outfit to make nickname guesses from, based on its newest members.")] string outfitTag)
        {
            Result<Outfit?> getOutfit = await _censusApi.GetOutfit(outfitTag, CancellationToken).ConfigureAwait(false);
            if (!getOutfit.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getOutfit;
            }

            if (getOutfit.Entity is null)
                return await _responder.RespondWithUserErrorAsync("That outfit does not exist.", CancellationToken).ConfigureAwait(false);

            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            GuildWelcomeMessageDto updatedWelcomeMessage = getWelcomeMessage.Entity with { DoIngameNameGuess = isEnabled, OutfitId = getOutfit.Entity.OutfitId };
            Result updateWelcomeMessage = await _dbApi.UpdateGuildWelcomeMessageAsync(updatedWelcomeMessage.GuildId, updatedWelcomeMessage, CancellationToken).ConfigureAwait(false);
            if (!updateWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return updateWelcomeMessage;
            }

            return await _responder.RespondWithSuccessAsync(
                $"Nickname guesses from the outfit { Formatter.Bold(getOutfit.Entity.Name) } will now be presented on the welcome message.",
                CancellationToken).ConfigureAwait(false);
        }

        [Command("message")]
        [Description("Sets the message to present to new members.")]
        public async Task<IResult> MessageCommand([Description("The message. Use <name> as a placeholder for the member's name.")] string message)
        {
            Result<GuildWelcomeMessageDto> getWelcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!getWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return getWelcomeMessage;
            }

            GuildWelcomeMessageDto updatedWelcomeMessage = getWelcomeMessage.Entity with { Message = message };
            Result updateWelcomeMessage = await _dbApi.UpdateGuildWelcomeMessageAsync(updatedWelcomeMessage.GuildId, updatedWelcomeMessage, CancellationToken).ConfigureAwait(false);
            if (!updateWelcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync("Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return updateWelcomeMessage;
            }

            return await _responder.RespondWithSuccessAsync("Message successfully updated!", CancellationToken).ConfigureAwait(false);
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
