using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;

namespace UVOCBot.Utilities
{
    internal class SubscriptionBuilder
    {
        private readonly DiscordContext _dbContext;

        public SubscriptionBuilder(DiscordContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
