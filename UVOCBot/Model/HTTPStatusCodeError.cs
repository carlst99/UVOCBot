using Remora.Results;
using System.Net;

namespace UVOCBot.Model
{
    public record HttpStatusCodeError : IResultError
    {
        public HttpStatusCode StatusCode { get; }
        public string Message => StatusCode.ToString();

        public HttpStatusCodeError(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
    }
}
