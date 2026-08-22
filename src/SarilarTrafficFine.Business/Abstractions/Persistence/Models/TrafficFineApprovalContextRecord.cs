using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Abstractions.Persistence.Models;

public sealed record TrafficFineApprovalContextRecord(
    int TrafficFineId,
    TrafficFineStatus Status,
    int? ApprovalWorkflowId,
    int? CurrentApprovalStepId);