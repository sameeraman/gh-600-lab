using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Models;
using TodoApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var connectionString = builder.Configuration.GetConnectionString("TodoDb");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "AppAuthentication";
        options.DefaultChallengeScheme = "AppAuthentication";
    })
    .AddPolicyScheme("AppAuthentication", "App authentication", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
#if DEBUG
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
            var requireSignedTokens = configuration.GetValue("Authentication:RequireSignedTokens", true);

            return environment.IsDevelopment() && !requireSignedTokens
                ? LocalDevelopmentAuthenticationHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme;
#else
            return JwtBearerDefaults.AuthenticationScheme;
#endif
        };
    })
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var tenantId = configuration["Authentication:TenantId"] ?? "common";
        var clientId = configuration["Authentication:ClientId"];

        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = clientId,
            NameClaimType = "name"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Headers["x-ms-token-aad-id-token"].FirstOrDefault()
                    ?? context.Request.Headers["x-ms-token-aad-access-token"].FirstOrDefault();
                return Task.CompletedTask;
            }
        };
    });

#if DEBUG
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, LocalDevelopmentAuthenticationHandler>(
        LocalDevelopmentAuthenticationHandler.SchemeName,
        _ => { });
#endif

builder.Services.AddAuthorization();

builder.Services.AddDbContext<TodoContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
        options.UseInMemoryDatabase("TodoList");
    else
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});
builder.Services.AddScoped<ITodoService, TodoService>();

var app = builder.Build();

#if DEBUG
var allowLocalDevelopmentAuthentication = app.Environment.IsDevelopment()
    && !app.Configuration.GetValue("Authentication:RequireSignedTokens", true);
#else
const bool allowLocalDevelopmentAuthentication = false;
#endif

if (!allowLocalDevelopmentAuthentication && string.IsNullOrWhiteSpace(app.Configuration["Authentication:ClientId"]))
{
    throw new InvalidOperationException("Authentication:ClientId must be configured when signed tokens are required.");
}

if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TodoContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
