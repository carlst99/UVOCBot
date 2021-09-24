﻿using DbgCensus.Core.Objects;
using System;

namespace UVOCBot.Model.EventStream
{
    public class PlayerFacilityCapture
    {
        public ulong CharacterID { get; set; }
        public string EventName { get; set; }
        public uint FacilityID { get; set; }
        public ulong OutfitID { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public WorldDefinition WorldID { get; set; }
        public ZoneDefinition ZoneID { get; set; }

        public PlayerFacilityCapture()
        {
            EventName = "PlayerFacilityCapture";
        }
    }
}
