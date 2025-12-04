namespace OmniCalendar.Api.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int? CategoryId { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsAllDay { get; set; }

    public int Priority { get; set; } = 4;
    public string Status { get; set; } = "Pending";

    public string? RecurrenceRule { get; set; }
    public Guid? ParentTaskId { get; set; }

    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> ChildTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskNote> Notes { get; set; } = new List<TaskNote>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}


