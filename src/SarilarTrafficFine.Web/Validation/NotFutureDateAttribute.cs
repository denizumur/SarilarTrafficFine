using System.ComponentModel.DataAnnotations;

namespace SarilarTrafficFine.Web.Validation;

[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class NotFutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(
        object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not DateOnly date)
        {
            return false;
        }

        var today =
            DateOnly.FromDateTime(
                DateTime.Now);

        return date <= today;
    }
}