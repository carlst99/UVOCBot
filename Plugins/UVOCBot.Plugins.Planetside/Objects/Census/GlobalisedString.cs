using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.Planetside.Objects.Census
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
