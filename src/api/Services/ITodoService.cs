using TodoApi.Models;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<IEnumerable<TodoItem>> GetAllAsync(string userId);
    Task<TodoItem?> GetByIdAsync(int id, string userId);
    Task<TodoItem> CreateAsync(TodoItem item, string userId);
    Task<TodoItem?> UpdateAsync(int id, TodoItem item, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<TodoItem?> ToggleCompleteAsync(int id, string userId);
}
