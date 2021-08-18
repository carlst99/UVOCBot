using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Commands.Utilities;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("rolemenu")]
    [Description("Commands to create and edit role menus.")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public class RoleMenuCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _replyService;

        public RoleMenuCommands(
            ICommandContext context,
            DiscordContext dbContext,
            IDiscordRestChannelAPI channelApi,
            IPermissionChecksService permissionChecksService,
            IReplyService replyService)
        {
            _context = context;
            _dbContext = dbContext;
            _channelApi = channelApi;
            _permissionChecksService = permissionChecksService;
            _replyService = replyService;
        }

        [Command("create")]
        [Description("Creates a new role menu.")]
        [Ephemeral]
        public async Task<IResult> CreateCommand(
            [Description("The channel to post the role menu in.")] IChannel channel,
            [Description("The title of the role menu.")] string title,
            [Description("The description of the role menu.")] string? description = null)
        {
            Result<IDiscordPermissionSet> permissionsResult = await _permissionChecksService.GetPermissionsInChannel(channel.ID, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!permissionsResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return permissionsResult;
            }

            if (!permissionsResult.Entity.HasPermission(DiscordPermission.ManageRoles))
                return await _replyService.RespondWithUserErrorAsync("I don't have permission to manipulate roles in that channel!", CancellationToken).ConfigureAwait(false);

            if (!permissionsResult.Entity.HasPermission(DiscordPermission.SendMessages))
                return await _replyService.RespondWithUserErrorAsync("I don't have permission to send messages in that channel!", CancellationToken).ConfigureAwait(false);

            GuildRoleMenu menu = new(_context.GuildID.Value.Value, 0, channel.ID.Value, _context.User.ID.Value, title)
            {
                Description = description ?? string.Empty
            };

            IEmbed e = CreateRoleMenuEmbed(menu);

            Result<IMessage> menuCreationResult = await _channelApi.CreateMessageAsync(channel.ID, embeds: new IEmbed[] { e }, ct: CancellationToken).ConfigureAwait(false);
            if (!menuCreationResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return menuCreationResult;
            }

            menu.MessageId = menuCreationResult.Entity.ID.Value;

            _dbContext.Add(menu);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync("Menu created! ", CancellationToken).ConfigureAwait(false);
        }

        [Command("edit")]
        [Description("Edits a role menu.")]
        [Ephemeral]
        public async Task<IResult> EditCommand(
            [Description("The ID of the role menu message.")] Snowflake messageId,
            [Description("The title of the role menu.")] string newTitle,
            [Description("The description of the role menu.")] string? newDescription = null)
        {
            GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
            if (menu is null)
                return await _replyService.RespondWithUserErrorAsync("That role menu doesn't exist.", CancellationToken).ConfigureAwait(false);

            menu.Title = newTitle;
            menu.Description = newDescription ?? string.Empty;

            IResult modifyMenuResult = await ModifyRoleMenu(menu).ConfigureAwait(false);
            if (!modifyMenuResult.IsSuccess)
                return modifyMenuResult;

            return await _replyService.RespondWithSuccessAsync("Menu updated!", CancellationToken).ConfigureAwait(false);
        }

        [Command("delete")]
        [Description("Deletes a role menu.")]
        [Ephemeral]
        public async Task<IResult> DeleteCommand(
            [Description("The ID of the role menu message.")] Snowflake messageId)
        {
            GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
            if (menu is null)
                return await _replyService.RespondWithUserErrorAsync("That role menu doesn't exist.", CancellationToken).ConfigureAwait(false);

            Result deleteMenuResult = await _channelApi.DeleteMessageAsync(
                new Snowflake(menu.ChannelId),
                new Snowflake(menu.MessageId),
                "Role menu deletion requested by " + _context.User.Username,
                CancellationToken).ConfigureAwait(false);

            if (!deleteMenuResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return deleteMenuResult;
            }

            return await _replyService.RespondWithSuccessAsync("Menu deleted!", CancellationToken).ConfigureAwait(false);
        }

        [Command("add-role")]
        [Description("Adds a role selection item to a role menu.")]
        [Ephemeral]
        public async Task<IResult> AddRole(
            [Description("The ID of the role menu message.")] Snowflake messageId,
            [Description("The role to add.")] IRole roleToAdd,
            [Description("The description of the role selection item.")] string? roleItemDescription = null,
            [Description("The label of the role selection item. Leave empty to use the name of the role as the label.")] string? roleItemLabel = null)
        {
            GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
            if (menu is null)
                return await _replyService.RespondWithUserErrorAsync("That role menu doesn't exist.", CancellationToken).ConfigureAwait(false);

            int index = menu.Roles.FindIndex(r => r.RoleId == roleToAdd.ID.Value);
            if (index != -1)
                return await _replyService.RespondWithUserErrorAsync("This role already exists on this menu!", CancellationToken).ConfigureAwait(false);

            if (menu.Roles.Count == 25)
            {
                return await _replyService.RespondWithUserErrorAsync(
                    "A role menu can only hold a maximum of 25 roles at a time. Either remove some roles from this menu, or create a new one.",
                    CancellationToken).ConfigureAwait(false);
            }

            GuildRoleMenuRole role = new(roleToAdd.ID.Value, roleItemLabel ?? roleToAdd.Name)
            {
                Description = roleItemDescription,
                Emoji = null
            };
            menu.Roles.Add(role);

            _dbContext.Update(menu);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            IResult modifyMenuResult = await ModifyRoleMenu(menu).ConfigureAwait(false);
            if (!modifyMenuResult.IsSuccess)
                return modifyMenuResult;

            return await _replyService.RespondWithSuccessAsync("That role has been added.", CancellationToken).ConfigureAwait(false);
        }

        [Command("remove-role")]
        [Description("Removes a role selection item from a role menu.")]
        [Ephemeral]
        public async Task<IResult> RemoveRole(
            [Description("The ID of the role menue message.")] Snowflake messageId,
            [Description("The role to remove.")] IRole roleToRemove)
        {
            GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
            if (menu is null)
                return await _replyService.RespondWithUserErrorAsync("That role menu doesn't exist.", CancellationToken).ConfigureAwait(false);

            if (menu.Roles.Count <= 1)
                return await _replyService.RespondWithUserErrorAsync("You cannot remove the last role. Either add some more roles first, or delete the menu.", CancellationToken).ConfigureAwait(false);

            int index = menu.Roles.FindIndex(r => r.RoleId == roleToRemove.ID.Value);
            if (index == -1)
                return await _replyService.RespondWithUserErrorAsync("That role does not exist on the given menu.", CancellationToken).ConfigureAwait(false);

            menu.Roles.RemoveAt(index);

            _dbContext.Update(menu);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            IResult modifyMenuResult = await ModifyRoleMenu(menu).ConfigureAwait(false);
            if (!modifyMenuResult.IsSuccess)
                return modifyMenuResult;

            return await _replyService.RespondWithSuccessAsync("That role has been removed.", CancellationToken).ConfigureAwait(false);
        }

        private GuildRoleMenu? GetGuildRoleMenu(ulong messageId)
            => _dbContext.RoleMenus.Include(r => r.Roles).FirstOrDefault(r => r.GuildId == _context.GuildID.Value.Value && r.MessageId == messageId);

        private async Task<IResult> ModifyRoleMenu(GuildRoleMenu menu)
        {
            Result<IMessage> menuModificationResult = await _channelApi.EditMessageAsync(
                new Snowflake(menu.ChannelId),
                new Snowflake(menu.MessageId),
                embeds: new IEmbed[] { CreateRoleMenuEmbed(menu) },
                components: menu.Roles.Count > 0 ? CreateRoleMenuMessageComponents(menu) : new Optional<IReadOnlyList<IMessageComponent>>(),
                ct: CancellationToken).ConfigureAwait(false);

            if (!menuModificationResult.IsSuccess)
            {
                IResult res = await CheckRoleMenuExists(menu).ConfigureAwait(false);
                if (!res.IsSuccess)
                    return res;

                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
            }

            return menuModificationResult;
        }

        private static IEmbed CreateRoleMenuEmbed(GuildRoleMenu menu)
        {
            return new Embed(
                menu.Title,
                Description: menu.Description,
                Colour: BotConstants.DEFAULT_EMBED_COLOUR,
                Footer: new EmbedFooter("If you can't deselect a role, refresh your client by pressing Ctrl+R."));
        }

        private static List<IMessageComponent> CreateRoleMenuMessageComponents(GuildRoleMenu menu)
        {
            List<SelectOption> selectOptions = menu.Roles.ConvertAll(r => new SelectOption(
                r.Label,
                r.RoleId.ToString(),
                r.Description ?? "",
                default,
                false));
            SelectMenuComponent selectMenu = new(
                ComponentIdFormatter.GetId(ComponentAction.RoleMenuToggleRole, menu.MessageId.ToString()),
                selectOptions,
                "Toggle roles...",
                1,
                menu.Roles.Count,
                false);

            ActionRowComponent actionRow = new(new List<IMessageComponent> { selectMenu });
            return new List<IMessageComponent> { actionRow };
        }

        private async Task<IResult> CheckRoleMenuExists(GuildRoleMenu menu)
        {
            Result<IMessage> getMessageResult = await _channelApi.GetChannelMessageAsync(new Snowflake(menu.ChannelId), new Snowflake(menu.MessageId), CancellationToken).ConfigureAwait(false);

            if (!getMessageResult.IsSuccess)
            {
                await _replyService.RespondWithUserErrorAsync("That role menu appears to have been deleted! Please create a new one.", CancellationToken).ConfigureAwait(false);

                _dbContext.Remove(menu);
                await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
            }

            return getMessageResult;
        }
    }
}
