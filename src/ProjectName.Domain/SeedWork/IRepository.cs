namespace ProjectName.Domain.SeedWork;

public interface IRepository<TEntity, in TKey> where TEntity : Entity<TKey>, IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
    
    Task<TEntity?> FindByIdAsync(TKey id);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}