using Microsoft.Extensions.Hosting;

namespace UVOCBot.Plugins.Planetside.Workers
{
    public class StartupWorker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken ct)
        {
            // TODO: Register in DI
            // TODO: Populate with metagame events and mapping data from Census query
            // Will need to convert to event stream objects
            throw new NotImplementedException();
        }
    }
}
