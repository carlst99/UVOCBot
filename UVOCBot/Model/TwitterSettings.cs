using DSharpPlus.Entities;
using Realms;
using System;
using System.Collections.Generic;
using System.Text;

namespace UVOCBot.Model
{
    public class TwitterSettings : RealmObject
    {
        public GuildSettings Guild { get; set; }
        public DiscordChannel BroadcastChannel { get; set; }

    }
}
