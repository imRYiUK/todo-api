using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly TodoService _todoService;

    public TodosController(TodoService todoService)
    {
        _todoService = todoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TodoItem>>> Get() =>
        await _todoService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<TodoItem>> Get(string id)
    {
        var todo = await _todoService.GetAsync(id);
        if (todo is null)
        {
            return NotFound();
        }

        return todo;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Post(TodoItem newTodo)
    {
        await _todoService.CreateAsync(newTodo);
        return CreatedAtAction(nameof(Get), new { id = newTodo.Id }, newTodo);
    }

    [HttpPut("{id:length(24)}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string id, TodoItem updatedTodo)
    {
        var existingTodo = await _todoService.GetAsync(id);
        if (existingTodo is null)
        {
            return NotFound();
        }

        await _todoService.UpdateAsync(id, updatedTodo);
        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var existingTodo = await _todoService.GetAsync(id);
        if (existingTodo is null)
        {
            return NotFound();
        }

        await _todoService.RemoveAsync(id);
        return NoContent();
    }
}
