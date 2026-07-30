using System.Text;
using System.Text.Json;

namespace TodoApi.Services;

public static class ClientPrincipalAccessor
{
    private const string DevUserId = "local-dev-user";

    public static string GetUserId(HttpContext context)
    {
        var header = context.Request.Headers["x-ms-client-principal"].FirstOrDefault();
        if (string.IsNullOrEmpty(header)) return DevUserId;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(header));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("userId", out var id)
                ? id.GetString() ?? DevUserId
                : DevUserId;
        }
        catch
        {
            return DevUserId;
        }
    }
}