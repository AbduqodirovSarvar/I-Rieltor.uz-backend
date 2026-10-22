using Application.Interfaces.Auditing;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;
using System.Security.Claims;

namespace WebApi.Middlewares
{
    /// <summary>
    /// Records every state-changing API request (POST, PUT, PATCH, DELETE) and the downloads of personal documents:
    /// who, when, which action, from where and how it ended. Sits outside the exception handler, so the status is the final
    /// one (a refused request is 403, not a 500 on its way to the handler), and before authentication, so 401s are recorded too.
    /// </summary>
    /// <summary>Remembers which controller action ran, for the audit entry.</summary>
    public sealed class AuditActionFilter : Microsoft.AspNetCore.Mvc.Filters.IActionFilter
    {
        public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
                context.HttpContext.Items[AuditLogMiddleware.ActionItemKey] = $"{descriptor.ControllerTypeInfo.Name}.{descriptor.ActionName}";
        }

        public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
        {
        }
    }

    public sealed class AuditLogMiddleware(RequestDelegate next, IAuditLogQueue queue)
    {
        /// <summary>Set by <see cref="AuditActionFilter"/>: the exception handler clears the endpoint, this survives it.</summary>
        public const string ActionItemKey = "audit.action";

        private static readonly string[] AuditedReadPaths = ["/api/document/download"];

        public async Task InvokeAsync(HttpContext context)
        {
            if (!ShouldAudit(context.Request))
            {
                await next(context);
                return;
            }

            var occurredAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var status = StatusCodes.Status500InternalServerError;   // stays so if the exception escapes every handler
            try
            {
                await next(context);
                status = context.Response.StatusCode;
            }
            finally
            {
                stopwatch.Stop();
                queue.Enqueue(new AuditEntry(
                    occurredAt,
                    UserId(context),
                    Action(context),
                    context.Request.Method,
                    Truncate(context.Request.Path.Value ?? string.Empty, 300),
                    status,
                    Truncate(context.Connection.RemoteIpAddress is { } ip ? (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString() : null, 45),
                    Truncate(context.Request.Headers.UserAgent.ToString(), 300),
                    Truncate(context.TraceIdentifier, 64),
                    (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue)));
            }
        }

        private static bool ShouldAudit(HttpRequest request)
        {
            if (!request.Path.StartsWithSegments("/api"))
                return false;
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method))
                return true;
            return HttpMethods.IsGet(request.Method) && AuditedReadPaths.Any(p => request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private static Guid? UserId(HttpContext context)
            => Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private static string Action(HttpContext context)
        {
            if (context.Items.TryGetValue(ActionItemKey, out var recorded) && recorded is string name)
                return name;
            // Refused before the action ran (401, 403, 429): the endpoint is still known.
            var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            return descriptor == null
                ? Truncate(context.Request.Path.Value ?? string.Empty, 200)!
                : $"{descriptor.ControllerTypeInfo.Name}.{descriptor.ActionName}";
        }

        private static string? Truncate(string? value, int length)
            => value is { Length: > 0 } && value.Length > length ? value[..length] : value;
    }
}
