namespace OmniCalendar.Api.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string ClerkId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public int ExperiencePoints { get; set; }
    public string CurrentRank { get; set; } = "Junior";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}


