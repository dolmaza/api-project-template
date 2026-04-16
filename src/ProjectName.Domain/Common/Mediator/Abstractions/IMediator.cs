using ProjectName.Domain.Common.DomainEvents;

namespace ProjectName.Domain.Common.Mediator.Abstractions;

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task Publish<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}