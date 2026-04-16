namespace ProjectName.Domain.Common.DomainEvents;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task Handle(T domainEvent, CancellationToken cancellationToken = default);
}