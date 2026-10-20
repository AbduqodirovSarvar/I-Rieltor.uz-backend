using Application.Exceptions;
using Application.Models.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;

namespace WebApi.Handlers
{
    /// <summary>
    /// Turns exceptions thrown by handlers and services into a <see cref="ResponseModel{TEntity}"/>
    /// with a matching HTTP status. Unexpected errors are logged and returned without details.
    /// </summary>
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger = logger;

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, message) = exception switch
            {
                // Signed-in users lacking a permission get 403; anonymous callers and failed sign-ins get 401.
                UnauthorizedAccessException when httpContext.User.Identity?.IsAuthenticated == true
                    => (HttpStatusCode.Forbidden, exception.Message),
                UnauthorizedAccessException
                    => (HttpStatusCode.Unauthorized, exception.Message),
                KeyNotFoundException
                    => (HttpStatusCode.NotFound, exception.Message),
                ConflictException
                    => (HttpStatusCode.Conflict, exception.Message),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                    => (HttpStatusCode.Conflict, "A record with the same unique value already exists."),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } }
                    => (HttpStatusCode.Conflict, "The record is linked to other records and cannot be changed or deleted."),
                ArgumentException or FormatException or InvalidOperationException
                    => (HttpStatusCode.BadRequest, exception.Message),
                NotImplementedException
                    => (HttpStatusCode.NotImplemented, "This operation is not implemented yet."),
                ServiceUnavailableException
                    => (HttpStatusCode.ServiceUnavailable, exception.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            else
                _logger.LogInformation("{Status} for {Method} {Path}: {Message}", (int)status, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

            httpContext.Response.StatusCode = (int)status;
            await httpContext.Response.WriteAsJsonAsync(ResponseModel<object>.Fail(message, statusCode: status), cancellationToken);
            return true;
        }
    }
}
