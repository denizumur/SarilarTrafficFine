namespace SarilarTrafficFine.Business.Features.TrafficFines.Models;

public enum TrafficFineCommandError
{
    None = 0,
    Validation = 1,
    Forbidden = 2,
    NotFound = 3,
    InvalidState = 4,
    ConcurrencyConflict = 5,
    Configuration = 6
}

public sealed record TrafficFineCommandResult(
    bool Succeeded,
    int? TrafficFineId,
    TrafficFineCommandError Error,
    string? ErrorField,
    string? ErrorMessage)
{
    public static TrafficFineCommandResult Success(
        int id)
    {
        return new(
            true,
            id,
            TrafficFineCommandError.None,
            null,
            null);
    }

    public static TrafficFineCommandResult Failure(
        TrafficFineCommandError error,
        string errorMessage,
        string? errorField = null)
    {
        return new(
            false,
            null,
            error,
            errorField,
            errorMessage);
    }
}