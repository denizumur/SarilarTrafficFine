using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarilarTrafficFine.Business.Constants;
using SarilarTrafficFine.Business.Features.TrafficFines;
using SarilarTrafficFine.Business.Features.TrafficFines.Models;
using SarilarTrafficFine.Business.Features.Vehicles;
using SarilarTrafficFine.Entities.Enums;
using SarilarTrafficFine.Web.Models.TrafficFines;
using SarilarTrafficFine.Web.Security;

namespace SarilarTrafficFine.Web.Controllers;

[Authorize]
public sealed class TrafficFinesController : Controller
{
    private readonly ITrafficFineService
        _trafficFineService;

    private readonly ITrafficFineApprovalQueryService
        _approvalQueryService;

    private readonly IVehicleService
        _vehicleService;

    public TrafficFinesController(
        ITrafficFineService trafficFineService,
        ITrafficFineApprovalQueryService approvalQueryService,
        IVehicleService vehicleService)
    {
        _trafficFineService =
            trafficFineService;

        _approvalQueryService =
            approvalQueryService;

        _vehicleService =
            vehicleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var trafficFines =
            await _trafficFineService.ListAsync(
                cancellationToken);

        var model = trafficFines
            .Select(BuildListItemViewModel)
            .ToList();

        return View(model);
    }

    [Authorize(
        Roles = RoleNames.Manager + "," + RoleNames.Finance)]
    [HttpGet]
    public async Task<IActionResult> PendingApprovals(
        CancellationToken cancellationToken)
    {
        var trafficFines =
            await _trafficFineService
                .GetPendingApprovalsAsync(
                    User.ToCurrentUserContext(),
                    cancellationToken);

        var model = trafficFines
            .Select(BuildListItemViewModel)
            .ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var trafficFine =
            await _trafficFineService.GetDetailsAsync(
                id,
                cancellationToken);

        if (trafficFine is null)
        {
            return NotFound();
        }

        var approval =
            await _approvalQueryService.GetAsync(
                id,
                cancellationToken);

        if (approval is null)
        {
            return NotFound();
        }

        var status =
            GetStatusPresentation(
                trafficFine.Status,
                approval.CurrentStepName);

        var isCreatedByCurrentUser =
    !string.IsNullOrWhiteSpace(
        User.Identity?.Name)
    && string.Equals(
        trafficFine.CreatedByUserName,
        User.Identity.Name,
        StringComparison.OrdinalIgnoreCase);

        var canApproveOrReject =
            trafficFine.Status ==
                TrafficFineStatus.InApproval
            && !string.IsNullOrWhiteSpace(
                approval.CurrentStepRequiredRole)
            && User.IsInRole(
                approval.CurrentStepRequiredRole)
            && !isCreatedByCurrentUser;

        var model =
            new TrafficFineDetailsViewModel(
                trafficFine.Id,
                trafficFine.VehicleId,
                trafficFine.PlateNumber,
                $"{trafficFine.Brand} {trafficFine.Model}",
                trafficFine.FineDate,
                trafficFine.Amount,
                trafficFine.Description,
                trafficFine.Status,
                status.Text,
                status.CssClass,
                trafficFine.CreatedByUserName,
                trafficFine.CreatedAt,
                trafficFine.UpdatedAt,
                approval.CurrentStepName,
                Convert.ToBase64String(
                    trafficFine.RowVersion),
                trafficFine.Status ==
                    TrafficFineStatus.New
                && User.IsInRole(
                    RoleNames.Operator))
            {
                CanSubmit =
                    trafficFine.Status ==
                        TrafficFineStatus.New
                    && User.IsInRole(
                        RoleNames.Operator),

                CanApproveOrReject =
                    canApproveOrReject,
                IsCreatedByCurrentUser =
                isCreatedByCurrentUser,

                CurrentStepRequiredRole =
                    approval.CurrentStepRequiredRole,

                CurrentStepRequiredRoleText =
                    GetRoleDisplayName(
                        approval.CurrentStepRequiredRole),

                WorkflowConfigurationError =
                    approval.ConfigurationError,

                WorkflowSteps =
                    BuildWorkflowSteps(
                        approval),

                ApprovalHistory =
                    BuildApprovalHistory(
                        approval)
            };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model =
            new TrafficFineCreateViewModel
            {
                FineDate =
                    DateOnly.FromDateTime(
                        DateTime.Today),

                Vehicles =
                    await GetVehicleOptionsAsync(
                        cancellationToken)
            };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TrafficFineCreateViewModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            input.Vehicles =
                await GetVehicleOptionsAsync(
                    cancellationToken);

            return View(input);
        }

        var request =
            new TrafficFineCreateRequest(
                input.VehicleId!.Value,
                input.FineDate!.Value,
                input.Amount!.Value,
                input.Description);

        var result =
            await _trafficFineService.CreateAsync(
                request,
                User.ToCurrentUserContext(),
                cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error ==
                TrafficFineCommandError.Forbidden)
            {
                return Forbid();
            }

            ModelState.AddModelError(
                result.ErrorField
                    ?? string.Empty,
                result.ErrorMessage
                    ?? "Trafik cezasý kaydedilemedi.");

            input.Vehicles =
                await GetVehicleOptionsAsync(
                    cancellationToken);

            return View(input);
        }

        TempData["SuccessMessage"] =
            "Trafik cezasý baþarýyla kaydedildi.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = result.TrafficFineId
            });
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var trafficFine =
            await _trafficFineService.GetDetailsAsync(
                id,
                cancellationToken);

        if (trafficFine is null)
        {
            return NotFound();
        }

        if (trafficFine.Status !=
            TrafficFineStatus.New)
        {
            TempData["ErrorMessage"] =
                "Yalnýzca Yeni durumundaki trafik cezalarý düzenlenebilir.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var model =
            new TrafficFineEditViewModel
            {
                Id =
                    trafficFine.Id,

                VehicleId =
                    trafficFine.VehicleId,

                FineDate =
                    trafficFine.FineDate,

                Amount =
                    trafficFine.Amount,

                Description =
                    trafficFine.Description,

                RowVersion =
                    Convert.ToBase64String(
                        trafficFine.RowVersion),

                Vehicles =
                    await GetVehicleOptionsAsync(
                        cancellationToken)
            };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        TrafficFineEditViewModel input,
        CancellationToken cancellationToken)
    {
        if (id != input.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            input.Vehicles =
                await GetVehicleOptionsAsync(
                    cancellationToken);

            return View(input);
        }

        byte[] expectedRowVersion;

        try
        {
            expectedRowVersion =
                Convert.FromBase64String(
                    input.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Kayýt sürüm bilgisi geçersiz.");

            input.Vehicles =
                await GetVehicleOptionsAsync(
                    cancellationToken);

            return View(input);
        }

        var request =
            new TrafficFineEditRequest(
                input.Id,
                input.VehicleId!.Value,
                input.FineDate!.Value,
                input.Amount!.Value,
                input.Description,
                expectedRowVersion);

        var result =
            await _trafficFineService.EditAsync(
                request,
                User.ToCurrentUserContext(),
                cancellationToken);

        if (!result.Succeeded)
        {
            switch (result.Error)
            {
                case TrafficFineCommandError.Forbidden:
                    return Forbid();

                case TrafficFineCommandError.NotFound:
                    return NotFound();

                case TrafficFineCommandError.InvalidState:
                    TempData["ErrorMessage"] =
                        result.ErrorMessage;

                    return RedirectToAction(
                        nameof(Details),
                        new { id });

                case TrafficFineCommandError.ConcurrencyConflict:
                    TempData["WarningMessage"] =
                        result.ErrorMessage;

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });

                default:
                    ModelState.AddModelError(
                        result.ErrorField
                            ?? string.Empty,
                        result.ErrorMessage
                            ?? "Trafik cezasý güncellenemedi.");

                    input.Vehicles =
                        await GetVehicleOptionsAsync(
                            cancellationToken);

                    return View(input);
            }
        }

        TempData["SuccessMessage"] =
            "Trafik cezasý baþarýyla güncellendi.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        var result =
            await _trafficFineService.SubmitAsync(
                id,
                User.ToCurrentUserContext(),
                cancellationToken);

        return HandleWorkflowResult(
            result,
            id,
            "Trafik cezasý onaya gönderildi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken cancellationToken)
    {
        var result =
            await _trafficFineService.ApproveAsync(
                id,
                User.ToCurrentUserContext(),
                cancellationToken);

        return HandleWorkflowResult(
            result,
            id,
            "Onay iþlemi baþarýyla tamamlandý.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        string reason,
        CancellationToken cancellationToken)
    {
        var result =
            await _trafficFineService.RejectAsync(
                id,
                reason,
                User.ToCurrentUserContext(),
                cancellationToken);

        return HandleWorkflowResult(
            result,
            id,
            "Trafik cezasý reddedildi.");
    }

    private IActionResult HandleWorkflowResult(
        TrafficFineCommandResult result,
        int id,
        string successMessage)
    {
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] =
                successMessage;

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        switch (result.Error)
        {
            case TrafficFineCommandError.Forbidden:
                return Forbid();

            case TrafficFineCommandError.NotFound:
                return NotFound();

            case TrafficFineCommandError.ConcurrencyConflict:
                TempData["WarningMessage"] =
                    result.ErrorMessage;
                break;

            default:
                TempData["ErrorMessage"] =
                    result.ErrorMessage
                    ?? "Ýþlem tamamlanamadý.";
                break;
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    private async Task<IReadOnlyList<TrafficFineVehicleOptionViewModel>>
        GetVehicleOptionsAsync(
            CancellationToken cancellationToken)
    {
        var vehicles =
            await _vehicleService.ListAsync(
                cancellationToken);

        return vehicles
            .Where(vehicle =>
                vehicle.IsActive)
            .OrderBy(vehicle =>
                vehicle.PlateNumber)
            .Select(vehicle =>
                new TrafficFineVehicleOptionViewModel(
                    vehicle.Id,
                    $"{vehicle.PlateNumber} — {vehicle.Brand} {vehicle.Model}"))
            .ToList();
    }

    private static IReadOnlyList<TrafficFineWorkflowStepViewModel>
        BuildWorkflowSteps(
            TrafficFineApprovalDetailsDto approval)
    {
        var rejectedStepOrder =
            approval.History
                .Where(history =>
                    history.ActionType ==
                        ApprovalActionType.Rejected)
                .OrderByDescending(history =>
                    history.ActionAt)
                .Select(history =>
                    history.WorkflowStepOrder)
                .FirstOrDefault();

        var currentStepOrder =
            approval.Steps
                .Where(step =>
                    step.Id ==
                    approval.CurrentApprovalStepId)
                .Select(step =>
                    (int?)step.StepOrder)
                .SingleOrDefault();

        return approval.Steps
            .OrderBy(step =>
                step.StepOrder)
            .Select(step =>
            {
                var state =
                    ResolveStepState(
                        approval.Status,
                        step.StepOrder,
                        currentStepOrder,
                        rejectedStepOrder);

                return new TrafficFineWorkflowStepViewModel(
                    step.Id,
                    step.StepOrder,
                    step.Name,
                    step.RequiredRole,
                    GetRoleDisplayName(
                        step.RequiredRole)
                        ?? step.RequiredRole,
                    state.Text,
                    state.CssClass);
            })
            .ToList();
    }

    private static StepPresentation ResolveStepState(
        TrafficFineStatus status,
        int stepOrder,
        int? currentStepOrder,
        int? rejectedStepOrder)
    {
        if (status ==
            TrafficFineStatus.Completed)
        {
            return new(
                "Tamamlandý",
                "workflow-step-completed");
        }

        if (status ==
            TrafficFineStatus.Rejected
            && rejectedStepOrder.HasValue)
        {
            if (stepOrder <
                rejectedStepOrder.Value)
            {
                return new(
                    "Tamamlandý",
                    "workflow-step-completed");
            }

            if (stepOrder ==
                rejectedStepOrder.Value)
            {
                return new(
                    "Reddedildi",
                    "workflow-step-rejected");
            }

            return new(
                "Bekliyor",
                "workflow-step-upcoming");
        }

        if (status ==
            TrafficFineStatus.InApproval
            && currentStepOrder.HasValue)
        {
            if (stepOrder <
                currentStepOrder.Value)
            {
                return new(
                    "Tamamlandý",
                    "workflow-step-completed");
            }

            if (stepOrder ==
                currentStepOrder.Value)
            {
                return new(
                    "Mevcut Aþama",
                    "workflow-step-current");
            }
        }

        return new(
            "Bekliyor",
            "workflow-step-upcoming");
    }

    private static IReadOnlyList<TrafficFineApprovalHistoryViewModel>
        BuildApprovalHistory(
            TrafficFineApprovalDetailsDto approval)
    {
        return approval.History
            .OrderByDescending(history =>
                history.ActionAt)
            .Select(history =>
            {
                var action =
                    history.ActionType switch
                    {
                        ApprovalActionType.Submitted =>
                            new HistoryPresentation(
                                "Onaya Gönderildi",
                                "history-submitted"),

                        ApprovalActionType.Approved =>
                            new HistoryPresentation(
                                "Onaylandý",
                                "history-approved"),

                        ApprovalActionType.Rejected =>
                            new HistoryPresentation(
                                "Reddedildi",
                                "history-rejected"),

                        _ =>
                            new HistoryPresentation(
                                history.ActionType.ToString(),
                                string.Empty)
                    };

                return new TrafficFineApprovalHistoryViewModel(
                    history.Id,
                    action.Text,
                    action.CssClass,
                    history.ActionAt,
                    history.ActionByUserName,
                    history.Comment,
                    history.PreviousState,
                    history.NewState,
                    history.WorkflowStepName);
            })
            .ToList();
    }

    private static TrafficFineListItemViewModel
        BuildListItemViewModel(
            TrafficFineListItemDto trafficFine)
    {
        var status =
            GetStatusPresentation(
                trafficFine.Status,
                trafficFine.CurrentStepName);

        return new TrafficFineListItemViewModel(
            trafficFine.Id,
            trafficFine.PlateNumber,
            trafficFine.VehicleName,
            trafficFine.FineDate,
            trafficFine.Amount,
            trafficFine.Status,
            status.Text,
            status.CssClass,
            trafficFine.CreatedByUserName,
            trafficFine.CurrentStepName);
    }

    private static StatusPresentation GetStatusPresentation(
        TrafficFineStatus status,
        string? currentStepName)
    {
        return status switch
        {
            TrafficFineStatus.New =>
                new(
                    "Yeni",
                    "sr-status-new"),

            TrafficFineStatus.InApproval =>
                new(
                    string.IsNullOrWhiteSpace(
                        currentStepName)
                        ? "Onayda"
                        : $"Onayda · {currentStepName}",
                    "sr-status-pending"),

            TrafficFineStatus.Completed =>
                new(
                    "Tamamlandý",
                    "sr-status-success"),

            TrafficFineStatus.Rejected =>
                new(
                    "Reddedildi",
                    "sr-status-danger"),

            _ =>
                new(
                    "Bilinmeyen",
                    "sr-status-new")
        };
    }

    private static string? GetRoleDisplayName(
        string? role)
    {
        return role switch
        {
            RoleNames.Operator =>
                "Operatör",

            RoleNames.Manager =>
                "Yönetici",

            RoleNames.Finance =>
                "Finans",

            null or "" =>
                null,

            _ =>
                role
        };
    }

    private sealed record StatusPresentation(
        string Text,
        string CssClass);

    private sealed record StepPresentation(
        string Text,
        string CssClass);

    private sealed record HistoryPresentation(
        string Text,
        string CssClass);
}