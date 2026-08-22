using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Abstractions.Persistence.Models;

public sealed record TrafficFineListRecord(
    int Id,
    string PlateNumber,
    string Brand,
    string Model,
    DateOnly FineDate,
    decimal Amount,
    TrafficFineStatus Status,
    string CreatedByUserName,
    string? CurrentStepName);