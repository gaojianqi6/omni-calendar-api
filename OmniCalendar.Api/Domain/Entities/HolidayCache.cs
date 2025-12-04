namespace OmniCalendar.Api.Domain.Entities;

public class HolidayCache
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = null!;
    public int Year { get; set; }
    public string DataJson { get; set; } = null!;
    public DateTimeOffset FetchedAt { get; set; }
}


