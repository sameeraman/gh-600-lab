using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TodoApi.Services;

// Reads the identity that App Service Easy Auth injects for the linked Static Web App.
// Only trustworthy because Easy Auth authenticates every request and overwrites any
// inbound x-ms-client-principal header before the app sees it.
public sealed class ClientPrincipalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ClientPrincipal";

    private const string HeaderName = "x-ms-client-principal";

    public ClientPrincipalAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
        {
            return Task.FromResult(AuthenticateResult.Fail($"A valid {HeaderName} header is required."));
        }

        List<Claim> claims;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(header));
            using var document = JsonDocument.Parse(json);
            claims = ReadClaims(document.RootElement);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return Task.FromResult(AuthenticateResult.Fail($"The {HeaderName} header could not be decoded."));
        }

        if (!claims.Any(claim => claim.Type is "oid" or ClaimTypes.NameIdentifier or "sub"))
        {
            return Task.FromResult(AuthenticateResult.Fail($"The {HeaderName} header did not contain a user identifier."));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, "name", ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    // Static Web Apps emits { userId, userDetails, userRoles }; Easy Auth emits { claims: [{ typ, val }] }.
    private static List<Claim> ReadClaims(JsonElement root)
    {
        var claims = new List<Claim>();

        if (TryGetNonEmptyString(root, "userId", out var userId))
        {
            claims.Add(new Claim("oid", userId));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (TryGetNonEmptyString(root, "userDetails", out var userDetails))
        {
            claims.Add(new Claim("name", userDetails));
        }

        if (root.TryGetProperty("userRoles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            claims.AddRange(roles.EnumerateArray()
                .Where(role => role.ValueKind == JsonValueKind.String)
                .Select(role => new Claim(ClaimTypes.Role, role.GetString()!)));
        }

        if (root.TryGetProperty("claims", out var easyAuthClaims) && easyAuthClaims.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in easyAuthClaims.EnumerateArray())
            {
                if (TryGetNonEmptyString(entry, "typ", out var type) && TryGetNonEmptyString(entry, "val", out var value))
                {
                    claims.Add(new Claim(NormalizeClaimType(type), value));
                }
            }
        }

        return claims;
    }

    private static bool TryGetNonEmptyString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return value.Length > 0;
    }

    private static string NormalizeClaimType(string type) => type switch
    {
        "http://schemas.microsoft.com/identity/claims/objectidentifier" => "oid",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => ClaimTypes.NameIdentifier,
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => "name",
        _ => type
    };
}
