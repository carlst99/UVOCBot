using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Plugins.Roles.Commands;

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
    private readonly FeedbackService _feedbackService;

    public RoleMenuCommands
    (
        ICommandContext context,
        DiscordContext dbContext,
        IDiscordRestChannelAPI channelApi,
        IPermissionChecksService permissionChecksService,
        FeedbackService replyService
    )
    {
        _context = context;
        _dbContext = dbContext;
        _channelApi = channelApi;
        _permissionChecksService = permissionChecksService;
        _feedbackService = replyService;
    }

    [Command("create")]
    [Description("Creates a new role menu.")]
    [Ephemeral]
    public async Task<Result> CreateCommand
    (
        [Description("The channel to post the role menu in.")][ChannelTypes(ChannelType.GuildText)] IChannel channel,
        [Description("The title of the role menu.")] string title,
        [Description("The description of the role menu.")] string? description = null
    )
    {
        Result<IDiscordPermissionSet> permissionsResult = await _permissionChecksService.GetPermissionsInChannel
        (
            channel.ID,
            DiscordConstants.UserId,
            CancellationToken
        ).ConfigureAwait(false);

        if (!permissionsResult.IsSuccess)
            return Result.FromError(permissionsResult);

        if (!permissionsResult.Entity.HasAdminOrPermission(DiscordPermission.ManageRoles))
            return new PermissionError(DiscordPermission.ManageRoles, DiscordConstants.UserId, channel.ID);

        if (!permissionsResult.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
            return new PermissionError(DiscordPermission.SendMessages, DiscordConstants.UserId, channel.ID);

        GuildRoleMenu menu = new(_context.GuildID.Value.Value, 0, channel.ID.Value, _context.User.ID.Value, title)
        {
            Description = description ?? string.Empty
        };

        IEmbed e = CreateRoleMenuEmbed(menu);

        Result<IMessage> menuCreationResult = await _channelApi.CreateMessageAsync
        (
            channel.ID,
            embeds: new IEmbed[] { e },
            ct: CancellationToken
        ).ConfigureAwait(false);

        if (!menuCreationResult.IsSuccess)
            return Result.FromError(menuCreationResult);

        menu.MessageId = menuCreationResult.Entity.ID.Value;

        _dbContext.Add(menu);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        IResult sendResult = await _feedbackService.SendContextualSuccessAsync("Menu created! ", ct: CancellationToken).ConfigureAwait(false);
        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult.Error!);
    }

    [Command("edit")]
    [Description("Edits a role menu.")]
    [Ephemeral]
    public async Task<IResult> EditCommand
    (
        [Description("The ID of the role menu message.")] Snowflake messageId,
        [Description("The title of the role menu.")] string newTitle,
        [Description("The description of the role menu.")] string? newDescription = null
    )
    {
        GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
        if (menu is null)
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken).ConfigureAwait(false);

        menu.Title = newTitle;
        menu.Description = newDescription ?? string.Empty;

        IResult modifyMenuResult = await ModifyRoleMenu(menu).ConfigureAwait(false);
        if (!modifyMenuResult.IsSuccess)
            return modifyMenuResult;

        return await _feedbackService.SendContextualSuccessAsync("Menu updated!", ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("delete")]
    [Description("Deletes a role menu.")]
    [Ephemeral]
    public async Task<IResult> DeleteCommand
    (
        [Description("The ID of the role menu message.")] Snowflake messageId
    )
    {
        GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
        if (menu is null)
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken).ConfigureAwait(false);

        _dbContext.Remove(menu);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        Result deleteMenuResult = await _channelApi.DeleteMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            "Role menu deletion requested by " + _context.User.Username,
            CancellationToken
        ).ConfigureAwait(false);

        if (!deleteMenuResult.IsSuccess)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                "The role menu has successfully been deleted but I couldn't remove the corresponding message. Please delete that now, as well.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        return await _feedbackService.SendContextualSuccessAsync
        (
            "The role menu has been successfully removed.",
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("add-role")]
    [Description("Adds a role selection item to a role menu.")]
    [Ephemeral]
    public async Task<IResult> AddRole
    (
        [Description("The ID of the role menu message.")] Snowflake messageId,
        [Description("The role to add.")] IRole roleToAdd,
        [Description("The description of the role selection item.")] string? roleItemDescription = null,
        [Description("The label of the role selection item. Leave empty to use the name of the role as the label.")] string? roleItemLabel = null
    )
    {
        GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
        if (menu is null)
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken).ConfigureAwait(false);

        if (menu.Roles.Count == 25)
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                "A role menu can hold a maximum of 25 roles at a time. Either remove some roles from this menu, or create a new one.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        GuildRoleMenuRole? dbRole = menu.Roles.Find(r => r.RoleId == roleToAdd.ID.Value);

        if (dbRole is null)
        {
            dbRole = new GuildRoleMenuRole(roleToAdd.ID.Value, roleItemLabel ?? roleToAdd.Name)
            {
                Description = roleItemDescription,
                Emoji = null
            };

            menu.Roles.Add(dbRole);
            _dbContext.Update(menu);
        }
        else
        {
            dbRole.Label = roleItemLabel ?? roleToAdd.Name;
            dbRole.Description = roleItemDescription;

            _dbContext.Update(dbRole);
        }

        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        IResult modifyMenuResult = await ModifyRoleMenu(menu).ConfigureAwait(false);
        if (!modifyMenuResult.IsSuccess)
            return modifyMenuResult;

        return await _feedbackService.SendContextualSuccessAsync("That role has been added.", ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("remove-role")]
    [Description("Removes a role selection item from a role menu.")]
    [Ephemeral]
    public async Task<IResult> RemoveRole
    (
        [Description("The ID of the role menu message.")] Snowflake messageId,
        [Description("The role to remove.")] IRole roleToRemove
    )
    {
        GuildRoleMenu? menu = GetGuildRoleMenu(messageId.Value);
        if (menu is null)
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: CancellationToken).ConfigureAwait(false);

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
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        IResult modifyMenuResult = await ModifyRoleMenu(menu).ConfigureAwait(false);
        if (!modifyMenuResult.IsSuccess)
            return modifyMenuResult;

        return await _feedbackService.SendContextualSuccessAsync("That role has been removed.", ct: CancellationToken).ConfigureAwait(false);
    }

    private GuildRoleMenu? GetGuildRoleMenu(ulong messageId)
        => _dbContext.RoleMenus.Include(r => r.Roles).FirstOrDefault(r => r.GuildId == _context.GuildID.Value.Value && r.MessageId == messageId);

    private async Task<IResult> ModifyRoleMenu(GuildRoleMenu menu)
    {
        Result<IMessage> menuModificationResult = await _channelApi.EditMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            embeds: new IEmbed[] { CreateRoleMenuEmbed(menu) },
            components: menu.Roles.Count > 0 ? CreateRoleMenuMessageComponents(menu) : new Optional<IReadOnlyList<IMessageComponent>>(),
            ct: CancellationToken
        ).ConfigureAwait(false);

        if (!menuModificationResult.IsSuccess)
        {
            IResult res = await CheckRoleMenuExists(menu).ConfigureAwait(false);
            if (!res.IsSuccess)
                return Result.FromSuccess();
        }

        return menuModificationResult;
    }

    private static IEmbed CreateRoleMenuEmbed(GuildRoleMenu menu)
        => new Embed
        (
            menu.Title,
            Description: menu.Description,
            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR,
            Footer: new EmbedFooter("If you can't deselect a role, refresh your client by pressing Ctrl+R.")
        );

    private static List<IMessageComponent> CreateRoleMenuMessageComponents(GuildRoleMenu menu)
    {
        List<SelectOption> selectOptions = menu.Roles.ConvertAll
        (
            r => new SelectOption
            (
                r.Label,
                r.RoleId.ToString(),
                r.Description ?? "",
                default,
                false
            )
        );

        SelectMenuComponent selectMenu = new
        (
            ComponentIdFormatter.GetId(RoleComponentKeys.ToggleRole, menu.MessageId.ToString()),
            selectOptions,
            "Toggle roles...",
            1,
            menu.Roles.Count,
            false
        );

        ActionRowComponent actionRow = new(new List<IMessageComponent> { selectMenu });
        return new List<IMessageComponent> { actionRow };
    }

    private async Task<IResult> CheckRoleMenuExists(GuildRoleMenu menu)
    {
        Result<IMessage> getMessageResult = await _channelApi.GetChannelMessageAsync
        (
            new Snowflake(menu.ChannelId, Remora.Discord.API.Constants.DiscordEpoch),
            new Snowflake(menu.MessageId, Remora.Discord.API.Constants.DiscordEpoch),
            CancellationToken
        ).ConfigureAwait(false);

        if (!getMessageResult.IsSuccess)
        {
            await _feedbackService.SendContextualErrorAsync
            (
                "That role menu appears to have been deleted! Please create a new one.",
                ct: CancellationToken
            ).ConfigureAwait(false);

            _dbContext.Remove(menu);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
        }

        return getMessageResult;
    }
}
