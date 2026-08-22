using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Features.TrafficFines.Models;

public sealed record TrafficFineListItemDto(
    int Id,
    string PlateNumber,
    string VehicleName,
    DateOnly FineDate,
    decimal Amount,
    TrafficFineStatus Status,
    string CreatedByUserName,
    string? CurrentStepName);