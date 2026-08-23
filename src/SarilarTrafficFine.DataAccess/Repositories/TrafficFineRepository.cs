using Microsoft.EntityFrameworkCore;
using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.Business.Abstractions.Persistence.Models;
using SarilarTrafficFine.DataAccess.Context;
using SarilarTrafficFine.Entities.Enums;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Repositories;

public sealed class TrafficFineRepository : ITrafficFineRepository
{
    private readonly AppDbContext _context;

    public TrafficFineRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TrafficFineListRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var query =
            from trafficFine in _context.TrafficFines.AsNoTracking()

            join user in _context.Users.AsNoTracking()
                on trafficFine.CreatedByUserId equals user.Id

            join approvalStep in
                _context.Set<ApprovalWorkflowStep>().AsNoTracking()
                on trafficFine.CurrentApprovalStepId equals approvalStep.Id
                into approvalStepGroup

            from currentStep in approvalStepGroup.DefaultIfEmpty()

            orderby trafficFine.FineDate descending,
                    trafficFine.Id descending

            select new TrafficFineListRecord(
                trafficFine.Id,
                trafficFine.Vehicle.PlateNumber,
                trafficFine.Vehicle.Brand,
                trafficFine.Vehicle.Model,
                trafficFine.FineDate,
                trafficFine.Amount,
                trafficFine.Status,
                user.UserName
                    ?? user.Email
                    ?? trafficFine.CreatedByUserId,
                currentStep == null
                    ? null
                    : currentStep.Name);

        return await query.ToListAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<TrafficFineListRecord>>
        GetPendingForRolesAsync(
            IEnumerable<string> roles,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var normalizedRoles = roles
            .Where(role =>
                !string.IsNullOrWhiteSpace(role))
            .Select(role =>
                role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedRoles.Length == 0)
        {
            return Array.Empty<TrafficFineListRecord>();
        }

        var query =
            from trafficFine in _context.TrafficFines.AsNoTracking()

            join user in _context.Users.AsNoTracking()
                on trafficFine.CreatedByUserId equals user.Id

            join currentStep in
                _context.Set<ApprovalWorkflowStep>().AsNoTracking()
                on trafficFine.CurrentApprovalStepId equals currentStep.Id

            where
                trafficFine.Status ==
                    TrafficFineStatus.InApproval
                && normalizedRoles.Contains(
                    currentStep.RequiredRole)

            orderby
                (trafficFine.UpdatedAt
                    ?? trafficFine.CreatedAt) ascending,
                trafficFine.Id ascending

            select new TrafficFineListRecord(
                trafficFine.Id,
                trafficFine.Vehicle.PlateNumber,
                trafficFine.Vehicle.Brand,
                trafficFine.Vehicle.Model,
                trafficFine.FineDate,
                trafficFine.Amount,
                trafficFine.Status,
                user.UserName
                    ?? user.Email
                    ?? trafficFine.CreatedByUserId,
                currentStep.Name);

        return await query.ToListAsync(
            cancellationToken);
    }

    public async Task<TrafficFineDetailsRecord?> GetDetailsAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        var query =
            from trafficFine in _context.TrafficFines.AsNoTracking()

            join user in _context.Users.AsNoTracking()
                on trafficFine.CreatedByUserId equals user.Id

            join approvalStep in
                _context.Set<ApprovalWorkflowStep>().AsNoTracking()
                on trafficFine.CurrentApprovalStepId equals approvalStep.Id
                into approvalStepGroup

            from currentStep in approvalStepGroup.DefaultIfEmpty()

            where trafficFine.Id == id

            select new TrafficFineDetailsRecord(
                trafficFine.Id,
                trafficFine.VehicleId,
                trafficFine.Vehicle.PlateNumber,
                trafficFine.Vehicle.Brand,
                trafficFine.Vehicle.Model,
                trafficFine.FineDate,
                trafficFine.Amount,
                trafficFine.Description,
                trafficFine.Status,
                trafficFine.CreatedByUserId,
                user.UserName
                    ?? user.Email
                    ?? trafficFine.CreatedByUserId,
                trafficFine.CreatedAt,
                trafficFine.UpdatedAt,
                currentStep == null
                    ? null
                    : currentStep.Name,
                trafficFine.RowVersion);

        return await query.SingleOrDefaultAsync(
            cancellationToken);
    }

    public Task<TrafficFineApprovalContextRecord?>
        GetApprovalContextAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        return _context.TrafficFines
            .AsNoTracking()
            .Where(trafficFine =>
                trafficFine.Id == id)
            .Select(trafficFine =>
                new TrafficFineApprovalContextRecord(
                    trafficFine.Id,
                    trafficFine.Status,
                    trafficFine.ApprovalWorkflowId,
                    trafficFine.CurrentApprovalStepId))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalHistoryRecord>>
        GetApprovalHistoryAsync(
            int trafficFineId,
            CancellationToken cancellationToken = default)
    {
        return await _context
            .Set<ApprovalHistory>()
            .AsNoTracking()
            .Where(history =>
                history.TrafficFineId == trafficFineId)
            .OrderBy(history =>
                history.ActionAt)
            .ThenBy(history =>
                history.Id)
            .Select(history =>
                new ApprovalHistoryRecord(
                    history.Id,
                    history.ActionType,
                    history.ActionAt,
                    history.ActionByUserId,
                    history.ActionByUserName,
                    history.Comment,
                    history.PreviousState,
                    history.NewState,
                    history.WorkflowStepId,
                    history.WorkflowStepOrder,
                    history.WorkflowStepName))
            .ToListAsync(
                cancellationToken);
    }

    public Task<TrafficFine?> GetForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _context.TrafficFines
            .SingleOrDefaultAsync(
                trafficFine =>
                    trafficFine.Id == id,
                cancellationToken);
    }

    public void SetOriginalRowVersion(
        TrafficFine trafficFine,
        byte[] rowVersion)
    {
        _context.Entry(trafficFine)
            .Property(x => x.RowVersion)
            .OriginalValue = rowVersion;
    }
}