using Application.Exceptions;
using Application.Messaging;
using System.ComponentModel.DataAnnotations;

namespace Application.Behaviours
{
    /// <summary>
    /// Checks the DataAnnotations of every mediator request, so the rules on DTOs hold also when a command does not come
    /// through a controller (background jobs, other handlers). Requests from controllers were already validated by
    /// [ApiController] with the same attributes, so for them this never fails. One set of rules, kept on the DTOs.
    /// </summary>
    public sealed class DataAnnotationsValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
                throw new BadRequestException("One or more validation errors occurred.",
                    results.Select(r => $"{string.Join(", ", r.MemberNames)}: {r.ErrorMessage}").ToList());

            return next(cancellationToken);
        }
    }
}
