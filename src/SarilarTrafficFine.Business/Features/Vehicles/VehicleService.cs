using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.Business.Constants;
using SarilarTrafficFine.Business.Features.Vehicles.Models;
using SarilarTrafficFine.Entities.Enums;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.Business.Features.Vehicles;

public sealed class VehicleService : IVehicleService
{
    private readonly IGenericRepository<Vehicle> _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(
        IGenericRepository<Vehicle> vehicleRepository,
        IUnitOfWork unitOfWork)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<VehicleCreateResult> CreateAsync(
        VehicleCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plateNumber = request.PlateNumber.Trim().ToUpperInvariant();
        var brand = request.Brand.Trim();
        var model = request.Model.Trim();

        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            return VehicleCreateResult.Failure(
                "Plaka alaný zorunludur.");
        }

        if (plateNumber.Length > VehicleRules.PlateNumberMaxLength)
        {
            return VehicleCreateResult.Failure(
                $"Plaka en fazla {VehicleRules.PlateNumberMaxLength} karakter olabilir.");
        }

        if (!Enum.IsDefined(request.VehicleType))
        {
            return VehicleCreateResult.Failure(
                "Geçerli bir araç tipi seçiniz.");
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            return VehicleCreateResult.Failure(
                "Marka alaný zorunludur.");
        }

        if (brand.Length > VehicleRules.BrandMaxLength)
        {
            return VehicleCreateResult.Failure(
                $"Marka en fazla {VehicleRules.BrandMaxLength} karakter olabilir.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return VehicleCreateResult.Failure(
                "Model alaný zorunludur.");
        }

        if (model.Length > VehicleRules.ModelMaxLength)
        {
            return VehicleCreateResult.Failure(
                $"Model en fazla {VehicleRules.ModelMaxLength} karakter olabilir.");
        }

        var plateExists = await _vehicleRepository.AnyAsync(
            x => x.PlateNumber == plateNumber,
            cancellationToken);

        if (plateExists)
        {
            return VehicleCreateResult.Failure(
                "Bu plakaya sahip bir araç zaten kayýtlý.");
        }

        var vehicle = new Vehicle
        {
            PlateNumber = plateNumber,
            VehicleType = request.VehicleType,
            Brand = brand,
            Model = model,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _vehicleRepository.AddAsync(
            vehicle,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return VehicleCreateResult.Success(vehicle.Id);
    }

    public async Task<IReadOnlyList<VehicleListItemDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.ListAsync(
            cancellationToken);

        return vehicles
            .OrderBy(x => x.PlateNumber)
            .Select(x => new VehicleListItemDto(
                x.Id,
                x.PlateNumber,
                x.VehicleType,
                x.Brand,
                x.Model,
                x.IsActive))
            .ToList();
    }
}