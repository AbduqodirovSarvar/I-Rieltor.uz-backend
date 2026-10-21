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

        /// <summary>Not a registered HTTP status: the client closed the connection (nginx uses it the same way).</summary>
        private const int ClientClosedRequest = 499;

        /// <summary>
        /// Only our own exception types carry messages meant for clients. Anything else, including
        /// InvalidOperationException/ArgumentException from EF, Npgsql or HttpClient, is a server fault: 500 with a generic text.
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // The client hung up: nobody reads an answer, and it is not a server error.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug("Request {Method} {Path} was cancelled by the client", httpContext.Request.Method, httpContext.Request.Path);
                httpContext.Response.StatusCode = ClientClosedRequest;
                return true;
            }

            var (status, message) = exception switch
            {
                // Not (or no longer) signed in; ForbiddenException is the "signed in, but not allowed" case.
                UnauthorizedAccessException
                    => (HttpStatusCode.Unauthorized, exception.Message),
                ForbiddenException
                    => (HttpStatusCode.Forbidden, exception.Message),
                BadRequestException
                    => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException
                    => (HttpStatusCode.NotFound, exception.Message),
                ConflictException
                    => (HttpStatusCode.Conflict, exception.Message),
                TooManyRequestsException
                    => (HttpStatusCode.TooManyRequests, exception.Message),
                // Request body over Kestrel/form limits (e.g. an upload larger than the configured size).
                BadHttpRequestException bad
                    => ((HttpStatusCode)bad.StatusCode, bad.StatusCode == StatusCodes.Status413PayloadTooLarge ? "The request body is too large." : "The request is malformed."),
                DbUpdateConcurrencyException
                    => (HttpStatusCode.Conflict, "The record was changed or deleted by someone else meanwhile. Reload it and try again."),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                    => (HttpStatusCode.Conflict, "A record with the same unique value already exists."),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } }
                    => (HttpStatusCode.Conflict, "The record is linked to other records and cannot be changed or deleted."),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.StringDataRightTruncation } }
                    => (HttpStatusCode.BadRequest, "A value is too long."),
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
