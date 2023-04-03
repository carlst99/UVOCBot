using FuzzySharp;
using Microsoft.Extensions.Logging;
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
using UVOCBot.Core.Extensions;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Greetings.Abstractions.Services;
using UVOCBot.Plugins.Greetings.Objects;

namespace UVOCBot.Plugins.Greetings.Services;

/// <inheritdoc />
public class GreetingService : IGreetingService
{
    private readonly ILogger<GreetingService> _logger;
    private readonly ICensusQueryService _censusService;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly DiscordContext _dbContext;

    public GreetingService
    (
        ILogger<GreetingService> logger,
        ICensusQueryService censusService,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        DiscordContext dbContext
    )
    {
        _logger = logger;
        _censusService = censusService;
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

        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(guildID.Value, ct).ConfigureAwait(false);
        if (!welcomeMessage.IsEnabled)
            return Result.FromSuccess();

        List<IMessageComponent> messageComponents = new();

        if (welcomeMessage.DoIngameNameGuess)
        {
            Result<List<ButtonComponent>> getNicknameButtons = await CreateNicknameGuessButtonsAsync
            (
                welcomeMessage,
                user,
                ct
            ).ConfigureAwait(false);

            if (!getNicknameButtons.IsSuccess)
                _logger.LogWarning("Failed to retrieve nickname guesses: {Error}", getNicknameButtons.Error);
            else
                messageComponents.Add(new ActionRowComponent(getNicknameButtons.Entity));
        }

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
    public async Task<Result<IEnumerable<string>>> DoFuzzyNicknameGuess
    (
        string username,
        ulong outfitId,
        CancellationToken ct
    )
    {
        const int minMatchRatio = 65;
        const int maxGuesses = 2;
        List<Tuple<string, int>> nicknameGuesses = new();

        Result<IReadOnlyList<NewOutfitMember>> getNewMembers = await _censusService.GetNewOutfitMembersAsync(outfitId, 10, ct).ConfigureAwait(false);
        if (!getNewMembers.IsDefined())
            return Result<IEnumerable<string>>.FromError(getNewMembers);

        List<NewOutfitMember> newMembers = new(getNewMembers.Entity);

        for (int i = 0; i < newMembers.Count; i++)
        {
            NewOutfitMember m = newMembers[i];

            int matchRatio = Fuzz.PartialRatio(m.CharacterName.Name.First, username);
            if (matchRatio <= minMatchRatio)
                continue;

            nicknameGuesses.Add(new Tuple<string, int>(m.CharacterName.Name.First, matchRatio));
            newMembers.RemoveAt(i);
            i--;
        }

        nicknameGuesses.Sort((x, y) => y.Item2.CompareTo(x.Item2));

        if (nicknameGuesses.Count < maxGuesses)
            nicknameGuesses.AddRange(newMembers.Take(maxGuesses - nicknameGuesses.Count).Select(m => new Tuple<string, int>(m.CharacterName.Name.First, 0)));

        return Result<IEnumerable<string>>.FromSuccess(nicknameGuesses.Select(g => g.Item1));
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

        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(guildID.Value, ct)
            .ConfigureAwait(false);

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

    private async Task<Result<List<ButtonComponent>>> CreateNicknameGuessButtonsAsync
    (
        GuildWelcomeMessage welcomeMessage,
        IUser user,
        CancellationToken ct
    )
    {
        if (!welcomeMessage.DoIngameNameGuess)
            throw new InvalidOperationException("In-game name guesses are not enabled for this guild");

        Result<IEnumerable<string>> getNicknameGuesses = await DoFuzzyNicknameGuess
        (
            user.Username,
            welcomeMessage.OutfitId,
            ct
        ).ConfigureAwait(false);

        if (!getNicknameGuesses.IsDefined(out IEnumerable<string>? nicknameGuesses))
            return Result<List<ButtonComponent>>.FromError(getNicknameGuesses);

        string userID = user.ID.Value.ToString();

        List<ButtonComponent> messageButtons = nicknameGuesses.Select
        (
            nickname => new ButtonComponent
            (
                ButtonComponentStyle.Primary,
                "My PS2 name is: " + nickname,
                CustomID: ComponentIDFormatter.GetId(GreetingComponentKeys.SetGuessedNickname, userID + '@' + nickname)
            )
        ).ToList();

        messageButtons.Add(new ButtonComponent
        (
            ButtonComponentStyle.Secondary,
            "My PS2 name is none of these!",
            CustomID: ComponentIDFormatter.GetId(GreetingComponentKeys.NoNicknameMatches, userID)
        ));

        return messageButtons;
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
