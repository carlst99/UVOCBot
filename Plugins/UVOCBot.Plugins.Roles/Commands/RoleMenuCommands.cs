using OneOf;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Roles.Abstractions.Services;

namespace UVOCBot.Plugins.Roles.Commands;

[Group("rolemenu")]
[Description("Commands to create and edit role menus.")]
[RequireContext(ChannelContext.Guild)]
[RequireGuildPermission(DiscordPermission.ManageRoles)]
[Ephemeral]
public class RoleMenuCommands : CommandGroup
{
    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly IRoleMenuService _roleMenuService;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public RoleMenuCommands
    (
        ICommandContext context,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestInteractionAPI interactionApi,
        IPermissionChecksService permissionChecksService,
        IRoleMenuService roleMenuService,
        DiscordContext dbContext,
        FeedbackService replyService
    )
    {
        _context = context;
        _channelApi = channelApi;
        _interactionApi = interactionApi;
        _permissionChecksService = permissionChecksService;
        _roleMenuService = roleMenuService;
        _dbContext = dbContext;
        _feedbackService = replyService;
    }

    [Command("create")]
    [Description("Creates a new role menu.")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> CreateCommand
    (
        [Description("The channel to post the role menu in.")][ChannelTypes(ChannelType.GuildText)] IChannel channel
    )
    {
        if (_context is not InteractionContext ictx)
        {
            await _feedbackService.SendContextualErrorAsync("This command must be executed as a slash command.", ct: CancellationToken);
            return Result.FromSuccess();
        }

        async Task CreateNormalResponse()
        {
            await _interactionApi.CreateInteractionResponseAsync
            (
                ictx.ID,
                ictx.Token,
                new InteractionResponse
                (
                    InteractionCallbackType.DeferredChannelMessageWithSource,
                    new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>
                    (
                        new InteractionMessageCallbackData(Flags: MessageFlags.Ephemeral)
                    )
                ),
                ct: CancellationToken
            );
        }

        Result<IDiscordPermissionSet> permissionsResult = await _permissionChecksService.GetPermissionsInChannel
        (
            channel.ID,
            DiscordConstants.UserId,
            CancellationToken
        ).ConfigureAwait(false);

        if (!permissionsResult.IsSuccess)
        {
            await CreateNormalResponse();
            return Result.FromError(permissionsResult);
        }

        if (!permissionsResult.Entity.HasAdminOrPermission(DiscordPermission.ManageRoles))
        {
            await CreateNormalResponse();
            return new PermissionError(DiscordPermission.ManageRoles, DiscordConstants.UserId, channel.ID);
        }

        if (!permissionsResult.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
        {
            await CreateNormalResponse();
            return new PermissionError(DiscordPermission.SendMessages, DiscordConstants.UserId, channel.ID);
        }

        GuildRoleMenu menu = new
        (
            _context.GuildID.Value.Value,
            0,
            channel.ID.Value,
            _context.User.ID.Value,
            "Placeholder"
        );

        IEmbed e = _roleMenuService.CreateRoleMenuEmbed(menu);

        Result<IMessage> menuCreationResult = await _channelApi.CreateMessageAsync
        (
            channel.ID,
            embeds: new[] { e },
            ct: CancellationToken
        ).ConfigureAwait(false);

        if (!menuCreationResult.IsSuccess)
        {
            await CreateNormalResponse();
            return Result.FromError(menuCreationResult);
        }

        Snowflake messageID = menuCreationResult.Entity.ID;
        menu.MessageId = messageID.Value;

        _dbContext.Add(menu);
        int addedCount = await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        if (addedCount < 1)
        {
            await CreateNormalResponse();
            return new GenericCommandError();
        }

        return await EditRoleMenuCommandAsync(messageID);
    }

    [Command("edit")]
    [Description("Edits a role menu.")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> EditRoleMenuCommandAsync
    (
        [Description("The ID of the role menu message.")] Snowflake messageID
    )
    {
        if (_context is not InteractionContext ictx)
            return new GenericCommandError("This command must be executed as a slash command.");

        if (!_roleMenuService.TryGetGuildRoleMenu(messageID.Value, out GuildRoleMenu? menu))
        {
            IResult sendResult = await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken);
            return sendResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendResult.Error!);
        }

        InteractionModalCallbackData modal = new
        (
            ComponentIDFormatter.GetId(RoleComponentKeys.ModalEditMenu, messageID.Value.ToString()),
            "Editing Menu",
            new[] {
                new ActionRowComponent
                (
                    new[] {
                        new TextInputComponent
                        (
                            RoleComponentKeys.TextInputEditMenuTitle,
                            TextInputStyle.Short,
                            "Title",
                            default,
                            256, // Max embed title size
                            true,
                            menu.Title,
                            default
                        )
                    }
                ),
                new ActionRowComponent
                (
                    new[] {
                        new TextInputComponent
                        (
                            RoleComponentKeys.TextInputEditMenuDescription,
                            TextInputStyle.Paragraph,
                            "Description",
                            default,
                            default,
                            false,
                            menu.Description,
                            default
                        )
                    }
                )
            }
        );

        return await _interactionApi.CreateInteractionResponseAsync
        (
            ictx.ID,
            ictx.Token,
            new InteractionResponse
            (
                InteractionCallbackType.Modal,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(modal)
            ),
            ct: CancellationToken
        );
    }

    [Command("delete")]
    [Description("Deletes a role menu.")]
    public async Task<IResult> DeleteCommand
    (
        [Description("The ID of the role menu message.")] Snowflake messageID
    )
    {
        if (!_roleMenuService.TryGetGuildRoleMenu(messageID.Value, out GuildRoleMenu? _))
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken);

        ButtonComponent confirmationComponent = new
        (
            ButtonComponentStyle.Danger,
            "Proceed with deletion",
            CustomID: ComponentIDFormatter.GetId(RoleComponentKeys.ConfirmDeletion, messageID.Value.ToString())
        );

        return await _feedbackService.SendContextualWarningAsync
        (
            "Are you sure that you want to delete this role menu?",
            options: new FeedbackMessageOptions
            (
                MessageComponents: new[] { new ActionRowComponent(new[] { confirmationComponent }) }
            ),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("add-role")]
    [Description("Adds a role to a menu. If the role already exists on the message it will be updated.")]
    public async Task<IResult> AddRole
    (
        [Description("The ID of the role menu message.")] Snowflake messageID,
        [Description("The role to add.")] IRole roleToAdd,
        [Description("The label of the role selection item. Leave empty to use the name of the role as the label.")] string? roleItemLabel = null
    )
    {
        if (!_roleMenuService.TryGetGuildRoleMenu(messageID.Value, out GuildRoleMenu? menu))
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken);

        if (menu.Roles.Count == 25)
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                "A role menu can hold a maximum of 25 roles at a time. Either remove some roles from this menu, or create a new one.",
                ct: CancellationToken
            );
        }

        GuildRoleMenuRole? dbRole = menu.Roles.Find(r => r.RoleId == roleToAdd.ID.Value);

        if (dbRole is null)
        {
            dbRole = new GuildRoleMenuRole(roleToAdd.ID.Value, roleItemLabel ?? roleToAdd.Name);

            menu.Roles.Add(dbRole);
            menu.Roles.Sort
            (
                (r1, r2) => string.Compare(r1.Label, r2.Label, StringComparison.Ordinal)
            );

            _dbContext.Update(menu);
        }
        else
        {
            dbRole.Label = roleItemLabel ?? roleToAdd.Name;

            _dbContext.Update(dbRole);
        }

        int updateCount = await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        if (updateCount < 1)
            return Result.FromError(new GenericCommandError());

        IResult modifyMenuResult = await _roleMenuService.UpdateRoleMenuMessageAsync(menu, CancellationToken).ConfigureAwait(false);
        if (!modifyMenuResult.IsSuccess)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                $"The menu was updated internally, but I couldn't update the corresponding message. Please use the {Formatter.InlineQuote("rolemenu update")} command.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"The {Formatter.RoleMention(roleToAdd)} role has been added.",
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions()),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("remove-role")]
    [Description("Removes a role from a menu.")]
    public async Task<IResult> RemoveRole
    (
        [Description("The ID of the role menu message.")] Snowflake messageID,
        [Description("The role to remove.")] IRole roleToRemove
    )
    {
        if (!_roleMenuService.TryGetGuildRoleMenu(messageID.Value, out GuildRoleMenu? menu))
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken);

        if (menu.Roles.Count <= 1)
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                "You cannot remove the last role. Either add some more roles first, or delete the menu.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        GuildRoleMenuRole? dbRole = menu.Roles.Find(r => r.RoleId == roleToRemove.ID.Value);
        if (dbRole is null)
            return await _feedbackService.SendContextualErrorAsync("That role does not exist on the given menu.", ct: CancellationToken).ConfigureAwait(false);

        menu.Roles.Remove(dbRole);
        _dbContext.Update(menu);
        _dbContext.Remove(dbRole);
        int updateCount = await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        if (updateCount < 1)
            return Result.FromError(new GenericCommandError());

        IResult modifyMenuResult = await _roleMenuService.UpdateRoleMenuMessageAsync(menu);
        if (!modifyMenuResult.IsSuccess)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                $"The menu was updated internally, but I couldn't update the corresponding message. Please use the {Formatter.InlineQuote("rolemenu update")} command.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"The {Formatter.RoleMention(roleToRemove)} role has been removed.",
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions()),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("update")]
    [Description("Forces an update on a role menu message. This should seldom be needed.")]
    public async Task<IResult> UpdateMenuCommandAsync
    (
        [Description("The ID of the role menu message.")] Snowflake messageID
    )
    {
        if (!_roleMenuService.TryGetGuildRoleMenu(messageID.Value, out GuildRoleMenu? menu))
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken);

        IResult modifyMenuResult = await _roleMenuService.UpdateRoleMenuMessageAsync(menu);
        if (!modifyMenuResult.IsSuccess)
            return modifyMenuResult;

        return await _feedbackService.SendContextualSuccessAsync(Formatter.Emoji("white_check_mark"), ct: CancellationToken);
    }
}
