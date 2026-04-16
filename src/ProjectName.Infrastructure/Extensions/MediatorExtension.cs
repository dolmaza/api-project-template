using ProjectName.Domain.Common.DomainEvents;
using ProjectName.Domain.Common.Mediator.Abstractions;
using ProjectName.Infrastructure.Database;

namespace ProjectName.Infrastructure.Extensions;

public static class MediatorExtension
{
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, ApplicationDbContext context)
    {
        var domainEntities = context.ChangeTracker
            .Entries<HasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Any()).ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents!)
            .ToList();

        domainEntities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }
}