using Microsoft.Extensions.DependencyInjection;
using ProjectName.Domain.Common.DomainEvents;
using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Domain.Common.Mediator;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"Handler for '{request.GetType().Name}' not found.");

        var behaviors = serviceProvider
            .GetServices(typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse)))
            .Cast<object>()
            .Reverse()
            .ToList();

        var handlerDelegate = () =>
            (Task<TResponse>)handlerType
                .GetMethod("Handle")!
                .Invoke(handler, [request, cancellationToken])!;

        foreach (var behavior in behaviors)
        {
            var next = handlerDelegate;
            handlerDelegate = () =>
                (Task<TResponse>)behavior.GetType()
                    .GetMethod("Handle")!
                    .Invoke(behavior, [request, next, cancellationToken])!;
        }

        return await handlerDelegate();
    }

    // Domain event publishing
    public async Task Publish<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("Handle");

            if (method == null) continue;

            var task = (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;

            await task.ConfigureAwait(false);
        }
    }
}