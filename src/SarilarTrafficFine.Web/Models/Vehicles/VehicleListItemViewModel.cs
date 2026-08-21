namespace SarilarTrafficFine.Web.Models.Vehicles;

public sealed record VehicleListItemViewModel(
    int Id,
    string PlateNumber,
    string VehicleTypeName,
    string Brand,
    string Model,
    bool IsActive);