namespace SarilarTrafficFine.Business.Features.TrafficFines.Models;

public sealed record TrafficFineCreateRequest(
	int VehicleId,
	DateOnly FineDate,
	decimal Amount,
	string? Description);