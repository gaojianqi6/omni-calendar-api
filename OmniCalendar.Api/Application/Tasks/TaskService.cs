using Microsoft.EntityFrameworkCore;
using OmniCalendar.Api.Application.Auth;
using OmniCalendar.Api.Domain.Entities;
using OmniCalendar.Api.Infrastructure;

namespace OmniCalendar.Api.Application.Tasks;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(TaskCreateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResponse>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResponse>> GetTodayAsync(CancellationToken cancellationToken = default);
}

public class TaskService : ITaskService
{
    private readonly OmniCalendarDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TaskService(OmniCalendarDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TaskResponse> CreateAsync(TaskCreateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _currentUser.GetOrCreateUserAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Priority = request.Priority,
            RecurrenceRule = request.RecurrenceRule,
            IsAllDay = request.IsAllDay,
            Status = "Pending",
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Tasks.Add(task);

        if (request.TagIds is { Count: > 0 })
        {
            foreach (var tagId in request.TagIds)
            {
                _db.TaskTags.Add(new TaskTag
                {
                    TaskId = task.Id,
                    TagId = tagId
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new TaskResponse(task.Id, task.Title, task.Description, task.DueDate, task.Priority, task.IsCompleted, task.CompletedAt);
    }

    public async Task<IReadOnlyList<TaskResponse>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var user = await _currentUser.GetOrCreateUserAsync(cancellationToken);

        var tasks = await _db.Tasks
            .Where(t => t.UserId == user.Id &&
                        t.DueDate != null &&
                        t.DueDate >= from &&
                        t.DueDate <= to)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.Title)
            .Select(t => new TaskResponse(t.Id, t.Title, t.Description, t.DueDate, t.Priority, t.IsCompleted, t.CompletedAt))
            .ToListAsync(cancellationToken);

        return tasks;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = await _currentUser.GetOrCreateUserAsync(cancellationToken);

        var tasks = await _db.Tasks
            .Where(t => t.UserId == user.Id && t.DueDate == today)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Title)
            .Select(t => new TaskResponse(t.Id, t.Title, t.Description, t.DueDate, t.Priority, t.IsCompleted, t.CompletedAt))
            .ToListAsync(cancellationToken);

        return tasks;
    }
}


