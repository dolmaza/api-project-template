using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.DomainEvents;
using ProjectName.Domain.Common.Mediator.Abstractions;
using ProjectName.Domain.SeedWork;
using ProjectName.Infrastructure.Extensions;

namespace ProjectName.Infrastructure.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator mediator, ICurrentStateService currentStateService) : IdentityDbContext<ApplicationUser>(options), IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public const string PublicSchema = "public";
    public const string ApplicationSchema = "application";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema to application
        modelBuilder.HasDefaultSchema(ApplicationSchema);

        modelBuilder.UseOpenIddict();

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        _currentTransaction ??= await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (ChangeTracker.HasChanges())
            {
                await SaveChangesAsync(cancellationToken);
            }

            if (_currentTransaction != null) await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestampsAndAttachCurrentUserId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch Domain Events collection. 
        // Choices:
        // A) Right BEFORE committing data (EF SaveChanges) into the DB will make a single transaction including  
        // side effects from the domain event handlers which are using the same DbContext with "InstancePerLifetimeScope" or "scoped" lifetime
        // B) Right AFTER committing data (EF SaveChanges) into the DB will make multiple transactions. 
        // You will need to handle eventual consistency and compensatory actions in case of failures in any of the Handlers. 
        await mediator.DispatchDomainEventsAsync(this);

        // After executing this line all the changes (from the Command Handler and Domain Event Handlers) 
        // performed through the DbContext will be committed
        _ = await SaveChangesAsync(cancellationToken);

        return true;
    }

    private void UpdateTimestampsAndAttachCurrentUserId()
    {
        var userId = currentStateService.GetAuthorizedId();
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    {
                        UpdateCreateProperties(userId, entry);
                        UpdateModifiedProperties(userId, entry);
                        break;
                    }
                case EntityState.Modified:
                    {
                        UpdateModifiedProperties(userId, entry);
                        break;
                    }
            }
        }
    }

    private static void UpdateCreateProperties(string userId, EntityEntry entry)
    {
        var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(Entity<>.CreatedAt));
        if (createdAtProperty is { CurrentValue: DateTime createdAt } && createdAt == default)
        {
            createdAtProperty.CurrentValue = DateTime.UtcNow;
        }

        var createdByProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(Entity<>.CreatedBy));
        if (createdByProperty is { CurrentValue: string createdBy } && string.IsNullOrEmpty(createdBy))
        {
            createdByProperty.CurrentValue = userId;
        }
    }

    private static void UpdateModifiedProperties(string userId, EntityEntry entry)
    {
        var modifiedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(Entity<>.ModifiedAt));
        modifiedAtProperty?.CurrentValue = DateTime.UtcNow;

        var modifiedByProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(Entity<>.ModifiedBy));
        modifiedByProperty?.CurrentValue = userId;
    }
}

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        const string connectionString = "Host=postgres;Port=5432;Database=DefaultConnection;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options, new FakeMediator(), new FakeCurrentStateService());
    }

    private class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Publish<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
        {
            throw new NotImplementedException();
        }
    }

    private class FakeCurrentStateService : ICurrentStateService
    {
        public string GetAuthorizedId()
        {
            throw new NotImplementedException();
        }

        public bool IsInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public Guid GetCorrelationIdFromHeader()
        {
            throw new NotImplementedException();
        }
    }
}