using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniCalendar.Api.Application.Tasks;

namespace OmniCalendar.Api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Create a new task / event for the current Clerk user.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateAsync([FromBody] TaskCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByDateRangeAsync), new { from = result.DueDate, to = result.DueDate }, result);
    }

    /// <summary>
    /// Get tasks by due date range (inclusive) for the current Clerk user.
    /// This is the basis for the Todo page range filter (this year, etc.).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetByDateRangeAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var results = await _taskService.GetByDateRangeAsync(from, to, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Get today's tasks (due date == today) ordered by priority.
    /// Used for the Home page "Today's Event" section.
    /// </summary>
    [HttpGet("today")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetTodayAsync(CancellationToken cancellationToken)
    {
        var results = await _taskService.GetTodayAsync(cancellationToken);
        return Ok(results);
    }
}


