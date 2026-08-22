using Microsoft.EntityFrameworkCore;
using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.Business.Exceptions;
using SarilarTrafficFine.DataAccess.Context;

namespace SarilarTrafficFine.DataAccess.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "Kayýt baþka bir kullanýcý tarafýndan güncellendi.",
                exception);
        }
    }
}