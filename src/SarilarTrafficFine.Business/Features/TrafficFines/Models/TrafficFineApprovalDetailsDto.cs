using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Features.TrafficFines.Models;

public sealed record TrafficFineWorkflowStepDto(
    int Id,
    int StepOrder,
    string Name,
    string RequiredRole);

public sealed record TrafficFineApprovalHistoryDto(
    int Id,
    ApprovalActionType ActionType,
    DateTimeOffset ActionAt,
    string ActionByUserName,
    string? Comment,
    string PreviousState,
    string NewState,
    int? WorkflowStepId,
    int? WorkflowStepOrder,
    string? WorkflowStepName);

public sealed record TrafficFineApprovalDetailsDto(
    int TrafficFineId,
    TrafficFineStatus Status,
    int? ApprovalWorkflowId,
    int? CurrentApprovalStepId,
    string? CurrentStepName,
    string? CurrentStepRequiredRole,
    IReadOnlyList<TrafficFineWorkflowStepDto> Steps,
    IReadOnlyList<TrafficFineApprovalHistoryDto> History,
    string? ConfigurationError);