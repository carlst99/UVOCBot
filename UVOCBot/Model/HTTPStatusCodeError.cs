using Remora.Results;
using System.Net;

namespace UVOCBot.Model
{
    public record HTTPStatusCodeError : IResultError
    {
        public HttpStatusCode StatusCode { get; }
        public string Message => StatusCode.ToString();

        public HTTPStatusCodeError(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
    }
}
