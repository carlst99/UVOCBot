using FluentScheduler;
using UVOCBot.Services;

namespace UVOCBot
{
    public class JobRegistry : Registry
    {
        public JobRegistry()
        {
            Schedule<TwitterJob>().ToRunNow().AndEvery(10).Minutes();
        }
    }
}
