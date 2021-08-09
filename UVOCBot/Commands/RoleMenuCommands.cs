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
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("rolemenu")]
    [Description("Commands to create and edit role menus.")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public class RoleMenuCommands : CommandGroup
    {
        private const string EMBED_PROVIDER = "rolemenu";

        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _replyService;

        public RoleMenuCommands(ICommandContext context, IDiscordRestChannelAPI channelApi, IPermissionChecksService permissionChecksService, IReplyService replyService)
        {
            _context = context;
            _channelApi = channelApi;
            _permissionChecksService = permissionChecksService;
            _replyService = replyService;
        }

        [Command("create")]
        [Description("Creates a new role menu.")]
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

            Embed e = new(
                title,
                Description: description ?? default(Optional<string>),
                Colour: BotConstants.DEFAULT_EMBED_COLOUR,
                Provider: new EmbedProvider(EMBED_PROVIDER));

            Result<IMessage> menuCreationResult = await _channelApi.CreateMessageAsync(channel.ID, embeds: new Embed[] { e }, ct: CancellationToken).ConfigureAwait(false);
            if (!menuCreationResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return menuCreationResult;
            }

            return await _replyService.RespondWithSuccessAsync("Menu created!", CancellationToken).ConfigureAwait(false);
        }

        [Command("edit")]
        [Description("Edits a role menu.")]
        public async Task<IResult> EditCommand(
            [Description("The channel that the role menu is in.")] IChannel channel,
            [Description("The ID of the role menu message.")] Snowflake messageId,
            [Description("The title of the role menu.")] string newTitle,
            [Description("The description of the role menu.")] string? newDescription = null)
        {
            IMessage? menu = await GetRoleMenu(channel, messageId).ConfigureAwait(false);
            if (menu is null)
                return Result.FromSuccess();

            Embed e = (Embed)menu.Embeds[0] with
            {
                Title = newTitle,
                Description = newDescription ?? default(Optional<string>)
            };

            Result<IMessage> menuModificationResult = await _channelApi.EditMessageAsync(channel.ID, messageId, embeds: new Embed[] { e }, ct: CancellationToken).ConfigureAwait(false);
            if (!menuModificationResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return menuModificationResult;
            }

            return await _channelApi.CreateMessageAsync
            (
                _context.ChannelID,
                embeds: new IEmbed[] { _replyService.GetSuccessEmbed("Menu updated!") },
                messageReference: new MessageReference(messageId, channel.ID, _context.GuildID.Value, false),
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        [Command("add-role")]
        [Description("Adds a role selection item to a role menu.")]
        public async Task<IResult> AddRole(
            [Description("The channel that the role menu is in.")] IChannel channel,
            [Description("The ID of the role menu message.")] Snowflake messageId,
            [Description("The role to assign")] IRole roleToAssign,
            string roleItemLabel,
            [Description("The description of the role selection item.")] string roleItemDescription,
            IPartialEmoji roleItemEmoji)
        {
            IMessage? menu = await GetRoleMenu(channel, messageId).ConfigureAwait(false);
            if (menu is null)
                return Result.FromSuccess();

            IResult canAssignRoleResult = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, new ulong[] { roleToAssign.ID.Value }, CancellationToken).ConfigureAwait(false);
            if (!canAssignRoleResult.IsSuccess)
            {
                return await _replyService.RespondWithUserErrorAsync
                (
                    "I can't assign that role. Make sure my highest role is higher than any roles you'd like me to assign.",
                    CancellationToken
                ).ConfigureAwait(false);
            }

            SelectMenuComponent selectMenuComponent = GetSelectMenuComponent(menu);
            if (selectMenuComponent.Options.Count == 25)
            {
                return await _replyService.RespondWithUserErrorAsync
                (
                    "A maximum of 25 role items can be added. Consider removing some role items, or creating a new role menu.",
                    CancellationToken
                ).ConfigureAwait(false);
            }

            ISelectOption roleItem = new SelectOption(roleItemLabel, roleToAssign.ID.Value.ToString(), roleItemDescription, new(roleItemEmoji), false);

            return Result.FromSuccess();
        }

        private async Task<IMessage?> GetRoleMenu(IChannel channel, Snowflake messageId)
        {
            Result<IMessage> menuMessageResult = await _channelApi.GetChannelMessageAsync(channel.ID, messageId, CancellationToken).ConfigureAwait(false);
            if (!menuMessageResult.IsSuccess || menuMessageResult.Entity is null)
            {
                await _replyService.RespondWithUserErrorAsync("That role menu doesn't exist.", CancellationToken).ConfigureAwait(false);
                return null;
            }

            IMessage menu = menuMessageResult.Entity;

            if (menu.Author.ID != BotConstants.UserId)
            {
                await _replyService.RespondWithUserErrorAsync("That message isn't a UVOCBot role menu.", CancellationToken).ConfigureAwait(false);
                return null;
            }

            // Use footer instead with info about role toggling
            if (menu.Embeds.Count == 0 || !menu.Embeds[0].Provider.HasValue || menu.Embeds[0].Provider.Value.Name != EMBED_PROVIDER)
            {
                await _replyService.RespondWithUserErrorAsync("That message doesn't contain a role menu.", CancellationToken).ConfigureAwait(false);
                return null;
            }

            return menu;
        }

        private SelectMenuComponent GetSelectMenuComponent(IMessage menu)
        {
            List<IMessageComponent> messageComponents;
            if (menu.Components.HasValue && menu.Components.Value is not null)
                messageComponents = new List<IMessageComponent>(menu.Components.Value);
            else
                messageComponents = new List<IMessageComponent>();

            if (messageComponents.Count == 0)
            {
                messageComponents.Add
                (
                    new ActionRowComponent
                    (
                        new List<IMessageComponent>() { new SelectMenuComponent("rolemenu", new List<ISelectOption>(), "Toggle roles", default, 25, default) }
                    )
                );
            }

            return (SelectMenuComponent)((ActionRowComponent)messageComponents[0]).Components[0];
        }
    }
}
