using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery
{
    public record GlobalisedString
    (
        [property: JsonPropertyName("en")] string English
    )
    {
        public override string ToString()
        {
            return English;
        }
    }
}
