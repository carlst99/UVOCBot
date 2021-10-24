using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Voice;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;

namespace UVOCBot.Plugins.Music.Commands
{
    [RequireContext(ChannelContext.Guild)]
    public sealed class MusicCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordVoiceClientFactory _voiceClientFactory;
        private readonly FeedbackService _feedbackService;

        public MusicCommands
        (
            ICommandContext context,
            DiscordVoiceClientFactory voiceClientFactory,
            FeedbackService feedbackService
        )
        {
            _context = context;
            _voiceClientFactory = voiceClientFactory;
            _feedbackService = feedbackService;
        }

        [Command("play")]
        [Description("Plays audio from a YouTube video in the channel you are currently connected to.")]
        public Task<IResult> PlayCommandAsync
        (
            [Description("The URL of the youtube video to play.")] string youtubeUrl
        )
        {
            
        }
    }
}
