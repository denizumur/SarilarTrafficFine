using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Entities.Models;

public sealed class TrafficFine
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public DateOnly FineDate { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public TrafficFineStatus Status { get; set; }

    public int? ApprovalWorkflowId { get; set; }

    public int? CurrentApprovalStepId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Vehicle Vehicle { get; set; } = null!;

    public ApprovalWorkflow? ApprovalWorkflow { get; set; }

    public ApprovalWorkflowStep? CurrentApprovalStep { get; set; }

    public ICollection<ApprovalHistory> ApprovalHistories { get; set; }
        = new List<ApprovalHistory>();
}