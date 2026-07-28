using TodoApi.Models;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<IEnumerable<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(int id);
    Task<TodoItem> CreateAsync(TodoItem item);
    Task<TodoItem?> UpdateAsync(int id, TodoItem item);
    Task<bool> DeleteAsync(int id);
    Task<TodoItem?> ToggleCompleteAsync(int id);
}
