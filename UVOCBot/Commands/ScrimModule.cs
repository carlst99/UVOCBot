using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Group("scrim")]
    [Description("Commands pertinent to scrim organisation")]
    [RequireGuild]
    public class ScrimModule : BaseCommandModule
    {
    }
}
