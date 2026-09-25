using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Roles.Abstractions.Services;

namespace UVOCBot.Plugins.Roles.Services;

/// <inheritdoc cref="IRoleMenuService"/>
public class RoleMenuService : IRoleMenuService
{
    private readonly ILogger<RoleMenuService> _logger;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly DiscordContext _dbContext;
    private readonly IFeedbackService _feedbackService;

    public RoleMenuService
    (
        ILogger<RoleMenuService> logger,
        IDiscordRestChannelAPI channelApi,
        DiscordContext dbContext,
        IFeedbackService feedbackService
    )
    {
        _logger = logger;
        _channelApi = channelApi;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    /// <inheritdoc />
    public bool TryGetGuildRoleMenu
    (
        Optional<Snowflake> guildId,
        ulong messageID,
        [NotNullWhen(true)] out GuildRoleMenu? menu
    )
    {
        menu = null;
        if (!guildId.HasValue)
            return false;

        menu = _dbContext.RoleMenus
            .Include(grm => grm.Roles)
            .FirstOrDefault
            (
                grm => grm.GuildId == guildId.Value.Value
                     && grm.MessageId == messageID
            );

        return menu is not null;
    }

    /// <inheritdoc />
    public async Task<Result<IMessage>> UpdateRoleMenuMessageAsync
    (
        GuildRoleMenu menu,
        bool restoreDeletedMenus,
        CancellationToken ct = default
    )
    {
        menu.Roles.Sort
        (
            (r1, r2) => string.Compare(r1.Label, r2.Label, StringComparison.Ordinal)
        );
        List<IMessageComponent> components = CreateRoleMenuMessageComponents(menu);

        Result<IMessage> upsertResult = await _channelApi.EditMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            components: components,
            flags: MessageFlags.IsComponentsV2,
            ct: ct
        );

        // If we couldn't edit the message, it's quite possible it was deleted. Let's check if that was the case
        // and attempt to recreate the message if so

        if (upsertResult.IsSuccess || !restoreDeletedMenus)
            return upsertResult;

        // Test for an UnknownMessage error - meaning we should recreate the message
        bool failure = upsertResult.Error is not RestResultError<RestError> restError
            || !restError.Error.Code.TryGet(out DiscordError discordError)
            || discordError is not DiscordError.UnknownMessage;

        if (failure)
            return await ProcessMessageUpsertErrorResult(menu, upsertResult, ct);

        upsertResult = await _channelApi.CreateMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            components: components,
            flags: MessageFlags.IsComponentsV2,
            ct: ct
        );

        if (!upsertResult.IsDefined(out IMessage? createdMsg))
            return await ProcessMessageUpsertErrorResult(menu, upsertResult, ct);

        menu.MessageId = createdMsg.ID.Value;
        await _dbContext.SaveChangesAsync(ct);

        return upsertResult;
    }

    private async ValueTask<Result<IMessage>> ProcessMessageUpsertErrorResult
    (
        GuildRoleMenu menu,
        Result<IMessage> result,
        CancellationToken ct
    )
    {
        _logger.LogError
        (
            "Failed to upsert role menu with guild / channel / message ID {GuildId} / {MessageId} / {ChannelId}",
            menu.GuildId,
            menu.ChannelId,
            menu.MessageId
        );

        if
        (
            result.Error is not RestResultError<RestError>
            {
                Error:
                {
                    Code: { HasValue: true, Value: DiscordError.InvalidFormBody },
                    Errors: { HasValue: true, Value: { Count: > 0 } errValues }
                }
            }
        )
        {
            return result;
        }

        // Drill all the way down, only processing the first member error at each level - the user will have to
        // solve them one at a time anyway
        (string key, OneOf<IPropertyErrorDetails, IReadOnlyList<IErrorDetails>> value) = errValues.First();
        if (key is "emoji" || (value.IsT1 && value.AsT1.Any(x => x.Code is "COMPONENT_INVALID_EMOJI")))
            return await SendEmojiCheckMessage(menu, ct);

        string? currKey = key;
        IPropertyErrorDetails? currDetails = value.AsT0;
        do
        {
            if (currKey == "emoji" || currDetails.Errors?.Any(x => x.Code is "COMPONENT_INVALID_EMOJI") is true)
                return await SendEmojiCheckMessage(menu, ct);

            if (currDetails.MemberErrors is not null)
                (currKey, currDetails) = currDetails.MemberErrors!.FirstOrDefault();
            else
                (currKey, currDetails) = (null, null);
        }
        while (currDetails is not null);

        return result;
    }

    private async Task<Result<IMessage>> SendEmojiCheckMessage(GuildRoleMenu menu, CancellationToken ct)
    {
        StringBuilder sb = new();
        sb.AppendLine(Formatter.Bold("The role menu could not be updated due to invalid emoji(s)."));
        sb.AppendLine
        (
            "Please review the below roles on the menu for broken emoji, and use the `add-role` command to update"
                + " those that are broken"
        );
        sb.AppendLine();

        foreach (GuildRoleMenuRole role in menu.Roles.Where(x => x.Emoji is not null))
            sb.Append(role.Emoji).Append(" - ").AppendLine(Formatter.RoleMention(role.RoleId));

        return await _feedbackService.SendContextualAsync
        (
            sb.ToString(),
            default,
            new FeedbackMessageOptions
            (
                AllowedMentions: new AllowedMentions(Roles: new Optional<IReadOnlyList<Snowflake>>([]))
            ),
            ct
        );
    }

    private static List<IMessageComponent> CreateRoleMenuMessageComponents(GuildRoleMenu menu)
    {
        List<SectionComponent> roleButtonsWithDesc = [];
        List<ButtonComponent> roleButtons = [];

        foreach (GuildRoleMenuRole role in menu.Roles.Where(x => string.IsNullOrEmpty(x.Description)))
        {
            Optional<IPartialEmoji> emoji = default;
            if (role.Emoji is not null)
            {
                emoji = Formatter.EmojiFromString(role.Emoji).MapOr(x => new Optional<IPartialEmoji>(x), default);
                Console.WriteLine(emoji);
            }

            roleButtons.Add(new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                role.Label,
                emoji,
                ComponentIDFormatter.GetId(RoleComponentKeys.ToggleRole, role.RoleId.ToString())
            ));
        }

        foreach (GuildRoleMenuRole role in menu.Roles.Where(x => !string.IsNullOrEmpty(x.Description)))
        {
            Optional<IPartialEmoji> emoji = default;
            if (role.Emoji is not null)
            {
                emoji = Formatter.EmojiFromString(role.Emoji).MapOr(x => new Optional<IPartialEmoji>(x), default);
                Console.WriteLine(emoji);
            }

            roleButtonsWithDesc.Add(new SectionComponent
            (
                [new TextDisplayComponent(role.Description!)],
                new ButtonComponent
                (
                    ButtonComponentStyle.Secondary,
                    role.Label,
                    emoji,
                    ComponentIDFormatter.GetId(RoleComponentKeys.ToggleRole, role.RoleId.ToString())
                )
            ));
        }

        StringBuilder headerSb = new(Formatter.Header2(menu.Title));
        if (!string.IsNullOrEmpty(menu.Description))
            headerSb.AppendLine().Append(menu.Description);
        TextDisplayComponent header = new(headerSb.ToString());

        List<ActionRowComponent> actionRows = roleButtons.Chunk(5)
            .Select(bl => new ActionRowComponent(bl))
            .ToList();

        List<IMessageComponent> containerChildren = [header];
        if (roleButtonsWithDesc.Count > 0)
        {
            containerChildren.Add(new SeparatorComponent());
            containerChildren.AddRange(roleButtonsWithDesc);
        }
        if (actionRows.Count > 0)
        {
            containerChildren.Add(new SeparatorComponent());
            containerChildren.AddRange(actionRows);
        }

        ContainerComponent container = new
        (
            containerChildren,
            false,
            DiscordConstants.DEFAULT_EMBED_COLOUR
        );

        return [container];
    }
}
