using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;

namespace UVOCBot.Plugins.Planetside.Commands;

public class CharacterNameAutocompleteProvider : IAutocompleteProvider
{
    private readonly ILogger<CharacterNameAutocompleteProvider> _logger;
    private readonly IQueryService _queryService;

    public string Identity => "autocomplete::ps2CharacterName";

    public CharacterNameAutocompleteProvider(ILogger<CharacterNameAutocompleteProvider> logger, IQueryService queryService)
    {
        _logger = logger;
        _queryService = queryService;
    }

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
    (
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrEmpty(userInput) || userInput.Length < 5)
            return Array.Empty<IApplicationCommandOptionChoice>();

        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("character")
            .Where("name.first_lower", SearchModifier.StartsWith, userInput.ToLower())
            .WithSortOrder("times.last_login", SortOrder.Descending)
            .ShowFields("name.first")
            .WithLimit(10);

        List<CharacterNameElement>? characterNames = null;
        try
        {
            characterNames = await _queryService.GetAsync<List<CharacterNameElement>>(query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query character names for autocomplete");
        }

        if (characterNames is null)
            return Array.Empty<IApplicationCommandOptionChoice>();

        return characterNames.Select
            (
                c => new ApplicationCommandOptionChoice(c.Name.First, c.Name.First)
            ).ToList();
    }

    private record CharacterNameElement(Name Name);
}
