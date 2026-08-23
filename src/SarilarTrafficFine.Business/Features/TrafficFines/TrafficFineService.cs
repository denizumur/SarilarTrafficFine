using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.Business.Abstractions.Persistence.Models;
using SarilarTrafficFine.Business.Constants;
using SarilarTrafficFine.Business.Exceptions;
using SarilarTrafficFine.Business.Features.TrafficFines.Models;
using SarilarTrafficFine.Business.Security;
using SarilarTrafficFine.Entities.Enums;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.Business.Features.TrafficFines;

public sealed class TrafficFineService : ITrafficFineService
{
    private const int DescriptionMaxLength = 1000;
    private const int RejectReasonMaxLength = 1000;

    private readonly ITrafficFineRepository _trafficFineRepository;

    private readonly IApprovalWorkflowRepository
        _approvalWorkflowRepository;

    private readonly IGenericRepository<TrafficFine>
        _genericTrafficFineRepository;

    private readonly IGenericRepository<ApprovalHistory>
        _approvalHistoryRepository;

    private readonly IGenericRepository<Vehicle>
        _vehicleRepository;

    private readonly IUnitOfWork _unitOfWork;

    public TrafficFineService(
        ITrafficFineRepository trafficFineRepository,
        IApprovalWorkflowRepository approvalWorkflowRepository,
        IGenericRepository<TrafficFine> genericTrafficFineRepository,
        IGenericRepository<ApprovalHistory> approvalHistoryRepository,
        IGenericRepository<Vehicle> vehicleRepository,
        IUnitOfWork unitOfWork)
    {
        _trafficFineRepository =
            trafficFineRepository;

        _approvalWorkflowRepository =
            approvalWorkflowRepository;

        _genericTrafficFineRepository =
            genericTrafficFineRepository;

        _approvalHistoryRepository =
            approvalHistoryRepository;

        _vehicleRepository =
            vehicleRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<IReadOnlyList<TrafficFineListItemDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records =
            await _trafficFineRepository.ListAsync(
                cancellationToken);

        return MapListItems(
            records);
    }

    public async Task<IReadOnlyList<TrafficFineListItemDto>>
        GetPendingApprovalsAsync(
            CurrentUserContext currentUser,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            currentUser);

        var records =
            await _trafficFineRepository
                .GetPendingForRolesAsync(
                    currentUser.Roles,
                    cancellationToken);

        return MapListItems(
            records);
    }

    public async Task<TrafficFineDetailsDto?> GetDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record =
            await _trafficFineRepository.GetDetailsAsync(
                id,
                cancellationToken);

        if (record is null)
        {
            return null;
        }

        return new TrafficFineDetailsDto(
            record.Id,
            record.VehicleId,
            record.PlateNumber,
            record.Brand,
            record.Model,
            record.FineDate,
            record.Amount,
            record.Description,
            record.Status,
            record.CreatedByUserId,
            record.CreatedByUserName,
            record.CreatedAt,
            record.UpdatedAt,
            record.CurrentStepName,
            record.RowVersion);
    }

    public async Task<TrafficFineCommandResult> CreateAsync(
        TrafficFineCreateRequest request,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            currentUser);

        var validation =
            await ValidateFineDataAsync(
                request.VehicleId,
                request.FineDate,
                request.Amount,
                request.Description,
                cancellationToken);

        if (validation is not null)
        {
            return validation;
        }

        var trafficFine =
            new TrafficFine
            {
                VehicleId =
                    request.VehicleId,

                FineDate =
                    request.FineDate,

                Amount =
                    request.Amount,

                Description =
                    NormalizeOptionalText(
                        request.Description),

                Status =
                    TrafficFineStatus.New,

                CreatedByUserId =
                    currentUser.UserId,

                CreatedAt =
                    DateTimeOffset.UtcNow
            };

        await _genericTrafficFineRepository.AddAsync(
            trafficFine,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return TrafficFineCommandResult.Success(
            trafficFine.Id);
    }

    public async Task<TrafficFineCommandResult> EditAsync(
        TrafficFineEditRequest request,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            currentUser);

        var trafficFine =
            await _trafficFineRepository
                .GetForUpdateAsync(
                    request.Id,
                    cancellationToken);

        if (trafficFine is null)
        {
            return Failure(
                TrafficFineCommandError.NotFound,
                "Trafik cezasý bulunamadý.");
        }

        if (!string.Equals(
                trafficFine.CreatedByUserId,
                currentUser.UserId,
                StringComparison.Ordinal))
        {
            return Failure(
                TrafficFineCommandError.Forbidden,
                "Yalnýzca kendi oluþturduðunuz trafik cezasýný düzenleyebilirsiniz.");
        }

        if (trafficFine.Status !=
            TrafficFineStatus.New)
        {
            return Failure(
                TrafficFineCommandError.InvalidState,
                "Yalnýzca Yeni durumundaki trafik cezalarý düzenlenebilir.");
        }

        var validation =
            await ValidateFineDataAsync(
                request.VehicleId,
                request.FineDate,
                request.Amount,
                request.Description,
                cancellationToken);

        if (validation is not null)
        {
            return validation;
        }

        if (request.ExpectedRowVersion.Length == 0)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                "Kayýt sürüm bilgisi geçersiz.");
        }

        _trafficFineRepository.SetOriginalRowVersion(
            trafficFine,
            request.ExpectedRowVersion);

        trafficFine.VehicleId =
            request.VehicleId;

        trafficFine.FineDate =
            request.FineDate;

        trafficFine.Amount =
            request.Amount;

        trafficFine.Description =
            NormalizeOptionalText(
                request.Description);

        trafficFine.UpdatedAt =
            DateTimeOffset.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return ConcurrencyFailure();
        }

        return TrafficFineCommandResult.Success(
            trafficFine.Id);
    }

    public async Task<TrafficFineCommandResult> SubmitAsync(
        int id,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            currentUser);

        var trafficFine =
            await _trafficFineRepository
                .GetForUpdateAsync(
                    id,
                    cancellationToken);

        if (trafficFine is null)
        {
            return Failure(
                TrafficFineCommandError.NotFound,
                "Trafik cezasý bulunamadý.");
        }

        if (!string.Equals(
                trafficFine.CreatedByUserId,
                currentUser.UserId,
                StringComparison.Ordinal))
        {
            return Failure(
                TrafficFineCommandError.Forbidden,
                "Yalnýzca kendi oluþturduðunuz trafik cezasýný onaya gönderebilirsiniz.");
        }

        if (trafficFine.Status !=
            TrafficFineStatus.New)
        {
            return Failure(
                TrafficFineCommandError.InvalidState,
                "Yalnýzca Yeni durumundaki trafik cezalarý onaya gönderilebilir.");
        }

        var workflow =
            await _approvalWorkflowRepository
                .GetActiveByCodeWithStepsAsync(
                    ApprovalWorkflowCodes.TrafficFine,
                    cancellationToken);

        if (workflow is null)
        {
            return ConfigurationFailure(
                "Aktif trafik cezasý onay akýþý bulunamadý.");
        }

        var firstStep =
            workflow.Steps
                .OrderBy(step =>
                    step.StepOrder)
                .FirstOrDefault();

        if (firstStep is null)
        {
            return ConfigurationFailure(
                "Trafik cezasý onay akýþýnda tanýmlý aþama bulunamadý.");
        }

        var previousState =
            BuildStateSnapshot(
                TrafficFineStatus.New,
                null);

        var now =
            DateTimeOffset.UtcNow;

        trafficFine.Status =
            TrafficFineStatus.InApproval;

        trafficFine.ApprovalWorkflowId =
            workflow.Id;

        trafficFine.CurrentApprovalStepId =
            firstStep.Id;

        trafficFine.UpdatedAt =
            now;

        var newState =
            BuildStateSnapshot(
                TrafficFineStatus.InApproval,
                firstStep.Name);

        await AddHistoryAsync(
            trafficFine,
            currentUser,
            ApprovalActionType.Submitted,
            now,
            "Kayýt onay akýþýna gönderildi.",
            previousState,
            newState,
            firstStep,
            cancellationToken);

        return await SaveTransitionAsync(
            trafficFine.Id,
            cancellationToken);
    }

    public async Task<TrafficFineCommandResult> ApproveAsync(
        int id,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            currentUser);

        var preparation =
            await PrepareApprovalActionAsync(
                id,
                currentUser,
                cancellationToken);

        if (preparation.Error is not null)
        {
            return preparation.Error;
        }

        var trafficFine =
            preparation.TrafficFine!;

        var workflow =
            preparation.Workflow!;

        var currentStep =
            preparation.CurrentStep!;

        var previousState =
            BuildStateSnapshot(
                TrafficFineStatus.InApproval,
                currentStep.Name);

        var nextStep =
            workflow.Steps
                .Where(step =>
                    step.StepOrder >
                    currentStep.StepOrder)
                .OrderBy(step =>
                    step.StepOrder)
                .FirstOrDefault();

        var now =
            DateTimeOffset.UtcNow;

        string newState;

        if (nextStep is null)
        {
            trafficFine.Status =
                TrafficFineStatus.Completed;

            trafficFine.CurrentApprovalStepId =
                null;

            newState =
                BuildStateSnapshot(
                    TrafficFineStatus.Completed,
                    null);
        }
        else
        {
            trafficFine.Status =
                TrafficFineStatus.InApproval;

            trafficFine.CurrentApprovalStepId =
                nextStep.Id;

            newState =
                BuildStateSnapshot(
                    TrafficFineStatus.InApproval,
                    nextStep.Name);
        }

        trafficFine.UpdatedAt =
            now;

        await AddHistoryAsync(
            trafficFine,
            currentUser,
            ApprovalActionType.Approved,
            now,
            "Onaylandý.",
            previousState,
            newState,
            currentStep,
            cancellationToken);

        return await SaveTransitionAsync(
            trafficFine.Id,
            cancellationToken);
    }

    public async Task<TrafficFineCommandResult> RejectAsync(
        int id,
        string reason,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            currentUser);

        var normalizedReason =
            NormalizeOptionalText(
                reason);

        if (normalizedReason is null)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                "Ret nedeni zorunludur.",
                "Reason");
        }

        if (normalizedReason.Length >
            RejectReasonMaxLength)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                $"Ret nedeni en fazla {RejectReasonMaxLength} karakter olabilir.",
                "Reason");
        }

        var preparation =
            await PrepareApprovalActionAsync(
                id,
                currentUser,
                cancellationToken);

        if (preparation.Error is not null)
        {
            return preparation.Error;
        }

        var trafficFine =
            preparation.TrafficFine!;

        var currentStep =
            preparation.CurrentStep!;

        var previousState =
            BuildStateSnapshot(
                TrafficFineStatus.InApproval,
                currentStep.Name);

        var newState =
            BuildStateSnapshot(
                TrafficFineStatus.Rejected,
                null);

        var now =
            DateTimeOffset.UtcNow;

        trafficFine.Status =
            TrafficFineStatus.Rejected;

        trafficFine.CurrentApprovalStepId =
            null;

        trafficFine.UpdatedAt =
            now;

        await AddHistoryAsync(
            trafficFine,
            currentUser,
            ApprovalActionType.Rejected,
            now,
            normalizedReason,
            previousState,
            newState,
            currentStep,
            cancellationToken);

        return await SaveTransitionAsync(
            trafficFine.Id,
            cancellationToken);
    }

    private async Task<ApprovalPreparation>
        PrepareApprovalActionAsync(
            int id,
            CurrentUserContext currentUser,
            CancellationToken cancellationToken)
    {
        var trafficFine =
            await _trafficFineRepository
                .GetForUpdateAsync(
                    id,
                    cancellationToken);

        if (trafficFine is null)
        {
            return ApprovalPreparation.Failed(
                Failure(
                    TrafficFineCommandError.NotFound,
                    "Trafik cezasý bulunamadý."));
        }

        if (trafficFine.Status !=
            TrafficFineStatus.InApproval)
        {
            return ApprovalPreparation.Failed(
                Failure(
                    TrafficFineCommandError.InvalidState,
                    "Yalnýzca onay sürecindeki trafik cezalarýnda bu iþlem yapýlabilir."));
        }

        if (trafficFine.ApprovalWorkflowId is null)
        {
            return ApprovalPreparation.Failed(
                ConfigurationFailure(
                    "Trafik cezasýna baðlý onay akýþý bulunamadý."));
        }

        if (trafficFine.CurrentApprovalStepId is null)
        {
            return ApprovalPreparation.Failed(
                ConfigurationFailure(
                    "Trafik cezasýnýn mevcut onay aþamasý bulunamadý."));
        }

        var workflow =
            await _approvalWorkflowRepository
                .GetByIdWithStepsAsync(
                    trafficFine.ApprovalWorkflowId.Value,
                    cancellationToken);

        if (workflow is null)
        {
            return ApprovalPreparation.Failed(
                ConfigurationFailure(
                    "Trafik cezasýna baðlý onay akýþý bulunamadý."));
        }

        var currentStep =
            workflow.Steps.SingleOrDefault(
                step =>
                    step.Id ==
                    trafficFine.CurrentApprovalStepId.Value);

        if (currentStep is null)
        {
            return ApprovalPreparation.Failed(
                ConfigurationFailure(
                    "Mevcut onay aþamasý workflow tanýmýnda bulunamadý."));
        }

        if (!currentUser.IsInRole(
                currentStep.RequiredRole))
        {
            return ApprovalPreparation.Failed(
                Failure(
                    TrafficFineCommandError.Forbidden,
                    "Bu onay aþamasý için yetkiniz bulunmuyor."));
        }

        if (string.Equals(
                trafficFine.CreatedByUserId,
                currentUser.UserId,
                StringComparison.Ordinal))
        {
            return ApprovalPreparation.Failed(
                Failure(
                    TrafficFineCommandError.Forbidden,
                    "Kendi oluþturduðunuz kaydý onaylayamaz veya reddedemezsiniz."));
        }

        return ApprovalPreparation.Success(
            trafficFine,
            workflow,
            currentStep);
    }

    private async Task AddHistoryAsync(
        TrafficFine trafficFine,
        CurrentUserContext currentUser,
        ApprovalActionType actionType,
        DateTimeOffset actionAt,
        string? comment,
        string previousState,
        string newState,
        ApprovalWorkflowStep? workflowStep,
        CancellationToken cancellationToken)
    {
        var history =
            new ApprovalHistory
            {
                TrafficFineId =
                    trafficFine.Id,

                ActionByUserId =
                    currentUser.UserId,

                ActionByUserName =
                    currentUser.UserName,

                ActionType =
                    actionType,

                ActionAt =
                    actionAt,

                Comment =
                    comment,

                PreviousState =
                    previousState,

                NewState =
                    newState,

                WorkflowStepId =
                    workflowStep?.Id,

                WorkflowStepOrder =
                    workflowStep?.StepOrder,

                WorkflowStepName =
                    workflowStep?.Name
            };

        await _approvalHistoryRepository.AddAsync(
            history,
            cancellationToken);
    }

    private async Task<TrafficFineCommandResult>
        SaveTransitionAsync(
            int trafficFineId,
            CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return TrafficFineCommandResult.Success(
                trafficFineId);
        }
        catch (ConcurrencyConflictException)
        {
            return ConcurrencyFailure();
        }
    }

    private async Task<TrafficFineCommandResult?>
        ValidateFineDataAsync(
            int vehicleId,
            DateOnly fineDate,
            decimal amount,
            string? description,
            CancellationToken cancellationToken)
    {
        var today =
            DateOnly.FromDateTime(
                DateTime.Now);

        if (fineDate > today)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                "Ceza tarihi gelecek bir tarih olamaz.",
                "FineDate");
        }

        if (vehicleId <= 0)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                "Geçerli bir araç seçiniz.",
                "VehicleId");
        }

        var vehicleExists =
            await _vehicleRepository.AnyAsync(
                vehicle =>
                    vehicle.Id == vehicleId
                    && vehicle.IsActive,
                cancellationToken);

        if (!vehicleExists)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                "Seçilen araç bulunamadý veya aktif deðil.",
                "VehicleId");
        }

        if (amount <= 0)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                "Ceza tutarý sýfýrdan büyük olmalýdýr.",
                "Amount");
        }

        var normalizedDescription =
            NormalizeOptionalText(
                description);

        if (normalizedDescription?.Length >
            DescriptionMaxLength)
        {
            return Failure(
                TrafficFineCommandError.Validation,
                $"Açýklama en fazla {DescriptionMaxLength} karakter olabilir.",
                "Description");
        }

        return null;
    }

    private static IReadOnlyList<TrafficFineListItemDto>
        MapListItems(
            IEnumerable<TrafficFineListRecord> records)
    {
        return records
            .Select(record =>
                new TrafficFineListItemDto(
                    record.Id,
                    record.PlateNumber,
                    $"{record.Brand} {record.Model}",
                    record.FineDate,
                    record.Amount,
                    record.Status,
                    record.CreatedByUserName,
                    record.CurrentStepName))
            .ToList();
    }

    private static string BuildStateSnapshot(
        TrafficFineStatus status,
        string? stepName)
    {
        return status switch
        {
            TrafficFineStatus.New =>
                "Yeni",

            TrafficFineStatus.InApproval
                when !string.IsNullOrWhiteSpace(
                    stepName) =>
                $"Onayda · {stepName}",

            TrafficFineStatus.InApproval =>
                "Onayda",

            TrafficFineStatus.Completed =>
                "Tamamlandý",

            TrafficFineStatus.Rejected =>
                "Reddedildi",

            _ =>
                status.ToString()
        };
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
            value)
            ? null
            : value.Trim();
    }

    private static TrafficFineCommandResult Failure(
        TrafficFineCommandError error,
        string message,
        string? field = null)
    {
        return TrafficFineCommandResult.Failure(
            error,
            message,
            field);
    }

    private static TrafficFineCommandResult
        ConfigurationFailure(
            string message)
    {
        return Failure(
            TrafficFineCommandError.Configuration,
            message);
    }

    private static TrafficFineCommandResult
        ConcurrencyFailure()
    {
        return Failure(
            TrafficFineCommandError.ConcurrencyConflict,
            "Bu kayýt sizden önce baþka bir kullanýcý tarafýndan güncellendi.");
    }

    private sealed record ApprovalPreparation(
        TrafficFine? TrafficFine,
        ApprovalWorkflow? Workflow,
        ApprovalWorkflowStep? CurrentStep,
        TrafficFineCommandResult? Error)
    {
        public static ApprovalPreparation Success(
            TrafficFine trafficFine,
            ApprovalWorkflow workflow,
            ApprovalWorkflowStep currentStep)
        {
            return new(
                trafficFine,
                workflow,
                currentStep,
                null);
        }

        public static ApprovalPreparation Failed(
            TrafficFineCommandResult error)
        {
            return new(
                null,
                null,
                null,
                error);
        }
    }
}