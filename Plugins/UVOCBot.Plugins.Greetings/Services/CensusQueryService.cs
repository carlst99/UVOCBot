using DbgCensus.Core.Exceptions;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Greetings.Abstractions.Services;
using UVOCBot.Plugins.Greetings.Objects;

namespace UVOCBot.Plugins.Greetings.Services;

/// <inheritdoc cref="ICensusQueryService"/>
public class CensusQueryService : ICensusQueryService
{
    private readonly ILogger<CensusQueryService> _logger;
    private readonly IQueryService _queryService;

    public CensusQueryService
    (
        ILogger<CensusQueryService> logger,
        IQueryService queryService
    )
    {
        _logger = logger;
        _queryService = queryService;
    }

    /// <inheritdoc />
    public virtual async Task<Result<IReadOnlyList<NewOutfitMember>>> GetNewOutfitMembersAsync
    (
        ulong outfitId,
        uint limit,
        CancellationToken ct = default
    )
    {
        // https://census.daybreakgames.com/get/ps2/outfit_member?outfit_id=37562651025751157&c:sort=member_since:-1&c:show=character_id,member_since&c:join=character%5Eshow:name.first%5Einject_at:character_name&c:limit=10

        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("outfit_member")
            .Where("outfit_id", SearchModifier.Equals, outfitId)
            .WithSortOrder("member_since", SortOrder.Descending)
            .ShowFields("character_id", "member_since")
            .WithLimit(limit)
            .AddJoin("character", (j) =>
            {
                j.ShowFields("name.first")
                    .InjectAt("character_name");
            });

        return await GetListAsync<NewOutfitMember>(query, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<Result<Outfit?>> GetOutfitAsync(string tag, CancellationToken ct = default)
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("outfit")
            .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

        return await GetAsync<Outfit?>(query, ct).ConfigureAwait(false);
    }

    protected async Task<Result<T?>> GetAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
    {
        try
        {
            return await _queryService.GetAsync<T>(query, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            _logger.LogError(ex, "Census query failed for query {query}.", callerName);
            return ex;
        }
    }

    protected async Task<Result<IReadOnlyList<T>>> GetListAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
    {
        try
        {
            List<T>? result = await _queryService.GetAsync<List<T>>(query, ct).ConfigureAwait(false);

            if (result is null)
                return new CensusException($"Census returned no data for query { callerName }.");
            else
                return result;
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            _logger.LogError(ex, "Census query failed for query {query}.", callerName);
            return ex;
        }
    }
}
