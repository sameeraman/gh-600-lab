using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly ITodoService _todoService;

    public TodoController(ITodoService todoService)
    {
        _todoService = todoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetAll()
    {
        var items = await _todoService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetById(int id)
    {
        var item = await _todoService.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create(TodoItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
            return BadRequest("Title is required");

        var created = await _todoService.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TodoItem>> Update(int id, TodoItem item)
    {
        var updated = await _todoService.UpdateAsync(id, item);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _todoService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<TodoItem>> ToggleComplete(int id)
    {
        var item = await _todoService.ToggleCompleteAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }
}
