using Remora.Results;

namespace UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

public record ApexApiError(string Message) : ResultError(Message);
