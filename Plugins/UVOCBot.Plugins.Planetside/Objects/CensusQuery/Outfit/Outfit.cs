﻿namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit
{
    public record Outfit
    {
        public ulong OutfitId { get; init; }
        public string Name { get; init; }
        public string NameLower { get; init; }
        public string Alias { get; init; }
        public string AliasLower { get; init; }
        public DateTimeOffset TimeCreated { get; init; }
        public ulong LeaderCharacterId { get; init; }
        public uint MemberCount { get; init; }

        public Outfit()
        {
            Name = string.Empty;
            NameLower = string.Empty;
            Alias = string.Empty;
            AliasLower = string.Empty;
        }
    }
}
