namespace SarilarTrafficFine.Business.Features.Vehicles.Models;

public sealed record VehicleCreateResult(
    bool Succeeded,
    int? VehicleId,
    string? ErrorMessage)
{
    public static VehicleCreateResult Success(int vehicleId)
    {
        return new VehicleCreateResult(
            true,
            vehicleId,
            null);
    }

    public static VehicleCreateResult Failure(string errorMessage)
    {
        return new VehicleCreateResult(
            false,
            null,
            errorMessage);
    }
}