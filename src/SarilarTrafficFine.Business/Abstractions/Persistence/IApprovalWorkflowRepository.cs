using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.Business.Abstractions.Persistence;

public interface IApprovalWorkflowRepository
{
    Task<ApprovalWorkflow?> GetActiveByCodeWithStepsAsync(
        string code,
        CancellationToken cancellationToken = default);
}