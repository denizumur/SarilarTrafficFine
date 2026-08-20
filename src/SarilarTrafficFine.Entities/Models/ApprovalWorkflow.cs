namespace SarilarTrafficFine.Entities.Models;

public sealed class ApprovalWorkflow
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ApprovalWorkflowStep> Steps { get; set; }
        = new List<ApprovalWorkflowStep>();
}