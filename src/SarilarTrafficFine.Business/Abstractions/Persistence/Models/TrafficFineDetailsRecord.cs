using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Business.Abstractions.Persistence.Models;

public sealed record TrafficFineDetailsRecord(
    int Id,
    int VehicleId,
    string PlateNumber,
    string Brand,
    string Model,
    DateOnly FineDate,
    decimal Amount,
    string? Description,
    TrafficFineStatus Status,
    string CreatedByUserId,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? CurrentStepName,
    byte[] RowVersion);