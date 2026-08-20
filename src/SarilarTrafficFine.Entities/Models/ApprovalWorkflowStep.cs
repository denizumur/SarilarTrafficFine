namespace SarilarTrafficFine.Entities.Models;

public sealed class ApprovalWorkflowStep
{
    public int Id { get; set; }

    public int ApprovalWorkflowId { get; set; }

    public int StepOrder { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RequiredRole { get; set; } = string.Empty;

    public ApprovalWorkflow ApprovalWorkflow { get; set; } = null!;
}