using Head.Net.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Head.Net.EntityFrameworkCore;

/// <summary>
/// Persists Head.Net entities using an EF Core <see cref="DbContext"/>.
/// </summary>
/// <typeparam name="TContext">The EF Core context type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadEntityDbContextStore<TContext, TEntity> : IHeadEntityStore<TEntity>
    where TContext : DbContext
    where TEntity : class, IHeadEntity<int>
{
    private readonly TContext dbContext;
    private readonly DbSet<TEntity> dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadEntityDbContextStore{TContext, TEntity}"/> class.
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
    public Task<TEntity?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await dbSet.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public async Task<TEntity?> UpdateAsync(int id, TEntity entity, CancellationToken cancellationToken)
    {
        var existing = await dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
    public async Task<TEntity?> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var existing = await dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
