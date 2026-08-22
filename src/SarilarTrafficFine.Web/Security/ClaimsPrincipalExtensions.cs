using System.Security.Claims;
using SarilarTrafficFine.Business.Security;

namespace SarilarTrafficFine.Web.Security;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUserContext ToCurrentUserContext(
        this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userId =
            principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "Kimliði doðrulanmýþ kullanýcý için kullanýcý kimliði bulunamadý.");
        }

        var userName =
            principal.Identity?.Name
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? userId;

        var roles = principal
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CurrentUserContext(
            userId,
            userName,
            roles);
    }
}