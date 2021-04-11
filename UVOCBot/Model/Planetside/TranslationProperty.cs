using Newtonsoft.Json;

namespace UVOCBotRemora.Model.Planetside
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
