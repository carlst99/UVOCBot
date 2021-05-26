using Newtonsoft.Json;

namespace UVOCBot.Model.Census
{
    public record TranslationProperty
    {
        [JsonProperty("en")]
        public string English { get; init; }

        public TranslationProperty()
        {
            English = string.Empty;
        }
    }
}
