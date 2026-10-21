using Serilog.Context;

namespace WebApi.Middlewares
{
    /// <summary>
    /// One id per request in every log line (CorrelationId) and in the X-Request-ID response header, so a user's
    /// error report can be matched with the server logs. Nginx sends its $request_id, so its access log uses the same id;
    /// without a valid incoming id the ASP.NET Core trace identifier is used.
    /// </summary>
    public sealed class CorrelationIdMiddleware(RequestDelegate next)
    {
        public const string HeaderName = "X-Request-ID";

        public async Task InvokeAsync(HttpContext context)
        {
            var incoming = context.Request.Headers[HeaderName].ToString();
            var id = IsValid(incoming) ? incoming : context.TraceIdentifier;
            context.TraceIdentifier = id;
            context.Response.Headers[HeaderName] = id;

            using (LogContext.PushProperty("CorrelationId", id))
                await next(context);
        }

        // The value goes into logs as is: only short, plain ids are taken from outside.
        private static bool IsValid(string value)
            => value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
    }
}
