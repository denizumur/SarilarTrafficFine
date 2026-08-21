using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarilarTrafficFine.Business.Constants;
using SarilarTrafficFine.Business.Features.Vehicles;
using SarilarTrafficFine.Business.Features.Vehicles.Models;
using SarilarTrafficFine.Entities.Enums;
using SarilarTrafficFine.Web.Models.Vehicles;

namespace SarilarTrafficFine.Web.Controllers;

[Authorize]
public sealed class VehiclesController : Controller
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleService.ListAsync(
            cancellationToken);

        var model = vehicles
            .Select(x => new VehicleListItemViewModel(
                x.Id,
                x.PlateNumber,
                GetVehicleTypeName(x.VehicleType),
                x.Brand,
                x.Model,
                x.IsActive))
            .ToList();

        return View(model);
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new VehicleCreateViewModel());
    }

    [Authorize(Roles = RoleNames.Operator)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        VehicleCreateViewModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var request = new VehicleCreateRequest(
            input.PlateNumber,
            input.VehicleType!.Value,
            input.Brand,
            input.Model);

        var result = await _vehicleService.CreateAsync(
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                nameof(input.PlateNumber),
                result.ErrorMessage ?? "Araç kaydedilemedi.");

            return View(input);
        }

        TempData["SuccessMessage"] =
            "Araç baþarýyla kaydedildi.";

        return RedirectToAction(nameof(Index));
    }

    private static string GetVehicleTypeName(
        VehicleType vehicleType)
    {
        return vehicleType switch
        {
            VehicleType.PassengerCar => "Binek",
            VehicleType.TruckTractor => "Çekici",
            VehicleType.Trailer => "Dorse",
            VehicleType.RentalVehicle => "Kiralýk Araç",
            _ => "Bilinmeyen"
        };
    }
}