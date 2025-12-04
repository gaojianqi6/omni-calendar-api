namespace OmniCalendar.Api.Domain.Entities;

public class TaskNote
{
    public int Id { get; set; }
    public Guid TaskId { get; set; }
    public string Note { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public TaskItem Task { get; set; } = null!;
}


