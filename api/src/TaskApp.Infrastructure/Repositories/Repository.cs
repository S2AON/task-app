using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Common;
using TaskApp.Infrastructure.Data;

namespace TaskApp.Infrastructure.Repositories;

public sealed class Repository<T>(ApplicationDbContext context) : IRepository<T>
    where T : BaseEntity
{
    private readonly ApplicationDbContext _context = context;
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken
        );
    }

    public async Task<T?> GetByIdAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbSet.AsQueryable();

        query = includeFunc(query);

        return await query.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken
        );
    }

    public Task<IQueryable<T>> AsQueryable(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_dbSet.AsQueryable());
    }

    public async Task<T?> GetByIdIncludingDeletedAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbSet.IgnoreQueryFilters();
        query = includeFunc(query);

        return await query.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken
        );
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbSet.AsNoTracking();

        query = includeFunc(query);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes
    )
    {
        var query = _dbSet.Where(x => !x.IsDeleted);

        // Aplicar eager loading de relaciones
        query = includes.Aggregate(query,
            (current, include) => current.Include(include)
        );

        return await query
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbSet.AsQueryable();

        query = includeFunc(query);

        if (predicate != null)
            return await query
                .Where(predicate)
                .ToListAsync(cancellationToken);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.MarkAsDeleted();
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            x => x.Id == id && !x.IsDeleted,
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}
