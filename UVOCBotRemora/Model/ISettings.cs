using System.Text.Json.Serialization;

namespace UVOCBotRemora.Model
{
    public interface ISettings
    {
        [JsonIgnore]
        ISettings Default { get; }
    }
}
