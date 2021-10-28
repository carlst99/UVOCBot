using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Voice;
using Remora.Results;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Plugins.Music.Commands
{
    public class TestCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordVoiceClientFactory _voiceClientFactory;
        private readonly IVoiceStateCacheService _voiceStateCache;

        public TestCommands
        (
            ICommandContext context,
            DiscordVoiceClientFactory voiceClientFactory,
            IVoiceStateCacheService voiceStateCache
        )
        {
            _context = context;
            _voiceClientFactory = voiceClientFactory;
            _voiceStateCache = voiceStateCache;
        }

        [Command("test")]
        public async Task<Result> TestCommandAsync()
        {
            var userVoiceState = _voiceStateCache.GetUserVoiceState(_context.User.ID);
            if (!userVoiceState.IsDefined() || !userVoiceState.Value.ChannelID.HasValue)
                return new GenericCommandError("You must be in a voice channel to use this command.");

            DiscordVoiceClient client = _voiceClientFactory.Get(_context.GuildID.Value);

            if (!client.IsRunning)
            {
                Result runResult = await client.RunAsync
                (
                    userVoiceState.Value.ChannelID.Value,
                    _context.GuildID.Value,
                    false,
                    true,
                    CancellationToken
                ).ConfigureAwait(false);

                if (!runResult.IsSuccess)
                    return new GenericCommandError("Failed to start: " + runResult.Error.ToString());
            }

            if (client.IsRunning)
            {
                Result stopResult = await client.StopAsync().ConfigureAwait(false);

                if (!stopResult.IsSuccess)
                    return new GenericCommandError("Failed to stop: " + stopResult.Error.ToString());
            }

            return Result.FromSuccess();
        }
    }
}
