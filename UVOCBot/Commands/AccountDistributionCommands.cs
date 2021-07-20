using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Threading.Tasks;
using UVOCBot.Services;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("accounts")]
    public class AccountDistributionCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly ReplyService _responder;
        private readonly IDbApiService _dbApi;

        public AccountDistributionCommands(ICommandContext context, ReplyService responder, IDbApiService dbApi)
        {
            _context = context;
            _responder = responder;
            _dbApi = dbApi;
        }

        [Command("distribute-to-role")]
        public async Task<IResult> DistributeToRoleCommandAsync(IRole role, Optional<IChannel> channel = default)
        {
            if (!channel.HasValue)
            {

            }

            throw new NotImplementedException();
        }

        [Command("distribute-to-group")]
        public async Task<IResult> DistributeToGroupCommandAsync(string groupName, Optional<IChannel> channel = default)
        {
            throw new NotImplementedException();
        }
    }
}
