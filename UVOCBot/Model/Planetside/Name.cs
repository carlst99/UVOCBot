﻿namespace UVOCBot.Model.Planetside
{
    public record Name
    {
        public string First { get; init; }
        public string Last { get; init; }

        public Name()
        {
            First = string.Empty;
            Last = string.Empty;
        }
    }
}
