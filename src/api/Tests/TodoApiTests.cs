using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests;

public class TodoApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TodoApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = CreateDevelopmentClient();
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTodos()
    {
        var response = await _client.GetAsync("/api/todo");
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<TodoItem>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task Create_ReturnsTodo_WithValidInput()
    {
        var todo = new TodoItem { Title = "Test Todo", Description = "Test Description" };
        var response = await _client.PostAsJsonAsync("/api/todo", todo);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.NotNull(created);
        Assert.Equal("Test Todo", created.Title);
        Assert.False(created.IsCompleted);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenTitleEmpty()
    {
        var todo = new TodoItem { Title = "", Description = "No title" };
        var response = await _client.PostAsJsonAsync("/api/todo", todo);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvalidId()
    {
        var response = await _client.GetAsync("/api/todo/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleComplete_TogglesStatus()
    {
        var todo = new TodoItem { Title = "Toggle Test" };
        var createResponse = await _client.PostAsJsonAsync("/api/todo", todo);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        var toggleResponse = await _client.PatchAsync($"/api/todo/{created!.Id}/toggle", null);
        var toggled = await toggleResponse.Content.ReadFromJsonAsync<TodoItem>();
        Assert.True(toggled!.IsCompleted);
        Assert.NotNull(toggled.CompletedAt);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenExists()
    {
        var todo = new TodoItem { Title = "Delete Test" };
        var createResponse = await _client.PostAsJsonAsync("/api/todo", todo);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        var deleteResponse = await _client.DeleteAsync($"/api/todo/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Todos_AreScopedToAuthenticatedUser()
    {
        SetUser("user-a");
        var todo = new TodoItem { Title = "Private Todo", UserId = "untrusted-user" };
        var createResponse = await _client.PostAsJsonAsync("/api/todo", todo);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        Assert.NotNull(created);
        Assert.Equal("user-a", created.UserId);

        SetUser("user-b");
        var listResponse = await _client.GetFromJsonAsync<List<TodoItem>>("/api/todo");
        var getResponse = await _client.GetAsync($"/api/todo/{created.Id}");

        Assert.NotNull(listResponse);
        Assert.Empty(listResponse);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task HostedApi_ReturnsUnauthorized_WhenAuthenticationHeadersAreMissing()
    {
        using var client = CreateHostedClient();

        var response = await client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HostedApi_ReturnsUnauthorized_WhenOnlyUnsignedClientPrincipalHeaderIsPresent()
    {
        using var client = CreateHostedClient();
        SetUser(client, "user-a");

        var response = await client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HostedApi_ReturnsUnauthorized_WhenSignedTokenHeaderIsMalformed()
    {
        using var client = CreateHostedClient();
        client.DefaultRequestHeaders.Add("x-ms-token-aad-id-token", "not-a-jwt");

        var response = await client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void NonDevelopmentApi_Throws_WhenAuthenticationClientIdIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var _ = CreateClient(environmentName: "Production");
        });

        Assert.Contains("Authentication:ClientId", exception.Message);
    }

    [Fact]
    public async Task DevelopmentApi_AllowsUnsignedClientPrincipalHeader_WhenSignedTokensAreDisabled()
    {
        using var client = CreateClient(environmentName: "Development", requireSignedTokens: false);
        SetUser(client, "user-a");

        var response = await client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentApi_ReturnsUnauthorized_WhenUnsignedClientPrincipalHeaderIsMissing()
    {
        using var client = CreateClient(environmentName: "Development", requireSignedTokens: false);

        var response = await client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentApi_ReturnsUnauthorized_WhenUnsignedClientPrincipalHeaderIsMalformed()
    {
        using var client = CreateClient(environmentName: "Development", requireSignedTokens: false);
        client.DefaultRequestHeaders.Remove("x-ms-client-principal");
        client.DefaultRequestHeaders.Add("x-ms-client-principal", "not-base64");

        var response = await client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void HostedApi_Throws_WhenAuthenticationClientIdIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var _ = CreateClient(environmentName: "Production", requireSignedTokens: true);
        });

        Assert.Contains("Authentication:ClientId", exception.Message);
    }

    private HttpClient CreateHostedClient()
    {
        return CreateClient(
            environmentName: "Production",
            requireSignedTokens: true,
            clientId: "11111111-1111-1111-1111-111111111111");
    }

    private HttpClient CreateDevelopmentClient()
    {
        var client = CreateClient(environmentName: "Development", requireSignedTokens: false);
        SetUser(client, "local-dev-user");
        return client;
    }

    private HttpClient CreateClient(
        string environmentName = "Development",
        bool? requireSignedTokens = null,
        string? clientId = null)
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environmentName);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                if (requireSignedTokens is not null)
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:RequireSignedTokens"] = requireSignedTokens.Value.ToString(),
                        ["Authentication:TenantId"] = "common",
                        ["Authentication:ClientId"] = clientId
                    });
                }
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddDbContext<TodoContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        }).CreateClient();
    }

    private void SetUser(string userId)
    {
        SetUser(_client, userId);
    }

    private static void SetUser(HttpClient client, string userId)
    {
        var principal = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { userId })));
        client.DefaultRequestHeaders.Remove("x-ms-client-principal");
        client.DefaultRequestHeaders.Add("x-ms-client-principal", principal);
    }
}
