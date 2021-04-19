﻿using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Planetside;

namespace UVOCBot.Services.Abstractions
{
    public interface ICensusApiService
    {
        /// <summary>
        /// Gets information about a world
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        Task<Result<World>> GetWorld(WorldType world, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of an outfit
        /// </summary>
        /// <param name="outfitTag">The case-insensitive tag of the outfit</param>
        /// <returns></returns>
        Task<Result<OutfitOnlineMembers>> GetOnlineMembersAsync(string outfitTag, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of a number of outfits
        /// </summary>
        /// <param name="outfitTag">The case-insensitive tags of the outfit</param>
        /// <returns></returns>
        Task<Result<IEnumerable<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of a number of outfit
        /// </summary>
        /// <param name="outfitIds">The IDs of the outfits</param>
        /// <returns></returns>
        Task<Result<IEnumerable<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default);
    }
}
