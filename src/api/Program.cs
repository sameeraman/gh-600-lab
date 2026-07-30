using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using TodoApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var connectionString = builder.Configuration.GetConnectionString("TodoDb");

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

app.MapControllers();
app.Run();

public partial class Program { }
