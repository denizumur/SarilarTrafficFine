using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Web.Models.TrafficFines;

public sealed record TrafficFineListItemViewModel(
    int Id,
    string PlateNumber,
    string VehicleName,
    DateOnly FineDate,
    decimal Amount,
    TrafficFineStatus Status,
    string StatusText,
    string StatusCssClass,
    string CreatedByUserName,
    string? CurrentStepName);