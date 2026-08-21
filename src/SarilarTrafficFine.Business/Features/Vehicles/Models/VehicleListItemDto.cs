using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Features.Vehicles.Models;

public sealed record VehicleListItemDto(
    int Id,
    string PlateNumber,
    VehicleType VehicleType,
    string Brand,
    string Model,
    bool IsActive);