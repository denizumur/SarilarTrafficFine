namespace SarilarTrafficFine.Business.Security;

public sealed record CurrentUserContext(
    string UserId,
    string UserName,
    IReadOnlyCollection<string> Roles)
{
    public bool IsInRole(string roleName)
    {
        return Roles.Contains(
            roleName,
            StringComparer.OrdinalIgnoreCase);
    }
}