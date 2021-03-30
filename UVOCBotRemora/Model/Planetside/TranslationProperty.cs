using Newtonsoft.Json;

namespace UVOCBotRemora.Model.Planetside
{
    public class TranslationProperty
    {
        [JsonProperty("en")]
        public string English { get; set; }
    }
}
