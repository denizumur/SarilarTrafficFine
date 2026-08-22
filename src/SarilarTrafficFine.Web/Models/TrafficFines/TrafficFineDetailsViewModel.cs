using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Web.Models.TrafficFines;

public sealed record TrafficFineDetailsViewModel(
    int Id,
    int VehicleId,
    string PlateNumber,
    string VehicleName,
    DateOnly FineDate,
    decimal Amount,
    string? Description,
    TrafficFineStatus Status,
    string StatusText,
    string StatusCssClass,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? CurrentStepName,
    string RowVersion,
    bool CanEdit)
{
    public bool CanSubmit { get; init; }

    public bool CanApproveOrReject { get; init; }

    public string? CurrentStepRequiredRole { get; init; }

    public string? CurrentStepRequiredRoleText { get; init; }

    public string? WorkflowConfigurationError { get; init; }

    public IReadOnlyList<TrafficFineWorkflowStepViewModel>
        WorkflowSteps
    { get; init; } = [];

    public IReadOnlyList<TrafficFineApprovalHistoryViewModel>
        ApprovalHistory
    { get; init; } = [];
}

public sealed record TrafficFineWorkflowStepViewModel(
    int Id,
    int StepOrder,
    string Name,
    string RequiredRole,
    string RequiredRoleText,
    string StateText,
    string StateCssClass);

public sealed record TrafficFineApprovalHistoryViewModel(
    int Id,
    string ActionText,
    string ActionCssClass,
    DateTimeOffset ActionAt,
    string ActionByUserName,
    string? Comment,
    string PreviousState,
    string NewState,
    string? WorkflowStepName);