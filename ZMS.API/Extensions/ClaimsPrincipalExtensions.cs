using System.Security.Claims;

namespace ZMS.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the Supabase user ID (sub claim) from the JWT token.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal user)
    {
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        
        if (subClaim == null)
            throw new InvalidOperationException("User ID claim not found. Ensure JWT is properly configured and user is authenticated.");
        
        return subClaim.Value;
    }

    /// <summary>
    /// Safely extracts the user ID, returning null if not found.
    /// </summary>
    public static string? TryGetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    }
}
