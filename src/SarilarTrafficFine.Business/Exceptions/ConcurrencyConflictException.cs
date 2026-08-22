namespace SarilarTrafficFine.Business.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}