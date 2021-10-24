using Remora.Results;

namespace UVOCBot.Plugins.Music.Errors
{
    /// <summary>
    /// Represents an error with retrieving YouTube content, as a result of a user action.
    /// </summary>
    /// <param name="Message">The error message.</param>
    public record YouTubeUserError(string Message)
        : ResultError(Message);
}
