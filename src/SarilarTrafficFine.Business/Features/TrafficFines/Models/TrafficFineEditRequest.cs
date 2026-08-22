namespace SarilarTrafficFine.Business.Features.TrafficFines.Models;

public sealed record TrafficFineEditRequest(
    int Id,
    int VehicleId,
    DateOnly FineDate,
    decimal Amount,
    string? Description,
    byte[] ExpectedRowVersion);