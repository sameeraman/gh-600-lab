using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TodoApi.Services;

public sealed class LocalDevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "LocalDevelopment";
    private const string DevUserId = "local-dev-user";

    public LocalDevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = TryGetUserIdFromPrincipalHeader(Request.Headers["x-ms-client-principal"].FirstOrDefault()) ?? DevUserId;
        var claims = new[]
        {
            new Claim("oid", userId),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string? TryGetUserIdFromPrincipalHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(header));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("userId", out var id) ? id.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
