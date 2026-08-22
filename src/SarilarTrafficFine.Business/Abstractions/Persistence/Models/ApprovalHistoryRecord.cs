using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Abstractions.Persistence.Models;

public sealed record ApprovalHistoryRecord(
    int Id,
    ApprovalActionType ActionType,
    DateTimeOffset ActionAt,
    string ActionByUserId,
    string ActionByUserName,
    string? Comment,
    string PreviousState,
    string NewState,
    int? WorkflowStepId,
    int? WorkflowStepOrder,
    string? WorkflowStepName);