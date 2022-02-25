using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;

namespace UVOCBot.Commands;

[Group("team")]
[Description("Commands that help with team generation")]
[RequireContext(ChannelContext.Guild)]
public class TeamGenerationCommands : CommandGroup
{
    private readonly ICommandContext _context;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly FeedbackService _feedbackService;

    public TeamGenerationCommands
    (
        ICommandContext context,
        IDiscordRestGuildAPI guildAPI,
        FeedbackService feedbackService
    )
    {
        _context = context;
        _feedbackService = feedbackService;
        _guildAPI = guildAPI;
    }

    [Command("random-from-role")]
    [Description("Randomly sorts members with a certain role into teams")]
    public async Task<IResult> RandomFromRoleCommandAsync
    (
        [Description("Teams will be generated using members with this role")] IRole role,
        [Description("The number of teams to generate")] int numberOfTeams
    )
    {
        if (numberOfTeams < 2)
            return await _feedbackService.SendContextualErrorAsync("At least two teams are required", ct: CancellationToken).ConfigureAwait(false);

        List<ulong> roleMembers = new();

        await foreach (Result<IReadOnlyList<IGuildMember>> users in _guildAPI.GetAllMembersAsync(_context.GuildID.Value, (m) => m.Roles.Contains(role.ID), CancellationToken))
        {
            if (!users.IsDefined())
                return users;

            roleMembers.AddRange(users.Entity.Select(m => m.User.Value.ID.Value));
        }

        return await SendRandomTeams(roleMembers, numberOfTeams, $"Build from the role { Formatter.RoleMention(role.ID)  }").ConfigureAwait(false);
    }

    private async Task<IResult> SendRandomTeams(IList<ulong> memberPool, int teamCount, string embedDescription)
    {
        if (memberPool.Count < teamCount)
            return await _feedbackService.SendContextualErrorAsync("There cannot be more teams than team members", ct: CancellationToken).ConfigureAwait(false);

        List<List<ulong>> teams = await CreateRandomTeams(memberPool, teamCount).ConfigureAwait(false);

        return await _feedbackService.SendContextualEmbedAsync
        (
            BuildTeamsEmbed(teams, "Random Teams", embedDescription),
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions()),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<List<List<ulong>>> CreateRandomTeams(IList<ulong> memberPool, int teamCount)
    {
        List<List<ulong>> teams = new(teamCount);

        await Task.Run
        (
            () =>
            {
                memberPool.Shuffle();

                for (int i = 0; i < teamCount; i++)
                    teams.Add(new List<ulong>());

                int teamPos = 0;
                for (int i = 0; i < memberPool.Count; i++)
                {
                    teams[teamPos++].Add(memberPool[i]);
                    if (teamPos == teamCount)
                        teamPos = 0;
                }
            },
            CancellationToken
        ).ConfigureAwait(false);

        return teams;
    }

    private static Embed BuildTeamsEmbed(List<List<ulong>> teams, string embedTitle, string embedDescription)
    {
        // TODO: Add support for teams that will exceed longer than 6000 characters. See #22
        List<EmbedField> embedFields = new();

        for (int i = 0; i < teams.Count; i++)
        {
            StringBuilder sb = new();

            foreach (ulong userID in teams[i])
                sb.Append("- ").AppendLine(Formatter.UserMention(userID));

            embedFields.Add(new EmbedField($"Team {i + 1}", sb.ToString()));
        }

        return new Embed
        {
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Title = embedTitle,
            Description = embedDescription,
            Fields = embedFields
        };
    }
}
