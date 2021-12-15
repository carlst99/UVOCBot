using FuzzySharp;
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
using UVOCBot.Plugins.Greetings.Abstractions.Services;
using UVOCBot.Plugins.Greetings.Objects;

namespace UVOCBot.Plugins.Greetings.Services;

/// <inheritdoc />
public class GreetingService : IGreetingService
{
    private readonly ICensusQueryService _censusService;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly DiscordContext _dbContext;

    public GreetingService
    (
        ICensusQueryService censusService,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        DiscordContext dbContext
    )
    {
        _censusService = censusService;
        _channelApi = channelApi;
        _guildApi = guildApi;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result<IMessage?>> SendGreeting
    (
        Snowflake guildID,
        IGuildMember member,
        CancellationToken ct = default
    )
    {
        if (!member.User.IsDefined(out IUser? user))
            return new ArgumentNullError(nameof(user));

        // Get the welcome message settings
        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(guildID.Value, ct).ConfigureAwait(false);

        if (!welcomeMessage.IsEnabled)
            return Result<IMessage?>.FromSuccess(null);

        Result<IEnumerable<string>>? nicknameGuesses = null;
        if (welcomeMessage.DoIngameNameGuess)
            nicknameGuesses = await DoFuzzyNicknameGuess(user.Username, welcomeMessage.OutfitId, ct).ConfigureAwait(false);

        // Prepare components of the welcome message
        List<ButtonComponent> messageButtons = CreateWelcomeMessageButtons
            (welcomeMessage,
            nicknameGuesses.HasValue && nicknameGuesses.Value.IsDefined() ? nicknameGuesses.Value.Entity : null,
            user.ID.Value);

        string messageContent = SubstituteMessageVariables(welcomeMessage.Message, user.ID);

        // Send the welcome message
        Result<IMessage> sendWelcomeMessageResult = await _channelApi.CreateMessageAsync
        (
            new Snowflake(welcomeMessage.ChannelId, Remora.Discord.API.Constants.DiscordEpoch),
            messageContent,
            allowedMentions: new AllowedMentions(new List<MentionType>() { MentionType.Users }),
            components: new List<IMessageComponent>() { new ActionRowComponent(messageButtons) },
            ct: ct
        ).ConfigureAwait(false);

        // Assign default roles
        await _guildApi.ModifyRoles
        (
            guildID,
            user.ID,
            member.Roles,
            rolesToAdd: welcomeMessage.DefaultRoles,
            ct: ct
        ).ConfigureAwait(false);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return sendWelcomeMessageResult;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
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
            if (matchRatio > minMatchRatio)
            {
                nicknameGuesses.Add(new Tuple<string, int>(m.CharacterName.Name.First, matchRatio));
                newMembers.RemoveAt(i);
                i--;
            }
        }

        nicknameGuesses.Sort((Tuple<string, int> x, Tuple<string, int> y) => y.Item2.CompareTo(x.Item2));

        if (nicknameGuesses.Count < maxGuesses)
            nicknameGuesses.AddRange(newMembers.Take(maxGuesses - nicknameGuesses.Count).Select(m => new Tuple<string, int>(m.CharacterName.Name.First, 0)));

        return Result<IEnumerable<string>>.FromSuccess(nicknameGuesses.Select(g => g.Item1));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ulong>>> SetAlternateRoles
    (
        Snowflake guildID,
        IGuildMember member,
        CancellationToken ct = default
    )
    {
        if (!member.User.IsDefined(out IUser? user))
            return new ArgumentNullError(nameof(user));

        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(guildID.Value, ct).ConfigureAwait(false);

        // Remove the default roles and add the alternate roles
        Result roleChangeResult = await _guildApi.ModifyRoles
        (
            guildID,
            user.ID,
            member.Roles,
            welcomeMessage.AlternateRoles,
            welcomeMessage.DefaultRoles,
            ct
        ).ConfigureAwait(false);

        return !roleChangeResult.IsSuccess
            ? Result<IReadOnlyList<ulong>>.FromError(roleChangeResult)
            : Result<IReadOnlyList<ulong>>.FromSuccess(welcomeMessage.AlternateRoles.AsReadOnly());
    }

    private static List<ButtonComponent> CreateWelcomeMessageButtons
    (
        GuildWelcomeMessage welcomeMessage,
        IEnumerable<string>? nicknameGuesses,
        ulong userId
    )
    {
        List<ButtonComponent> messageButtons = new();

        if (welcomeMessage.OfferAlternateRoles)
        {
            messageButtons.Add(new ButtonComponent(
                ButtonComponentStyle.Danger,
                welcomeMessage.AlternateRoleLabel,
                CustomID: ComponentIDFormatter.GetId(GreetingComponentKeys.SetAlternateRoles, userId.ToString())));
        }

        if (nicknameGuesses is not null)
        {
            foreach (string nickname in nicknameGuesses)
            {
                messageButtons.Add(new ButtonComponent
                (
                    ButtonComponentStyle.Primary,
                    "My PS2 name is: " + nickname,
                    CustomID: ComponentIDFormatter.GetId(GreetingComponentKeys.SetGuessedNickname, userId.ToString() + '@' + nickname))
                );
            }

            messageButtons.Add(new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                "My PS2 name is none of these!",
                CustomID: ComponentIDFormatter.GetId(GreetingComponentKeys.NoNicknameMatches, userId.ToString()))
            );
        }

        return messageButtons;
    }

    private static string SubstituteMessageVariables(string welcomeMessage, Snowflake userId)
        => welcomeMessage.Replace("<name>", Formatter.UserMention(userId));
}
