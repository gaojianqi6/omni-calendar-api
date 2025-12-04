namespace OmniCalendar.Api.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string ColorHex { get; set; } = "#808080";
    public int? ParentId { get; set; }
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}


