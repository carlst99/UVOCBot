using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Greetings.Abstractions.Services;

namespace UVOCBot.Plugins.Greetings.Services;

/// <inheritdoc />
public class GreetingService : IGreetingService
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly DiscordContext _dbContext;

    public GreetingService
    (
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        DiscordContext dbContext
    )
    {
        _channelApi = channelApi;
        _guildApi = guildApi;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result> SendGreeting
    (
        Snowflake guildID,
        IGuildMember member,
        CancellationToken ct = default
    )
    {
        if (!member.User.IsDefined(out IUser? user))
            return new ArgumentNullError(nameof(user));

        GuildWelcomeMessage? welcomeMessage = await _dbContext.FindAsync<GuildWelcomeMessage>(guildID.Value, ct).ConfigureAwait(false);
        if (welcomeMessage is not { IsEnabled: true })
            return Result.FromSuccess();

        List<IMessageComponent> messageComponents = [];

        if (welcomeMessage.AlternateRolesets.Count > 0)
        {
            List<ButtonComponent> altRolesetButtons = CreateAlternateRolesetButtons(welcomeMessage, user.ID);
            messageComponents.Add(new ActionRowComponent(altRolesetButtons));
        }

        string messageContent = SubstituteMessageVariables(welcomeMessage.Message, user.ID);

        // Send the welcome message
        Result<IMessage> sendWelcomeMessageResult = await _channelApi.CreateMessageAsync
        (
            DiscordSnowflake.New(welcomeMessage.ChannelId),
            messageContent,
            allowedMentions: new AllowedMentions(new List<MentionType> { MentionType.Users }),
            components: messageComponents.Count == 0
                ? default(Optional<IReadOnlyList<IMessageComponent>>)
                : messageComponents,
            ct: ct
        ).ConfigureAwait(false);

        // We add roles one-at-a-time because there is no bulk add-role endpoint,
        // and modifying roles risks losing roles that other welcome bots have
        // assigned as we are not able to see that change in time
        foreach (ulong roleID in welcomeMessage.DefaultRoles)
        {
            await _guildApi.AddGuildMemberRoleAsync
            (
                guildID,
                user.ID,
                DiscordSnowflake.New(roleID),
                "Default role assigned by greeting",
                ct
            ).ConfigureAwait(false);
        }

        return (Result)sendWelcomeMessageResult;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ulong>>> SetAlternateRolesetAsync
    (
        Snowflake guildID,
        IGuildMember member,
        ulong rolesetID,
        CancellationToken ct = default
    )
    {
        if (!member.User.IsDefined(out IUser? user))
            return new ArgumentNullError(nameof(user));

        GuildWelcomeMessage? welcomeMessage = await _dbContext.FindAsync<GuildWelcomeMessage>(guildID.Value, ct)
            .ConfigureAwait(false);

        if (welcomeMessage is null)
            return Result<IReadOnlyList<ulong>>.FromSuccess(Array.Empty<ulong>());

        GuildGreetingAlternateRoleSet? roleset = welcomeMessage.AlternateRolesets
            .FirstOrDefault(rs => rs.ID == rolesetID);

        if (roleset is null)
            return new GenericCommandError("That roleset has been removed by your guild administrator!");

        List<ulong> toRemove = welcomeMessage.DefaultRoles;
        toRemove.AddRange
        (
            welcomeMessage.AlternateRolesets.Where(rs => rs.ID != rolesetID)
                .SelectMany(rs => rs.RoleIDs)
        );

        // Remove the default roles and add the alternate roles
        Result roleChangeResult = await _guildApi.ModifyRoles
        (
            guildID,
            user.ID,
            member.Roles,
            roleset.RoleIDs,
            toRemove,
            ct
        ).ConfigureAwait(false);

        return !roleChangeResult.IsSuccess
            ? Result<IReadOnlyList<ulong>>.FromError(roleChangeResult)
            : Result<IReadOnlyList<ulong>>.FromSuccess(roleset.RoleIDs);
    }

    /// <inheritdoc />
    public ISelectOption[] CreateAlternateRoleSelectOptions(IReadOnlyList<GuildGreetingAlternateRoleSet> alternateRolesets)
    {
        ISelectOption[] options = new ISelectOption[alternateRolesets.Count];
        for (int i = 0; i < alternateRolesets.Count; i++)
        {
            GuildGreetingAlternateRoleSet roleset = alternateRolesets[i];

            options[i] = new SelectOption
            (
                roleset.Description,
                roleset.ID.ToString()
            );
        }

        return options;
    }

    private static List<ButtonComponent> CreateAlternateRolesetButtons
    (
        GuildWelcomeMessage welcomeMessage,
        Snowflake userID
    )
        => welcomeMessage.AlternateRolesets.Select
        (
            roleset => new ButtonComponent
            (
                ButtonComponentStyle.Primary,
                roleset.Description,
                CustomID: ComponentIDFormatter.GetId
                (
                    GreetingComponentKeys.SetAlternateRoleset,
                    userID.Value + "|" + roleset.ID
                )
            )
        ).ToList();

    private static string SubstituteMessageVariables(string welcomeMessage, Snowflake userId)
        => welcomeMessage.Replace("<name>", Formatter.UserMention(userId));
}
