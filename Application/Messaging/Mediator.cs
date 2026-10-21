using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace Application.Messaging
{
    // In-house replacement for MediatR (commercial licence from v13, P3-02): the same names and signatures,
    // so handlers, behaviors and controllers only changed their using. Request/response only, no notifications.

    /// <summary>A request handled by exactly one <see cref="IRequestHandler{TRequest, TResponse}"/>.</summary>
    public interface IRequest<out TResponse>
    {
    }

    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wraps the handler (validation, logging...). Registered as open generics; the first registered runs outermost.
    /// </summary>
    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : notnull
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>For callers that know the request only as object (generic controllers); the result is boxed.</summary>
        Task<object?> Send(object request, CancellationToken cancellationToken = default);
    }

    public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
    {
        // One small typed dispatcher per request type, built once: no reflection on the hot path.
        private static readonly ConcurrentDictionary<Type, object> Dispatchers = new();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var dispatcher = (Dispatcher<TResponse>)Dispatchers.GetOrAdd(request.GetType(),
                type => Activator.CreateInstance(typeof(Dispatcher<,>).MakeGenericType(type, typeof(TResponse)))!);
            return dispatcher.Send(request, serviceProvider, cancellationToken);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var dispatcher = (IUntypedDispatcher)Dispatchers.GetOrAdd(request.GetType(), type =>
            {
                var requestInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    ?? throw new ArgumentException($"{type} does not implement IRequest<TResponse>.", nameof(request));
                return Activator.CreateInstance(typeof(Dispatcher<,>).MakeGenericType(type, requestInterface.GetGenericArguments()[0]))!;
            });
            return dispatcher.SendUntyped(request, serviceProvider, cancellationToken);
        }

        private interface IUntypedDispatcher
        {
            Task<object?> SendUntyped(object request, IServiceProvider services, CancellationToken cancellationToken);
        }

        private abstract class Dispatcher<TResponse> : IUntypedDispatcher
        {
            public abstract Task<TResponse> Send(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken);

            public async Task<object?> SendUntyped(object request, IServiceProvider services, CancellationToken cancellationToken)
                => await Send((IRequest<TResponse>)request, services, cancellationToken);
        }

        private sealed class Dispatcher<TRequest, TResponse> : Dispatcher<TResponse>
            where TRequest : IRequest<TResponse>
        {
            public override Task<TResponse> Send(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken)
            {
                var typed = (TRequest)request;
                var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
                RequestHandlerDelegate<TResponse> next = token => handler.Handle(typed, token);

                foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
                {
                    var inner = next;
                    next = token => behavior.Handle(typed, inner, token);
                }
                return next(cancellationToken);
            }
        }
    }

    public static class MediatorRegistration
    {
        /// <summary>
        /// The mediator, every closed <see cref="IRequestHandler{TRequest, TResponse}"/> in <paramref name="assembly"/>,
        /// and the given open-generic behaviors in order (first = outermost).
        /// </summary>
        public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly, params Type[] openBehaviors)
        {
            services.AddScoped<IMediator, Mediator>();

            var handlers = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .Select(i => (Service: i, Implementation: t)));
            foreach (var (service, implementation) in handlers)
            {
                if (services.Any(d => d.ServiceType == service))
                    throw new InvalidOperationException($"{service} has more than one handler.");
                services.AddScoped(service, implementation);
            }

            foreach (var behavior in openBehaviors)
                services.AddScoped(typeof(IPipelineBehavior<,>), behavior);
            return services;
        }
    }
}
