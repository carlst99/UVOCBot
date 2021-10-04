using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery
{
    public record GlobalisedString
    {
        [JsonPropertyName("en")]
        public string English { get; init; }

        public GlobalisedString()
        {
            English = string.Empty;
        }

        public override string ToString()
            => English;
    }
}
