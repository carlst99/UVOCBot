using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Group("planetside")]
    [Aliases("ps2", "ps")]
    [Description("Commands that provide information about PlanetSide 2")]
    public class PlanetsideModule : BaseCommandModule
    {
        [Command("random-teams")]
        [Aliases("rt", "random")]
        [Description("Generates any number of random teams from members with a particular role")]
        public async Task GetContinentStatus()
        {

        }
    }
}
