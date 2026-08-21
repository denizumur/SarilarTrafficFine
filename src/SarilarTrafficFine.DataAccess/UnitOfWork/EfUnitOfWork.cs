using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.DataAccess.Context;

namespace SarilarTrafficFine.DataAccess.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}