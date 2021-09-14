using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Model.Census;
using UVOCBot.Model.EventStream;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders.Census
{
    public class FacilityControlResponder : ICensusEventHandler<ServiceMessage<FacilityControl>>
    {
        private readonly DiscordContext _dbContext;
        private readonly ICensusApiService _censusApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public FacilityControlResponder(
            DiscordContext dbContext,
            ICensusApiService censusApi,
            IDiscordRestChannelAPI channelApi)
        {
            _dbContext = dbContext;
            _censusApi = censusApi;
            _channelApi = channelApi;
        }

        public async Task HandleAsync(ServiceMessage<FacilityControl> censusEvent, CancellationToken ct = default)
        {
            Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(censusEvent.Payload.OutfitId, ct).ConfigureAwait(false);
            if (!getOutfitResult.IsDefined())
                return;
            Outfit outfit = getOutfitResult.Entity;

            Result<MapRegion?> getFacilityResult = await _censusApi.GetFacilityRegionAsync(censusEvent.Payload.FacilityId, ct).ConfigureAwait(false);
            if (!getFacilityResult.IsDefined())
                return;
            MapRegion facility = getFacilityResult.Entity;

            foreach (PlanetsideSettings pSettings in _dbContext.PlanetsideSettings)
            {
                if (pSettings.BaseCaptureChannelId is null || !pSettings.TrackedOutfits.Contains(outfit.OutfitId))
                    continue;

                Embed embed = new(
                    "Base Captured",
                    Description: $"{ Formatter.Bold($"[{ outfit.Alias }]") } has captured { Formatter.Italic(facility.FacilityName) }",
                    Colour: BotConstants.DEFAULT_EMBED_COLOUR);

                await _channelApi.CreateMessageAsync(new Snowflake(pSettings.BaseCaptureChannelId.Value), embeds: new IEmbed[] { embed }, ct: ct).ConfigureAwait(false);
            }
        }
    }
}
