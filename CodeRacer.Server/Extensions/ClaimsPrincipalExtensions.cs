using System.Security.Claims;

namespace CodeRacer.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the authenticated user's Id from the NameIdentifier claim of the JWT.
    /// Returns false when the claim is missing or is not a valid Guid.
    /// </summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var claimValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claimValue, out userId);
    }
}
