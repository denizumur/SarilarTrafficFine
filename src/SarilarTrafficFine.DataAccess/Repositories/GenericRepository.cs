using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.DataAccess.Context;

namespace SarilarTrafficFine.DataAccess.Repositories;

public sealed class GenericRepository<T>
    : IGenericRepository<T>
    where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(
            [id],
            cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(
            entity,
            cancellationToken);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }
}