using SarilarTrafficFine.Business.Features.Vehicles.Models;

namespace SarilarTrafficFine.Business.Features.Vehicles;

public interface IVehicleService
{
    Task<VehicleCreateResult> CreateAsync(
        VehicleCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleListItemDto>> ListAsync(
        CancellationToken cancellationToken = default);
}