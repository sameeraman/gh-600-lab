using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly TodoContext _context;

    public TodoService(TodoContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TodoItem>> GetAllAsync(string userId)
    {
        return await _context.TodoItems
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TodoItem?> GetByIdAsync(int id, string userId)
    {
        return await _context.TodoItems.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<TodoItem> CreateAsync(TodoItem item, string userId)
    {
        item.UserId = userId;
        item.CreatedAt = DateTime.UtcNow;
        _context.TodoItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<TodoItem?> UpdateAsync(int id, TodoItem item, string userId)
    {
        var existing = await _context.TodoItems.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (existing == null) return null;

        existing.Title = item.Title;
        existing.Description = item.Description;
        existing.IsCompleted = item.IsCompleted;
        if (item.IsCompleted && !existing.IsCompleted)
            existing.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var item = await _context.TodoItems.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (item == null) return false;

        _context.TodoItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TodoItem?> ToggleCompleteAsync(int id, string userId)
    {
        var item = await _context.TodoItems.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (item == null) return null;

        item.IsCompleted = !item.IsCompleted;
        item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();
        return item;
    }
}
