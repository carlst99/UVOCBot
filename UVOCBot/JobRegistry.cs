using FluentScheduler;
using UVOCBot.Services;

namespace UVOCBot
{
    public class JobRegistry : Registry
    {
        public JobRegistry()
        {
            Schedule<TwitterService>().ToRunNow().AndEvery(10).Minutes();
        }
    }
}
