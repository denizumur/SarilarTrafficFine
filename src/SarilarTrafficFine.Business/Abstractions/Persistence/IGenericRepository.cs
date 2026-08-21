using System.Linq.Expressions;

namespace SarilarTrafficFine.Business.Abstractions.Persistence;

public interface IGenericRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default);

    void Update(T entity);
}