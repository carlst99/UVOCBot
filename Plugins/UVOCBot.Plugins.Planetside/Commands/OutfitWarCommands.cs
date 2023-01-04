using DbgCensus.Core.Objects;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Commands;

[Group("outfit-wars")]
public class OutfitWarCommands : CommandGroup
{
    private readonly IInteraction _context;
    private readonly ICensusApiService _censusApi;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public OutfitWarCommands
    (
        IInteractionContext context,
        ICensusApiService censusApi,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
        _censusApi = censusApi;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    [Command("matches")]
    [Description("Gets the matches for the current outfit war on a particular server.")]
    [Deferred]
    public async Task<Result> GetActiveWarMatchesCommandAsync
    (
        [Description("Set your default server with '/default-server'.")]
        ValidWorldDefinition server = 0
    )
    {
        if (server == 0)
        {
            Result<ValidWorldDefinition> defaultWorld = await CheckForDefaultServer();
            if (!defaultWorld.IsSuccess)
                return (Result)defaultWorld;

            server = defaultWorld.Entity;
        }

        Result<OutfitWar?> getCurrentWar = await _censusApi.GetCurrentOutfitWar(server, CancellationToken);
        if (!getCurrentWar.IsDefined(out OutfitWar? currentWar))
        {
            await _feedbackService.SendContextualWarningAsync
            (
                $"There is no active outfit war on {server} at this time",
                ct: CancellationToken
            );
            return (Result)getCurrentWar;
        }

        Result<OutfitWarRoundWithMatches?> getCurrentRound = await _censusApi.GetCurrentOutfitWarMatches
        (
            currentWar.OutfitWarID,
            CancellationToken
        );

        if (!getCurrentRound.IsDefined(out OutfitWarRoundWithMatches? round))
        {
            await _feedbackService.SendContextualWarningAsync
            (
                $"There is no active round for the war: {currentWar.Title.English}",
                ct: CancellationToken
            );
            return (Result)getCurrentRound;
        }

        List<OutfitWarMatch> matches = round.Matches
            .Where(x => x.StartTime > DateTimeOffset.UtcNow)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.Order)
            .ToList();

        ulong[] outfitIDs = new ulong[matches.Count * 2];
        for (int i = 0; i < matches.Count; i++)
        {
            outfitIDs[i * 2] = matches[i].OutfitAId;
            outfitIDs[(i * 2) + 1] = matches[i].OutfitBId;
        }
        Result<List<Outfit>> outfits = await _censusApi.GetOutfitsAsync(outfitIDs, CancellationToken)
            .ConfigureAwait(false);
        if (!outfits.IsSuccess)
            return (Result)outfits;

        List<IEmbedField> fields = new();
        StringBuilder sb = new();

        foreach (IGrouping<DateTimeOffset, OutfitWarMatch> value in matches.GroupBy(x => x.StartTime))
        {
            sb.Clear();

            foreach (OutfitWarMatch match in value)
            {
                Outfit? outfitA = outfits.Entity.FirstOrDefault(o => o.OutfitId == match.OutfitAId);
                Outfit? outfitB = outfits.Entity.FirstOrDefault(o => o.OutfitId == match.OutfitBId);

                sb.Append(FactionToEmoji(match.OutfitAFactionID))
                    .Append(" [")
                    .Append(outfitA?.Alias ?? match.OutfitAId.ToString())
                    .Append("] ")
                    .Append(outfitA?.Name ?? "<Unknown>")
                    .Append(" vs. ")
                    .Append(FactionToEmoji(match.OutfitBFactionID))
                    .Append(" [")
                    .Append(outfitB?.Alias ?? match.OutfitBId.ToString())
                    .Append("] ")
                    .AppendLine(outfitB?.Name ?? "<Unknown>");
            }

            fields.Add(new EmbedField
            (
                Formatter.Timestamp(value.Key, TimestampStyle.LongDateTime),
                sb.ToString()
            ));
        }

        Embed embed = new
        (
            $"Upcoming matches for Round {round.Order}",
            Description: $"{round.Stage} round, ending {Formatter.Timestamp(round.EndTime, TimestampStyle.RelativeTime)}",
            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR,
            Fields: fields
        );

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    [Command("registrations")]
    [Description("Gets the outfits that have registered for the current outfit war on a particular server.")]
    [Deferred]
    public async Task<Result> GetOutfitRegistrationsCommandAsync
    (
        [Description("Set your default server with '/default-server'.")]
        ValidWorldDefinition server = 0
    )
    {
        if (server == 0)
        {
            Result<ValidWorldDefinition> defaultWorld = await CheckForDefaultServer();
            if (!defaultWorld.IsSuccess)
                return (Result)defaultWorld;

            server = defaultWorld.Entity;
        }

        Result<OutfitWar?> getCurrentWar = await _censusApi.GetCurrentOutfitWar(server, CancellationToken);
        if (!getCurrentWar.IsDefined(out OutfitWar? currentWar))
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                $"There is no active outfit war on {server} at this time",
                ct: CancellationToken
            );
        }

        Result<List<OutfitWarRegistration>> getRegs = await _censusApi.GetOutfitWarRegistrationsAsync(currentWar.OutfitWarID, CancellationToken);
        if (!getRegs.IsDefined(out List<OutfitWarRegistration>? regs))
            return (Result)getRegs;

        Result<List<Outfit>> outfits = await _censusApi.GetOutfitsAsync(regs.Select(o => o.OutfitID), CancellationToken)
            .ConfigureAwait(false);
        if (!outfits.IsSuccess)
            return (Result)outfits;

        StringBuilder sb = new();
        List<IEmbedField> fields = new();

        void AppendRegistrationToStringBuilder(OutfitWarRegistration registration, bool appendLine = true)
        {
            Outfit? o = outfits.Entity.FirstOrDefault(o => o.OutfitId == registration.OutfitID);
            sb.Append(FactionToEmoji((FactionDefinition)registration.FactionID))
                .Append(" [")
                .Append(o?.Alias ?? registration.OutfitID.ToString())
                .Append("] ");

            if (appendLine)
                sb.AppendLine(o?.Name ?? "<Unknown>");
            else
                sb.Append(o?.Name ?? "<Unknown>");
        }

        int fullRegCount = 0;
        foreach (OutfitWarRegistration reg in regs.Where(o => o.Status == "Full").OrderBy(o => o.RegistrationOrder))
        {
            AppendRegistrationToStringBuilder(reg);
            fullRegCount++;
        }

        if (fullRegCount > 0)
            fields.Add(new EmbedField($"Fully Registered ({fullRegCount})", sb.ToString()));

        sb.Clear();
        int waitingRegCount = 0;
        foreach (OutfitWarRegistration reg in regs.Where(o => o.Status == "WaitingOnNextFullReg"))
        {
            AppendRegistrationToStringBuilder(reg);
            waitingRegCount++;
        }

        if (waitingRegCount > 0)
        {
            fields.Add(new EmbedField
            (
                $"Waiting ({waitingRegCount}) - an even number of full registrations is required",
                sb.ToString()
            ));
        }

        sb.Clear();
        int partialRegCount = 0;
        foreach (OutfitWarRegistration reg in regs.Where(o => o.Status == "Partial").OrderByDescending(o => o.MemberSignupCount))
        {
            AppendRegistrationToStringBuilder(reg, false);
            sb.Append(" - ")
                .Append(reg.MemberSignupCount)
                .AppendLine(" signups");
            partialRegCount++;
        }

        if (partialRegCount > 0)
        {
            fields.Add(new EmbedField
            (
                $"Partially Registered ({partialRegCount}) - {currentWar.OutfitSignupRequirement} member are required to signup",
                sb.ToString()
            ));
        }

        Embed embed = new
        (
            Title: $"{server} registrations for {currentWar.Title.English.Value}",
            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR,
            Fields: fields,
            Timestamp: DateTimeOffset.UtcNow
        );
        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    private async Task<Result<ValidWorldDefinition>> CheckForDefaultServer()
    {
        if (!_context.GuildID.HasValue)
            return new GenericCommandError("To use this command in a DM you must provide a server.");

        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        if (settings.DefaultWorld is null)
            return new GenericCommandError($"You haven't set a default server! Please do so using the { Formatter.InlineQuote("/default-server") } command.");

        return (ValidWorldDefinition)settings.DefaultWorld;
    }

    private static string FactionToEmoji(FactionDefinition faction)
        => faction switch
        {
            FactionDefinition.VS => Formatter.Emoji("purple_circle"),
            FactionDefinition.NC => Formatter.Emoji("blue_circle"),
            FactionDefinition.TR => Formatter.Emoji("red_circle"),
            FactionDefinition.NSO => Formatter.Emoji("white_circle"),
            _ => Formatter.Emoji("black_circle")
        };
}
