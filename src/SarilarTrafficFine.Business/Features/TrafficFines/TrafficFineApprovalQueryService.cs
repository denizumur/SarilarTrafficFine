using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.Business.Features.TrafficFines.Models;

namespace SarilarTrafficFine.Business.Features.TrafficFines;

public sealed class TrafficFineApprovalQueryService
    : ITrafficFineApprovalQueryService
{
    private readonly ITrafficFineRepository
        _trafficFineRepository;

    private readonly IApprovalWorkflowRepository
        _approvalWorkflowRepository;

    public TrafficFineApprovalQueryService(
        ITrafficFineRepository trafficFineRepository,
        IApprovalWorkflowRepository approvalWorkflowRepository)
    {
        _trafficFineRepository =
            trafficFineRepository;

        _approvalWorkflowRepository =
            approvalWorkflowRepository;
    }

    public async Task<TrafficFineApprovalDetailsDto?> GetAsync(
        int trafficFineId,
        CancellationToken cancellationToken = default)
    {
        var context =
            await _trafficFineRepository
                .GetApprovalContextAsync(
                    trafficFineId,
                    cancellationToken);

        if (context is null)
        {
            return null;
        }

        var historyRecords =
            await _trafficFineRepository
                .GetApprovalHistoryAsync(
                    trafficFineId,
                    cancellationToken);

        var history = historyRecords
            .Select(record =>
                new TrafficFineApprovalHistoryDto(
                    record.Id,
                    record.ActionType,
                    record.ActionAt,
                    record.ActionByUserName,
                    record.Comment,
                    record.PreviousState,
                    record.NewState,
                    record.WorkflowStepId,
                    record.WorkflowStepOrder,
                    record.WorkflowStepName))
            .ToList();

        if (context.ApprovalWorkflowId is null)
        {
            return new TrafficFineApprovalDetailsDto(
                context.TrafficFineId,
                context.Status,
                null,
                context.CurrentApprovalStepId,
                null,
                null,
                [],
                history,
                null);
        }

        var workflow =
            await _approvalWorkflowRepository
                .GetByIdWithStepsAsync(
                    context.ApprovalWorkflowId.Value,
                    cancellationToken);

        if (workflow is null)
        {
            return new TrafficFineApprovalDetailsDto(
                context.TrafficFineId,
                context.Status,
                context.ApprovalWorkflowId,
                context.CurrentApprovalStepId,
                null,
                null,
                [],
                history,
                "Kayýtla iliþkili onay akýþý bulunamadý.");
        }

        var orderedSteps = workflow.Steps
            .OrderBy(step =>
                step.StepOrder)
            .ToList();

        var currentStep =
            context.CurrentApprovalStepId is null
                ? null
                : orderedSteps.SingleOrDefault(
                    step =>
                        step.Id ==
                        context.CurrentApprovalStepId.Value);

        var configurationError =
            context.CurrentApprovalStepId is not null
            && currentStep is null
                ? "Mevcut onay aþamasý workflow tanýmýnda bulunamadý."
                : null;

        var steps = orderedSteps
            .Select(step =>
                new TrafficFineWorkflowStepDto(
                    step.Id,
                    step.StepOrder,
                    step.Name,
                    step.RequiredRole))
            .ToList();

        return new TrafficFineApprovalDetailsDto(
            context.TrafficFineId,
            context.Status,
            context.ApprovalWorkflowId,
            context.CurrentApprovalStepId,
            currentStep?.Name,
            currentStep?.RequiredRole,
            steps,
            history,
            configurationError);
    }
}