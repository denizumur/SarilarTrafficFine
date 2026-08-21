using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Features.Vehicles.Models;

public sealed record VehicleCreateRequest(
    string PlateNumber,
    VehicleType VehicleType,
    string Brand,
    string Model);