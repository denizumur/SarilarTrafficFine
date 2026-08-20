using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Entities.Models;

public sealed class ApprovalHistory
{
    public int Id { get; set; }

    public int TrafficFineId { get; set; }

    public string ActionByUserId { get; set; } = string.Empty;

    public string ActionByUserName { get; set; } = string.Empty;

    public ApprovalActionType ActionType { get; set; }

    public DateTimeOffset ActionAt { get; set; }

    public string? Comment { get; set; }

    public string PreviousState { get; set; } = string.Empty;

    public string NewState { get; set; } = string.Empty;

    public int? WorkflowStepId { get; set; }

    public int? WorkflowStepOrder { get; set; }

    public string? WorkflowStepName { get; set; }

    public TrafficFine TrafficFine { get; set; } = null!;

    public ApprovalWorkflowStep? WorkflowStep { get; set; }
}