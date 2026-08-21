using Microsoft.EntityFrameworkCore;
using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.DataAccess.Context;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Repositories;

public sealed class ApprovalWorkflowRepository
    : IApprovalWorkflowRepository
{
    private readonly AppDbContext _context;

    public ApprovalWorkflowRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ApprovalWorkflow?> GetActiveByCodeWithStepsAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return _context.ApprovalWorkflows
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(
                x => x.Code == code && x.IsActive,
                cancellationToken);
    }
}