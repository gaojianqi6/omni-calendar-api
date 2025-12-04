namespace OmniCalendar.Api.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string ColorHex { get; set; } = "#3B82F6";

    public User User { get; set; } = null!;
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}


