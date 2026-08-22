using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.Business.Abstractions.Persistence;

public interface IApprovalWorkflowRepository
{
    Task<ApprovalWorkflow?> GetActiveByCodeWithStepsAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<ApprovalWorkflow?> GetByIdWithStepsAsync(
        int id,
        CancellationToken cancellationToken = default);
}