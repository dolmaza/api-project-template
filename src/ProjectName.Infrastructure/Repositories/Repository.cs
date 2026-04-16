using ProjectName.Domain.SeedWork;
using ProjectName.Infrastructure.Database;

namespace ProjectName.Infrastructure.Repositories;

public class Repository<TEntity, TKey>(ApplicationDbContext context)
    : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>, IAggregateRoot
{
    protected readonly ApplicationDbContext Context = context;
    protected IQueryable<TEntity> DbItems = context.Set<TEntity>().AsQueryable();

    public IUnitOfWork UnitOfWork => Context;
    
    public virtual async Task<TEntity?> FindByIdAsync(TKey id)
    {
        var isSoftDeletable = typeof(TEntity).IsSubclassOf(typeof(SoftDeletableEntity<TKey>));

        var entity = await Context.FindAsync<TEntity>(id);

        if (isSoftDeletable)
        {
            return entity is SoftDeletableEntity<TKey> { DeletedAt: null } softDeletableEntity ? entity : null;
        }

        return entity;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Remove(TEntity entity)
    {
        var isSoftDeletable = typeof(TEntity).IsSubclassOf(typeof(SoftDeletableEntity<TKey>));

        if (isSoftDeletable)
        {
            var softDeletableEntity = entity as SoftDeletableEntity<TKey>;
            softDeletableEntity?.MarkAsDeleted();
            Update(entity);
        }
        else
        {
            Context.Set<TEntity>().Remove(entity);
        }
    }

    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }
}