using System.Security.Claims;

namespace TodoApi.Services;

public static class ClientPrincipalAccessor
{
    public static string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue("oid")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated user is missing an identifier.");
    }
}