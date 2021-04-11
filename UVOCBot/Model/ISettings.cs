using System.Text.Json.Serialization;

namespace UVOCBot.Model
{
    public interface ISettings
    {
        [JsonIgnore]
        ISettings Default { get; }
    }
}
