using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Responders
{
    public class GuildMemberAddResponder : IResponder<IGuildMemberAdd>
    {
        public Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
