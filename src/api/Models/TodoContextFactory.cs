using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TodoApi.Models;

public class TodoContextFactory : IDesignTimeDbContextFactory<TodoContext>
{
    public TodoContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TodoContext>()
            .UseSqlServer("Server=localhost;Database=tododb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new TodoContext(options);
    }
}