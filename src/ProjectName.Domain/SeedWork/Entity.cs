using ProjectName.Domain.Common.DomainEvents;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Domain.SeedWork;

public class Entity<TKey> : HasDomainEvents, IAuditable
{
    public TKey Id { get; set; } = default!;
    public string? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    public DateTime ModifiedAt { get; private set; }
}

public class NoKeyEntity : IAuditable
{
    public string? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    public DateTime ModifiedAt { get; private set; }
}

public class SoftDeletableEntity<TKey> : Entity<TKey>
{
    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkAsDeleted()
    {
        DeletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Validates that the entity has not been softly deleted.
    /// Should be called at the beginning of state-changing operations.
    /// </summary>
    protected Result ValidateNotDeleted(Error alreadyDeletedError)
    {
        if (DeletedAt.HasValue)
        {
            return alreadyDeletedError;
        }

        return Result.Success();
    }
}

public interface IAuditable
{
    public string? CreatedBy { get; }
    public DateTime CreatedAt { get; }
    public string? ModifiedBy { get; }
    public DateTime ModifiedAt { get; }
}