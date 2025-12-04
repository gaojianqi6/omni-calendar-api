namespace OmniCalendar.Api.Domain.Entities;

public class TaskTag
{
    public Guid TaskId { get; set; }
    public int TagId { get; set; }

    public TaskItem Task { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}


