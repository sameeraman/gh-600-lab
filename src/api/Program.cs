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
var tenantId = builder.Configuration["Authentication:TenantId"] ?? "common";
var clientId = builder.Configuration["Authentication:ClientId"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "AppAuthentication";
        options.DefaultChallengeScheme = "AppAuthentication";
    })
    .AddPolicyScheme("AppAuthentication", "App authentication", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Authentication:RequireSignedTokens")
                ? JwtBearerDefaults.AuthenticationScheme
                : LocalDevelopmentAuthenticationHandler.SchemeName;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = !string.IsNullOrWhiteSpace(clientId),
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
    })
    .AddScheme<AuthenticationSchemeOptions, LocalDevelopmentAuthenticationHandler>(
        LocalDevelopmentAuthenticationHandler.SchemeName,
        _ => { });
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
