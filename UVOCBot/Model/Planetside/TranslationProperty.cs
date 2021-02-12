using Newtonsoft.Json;

namespace UVOCBot.Model.Planetside
{
    public class TranslationProperty
    {
        [JsonProperty("en")]
        public string English { get; set; }
    }
}
