namespace ProjectName.Domain.Common.DomainEvents;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}