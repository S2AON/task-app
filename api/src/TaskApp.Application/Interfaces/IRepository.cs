using System.Linq.Expressions;
using TaskApp.Domain.Common;

namespace TaskApp.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    );

    Task<IQueryable<T>> AsQueryable(CancellationToken cancellationToken = default);

    Task<T?> GetByIdIncludingDeletedAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetAllAsync(
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes
    );

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        CancellationToken cancellationToken = default
    );

    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    );
}
