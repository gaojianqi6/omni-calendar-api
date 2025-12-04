namespace OmniCalendar.Api.Application.Tasks;

public record TaskCreateRequest(
    string Title,
    string? Description,
    DateOnly? DueDate,
    int Priority,
    string? RecurrenceRule,
    int? CategoryId,
    List<int>? TagIds,
    bool IsAllDay);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    DateOnly? DueDate,
    int Priority,
    bool IsCompleted,
    DateTimeOffset? CompletedAt);


