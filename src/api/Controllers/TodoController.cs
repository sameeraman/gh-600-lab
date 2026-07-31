using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Authorize]
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
        var userId = ClientPrincipalAccessor.GetUserId(User);
        var items = await _todoService.GetAllAsync(userId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetById(int id)
    {
        var userId = ClientPrincipalAccessor.GetUserId(User);
        var item = await _todoService.GetByIdAsync(id, userId);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create(TodoItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
            return BadRequest("Title is required");

        var userId = ClientPrincipalAccessor.GetUserId(User);
        var created = await _todoService.CreateAsync(item, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TodoItem>> Update(int id, TodoItem item)
    {
        var userId = ClientPrincipalAccessor.GetUserId(User);
        var updated = await _todoService.UpdateAsync(id, item, userId);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = ClientPrincipalAccessor.GetUserId(User);
        var result = await _todoService.DeleteAsync(id, userId);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<TodoItem>> ToggleComplete(int id)
    {
        var userId = ClientPrincipalAccessor.GetUserId(User);
        var item = await _todoService.ToggleCompleteAsync(id, userId);
        if (item == null) return NotFound();
        return Ok(item);
    }
}
