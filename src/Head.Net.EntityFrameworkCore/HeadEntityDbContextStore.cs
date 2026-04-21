using Head.Net.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Head.Net.EntityFrameworkCore;

/// <summary>
/// Persists Head.Net entities using an EF Core <see cref="DbContext"/>.
/// Supports any key type that implements IComparable for ordering.
/// </summary>
/// <typeparam name="TContext">The EF Core context type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type. Must be non-null, equatable, and comparable.</typeparam>
public sealed class HeadEntityDbContextStore<TContext, TEntity, TKey> : IHeadEntityStore<TEntity, TKey>
    where TContext : DbContext
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>, IComparable<TKey>
{
    private readonly TContext dbContext;
    private readonly DbSet<TEntity> dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadEntityDbContextStore{TContext, TEntity, TKey}"/> class.
    /// </summary>
    public HeadEntityDbContextStore(TContext dbContext)
    {
        this.dbContext = dbContext;
        dbSet = dbContext.Set<TEntity>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbSet
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken)
    {
        return dbSet.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await dbSet.AddAsync(entity, cancellationToken);

        // flush changes
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public async Task<TEntity?> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken)
    {
        var existing = await dbSet.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
        if (existing is null)
        {
            return null;
        }

        dbContext.Entry(existing).CurrentValues.SetValues(entity);
        existing.Id = id;
        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <inheritdoc />
    public async Task<TEntity?> DeleteAsync(TKey id, CancellationToken cancellationToken)
    {
        var existing = await dbSet.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
        if (existing is null)
        {
            return null;
        }

        dbSet.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
