using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OmniCalendar.Api.Domain.Entities;
using OmniCalendar.Api.Infrastructure;

namespace OmniCalendar.Api.Application.Holidays;

public record HolidayDto(string Name, string Description, string DateIso, string PrimaryType);

public interface IHolidayService
{
    Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(string countryCode, int year, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fetches holidays from Calendarific and caches the raw JSON in the holiday_cache table.
/// </summary>
public class HolidayService : IHolidayService
{
    private readonly OmniCalendarDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HolidayService(OmniCalendarDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(string countryCode, int year, CancellationToken cancellationToken = default)
    {
        countryCode = countryCode.ToUpperInvariant();

        var cached = await _db.HolidayCache
            .FirstOrDefaultAsync(h => h.CountryCode == countryCode && h.Year == year, cancellationToken);

        string json;
        if (cached != null)
        {
            json = cached.DataJson;
        }
        else
        {
            var apiKey = _configuration["Calendarific:ApiKey"]
                         ?? throw new InvalidOperationException("Calendarific:ApiKey is not configured.");
            var baseUrl = _configuration["Calendarific:BaseUrl"] ?? "https://calendarific.com/api/v2";

            var client = _httpClientFactory.CreateClient("calendarific");
            client.BaseAddress = new Uri(baseUrl);

            var url = $"holidays?api_key={apiKey}&country={countryCode}&year={year}";
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadAsStringAsync(cancellationToken);

            var cache = new HolidayCache
            {
                CountryCode = countryCode,
                Year = year,
                DataJson = json,
                FetchedAt = DateTimeOffset.UtcNow
            };

            _db.HolidayCache.Add(cache);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // We keep parsing simple: use a dynamic-like anonymous projection via JsonDocument.
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var holidaysElement = document.RootElement
            .GetProperty("response")
            .GetProperty("holidays");

        var result = new List<HolidayDto>();
        foreach (var h in holidaysElement.EnumerateArray())
        {
            var name = h.GetProperty("name").GetString() ?? string.Empty;
            var description = h.GetProperty("description").GetString() ?? string.Empty;
            var dateIso = h.GetProperty("date").GetProperty("iso").GetString() ?? string.Empty;
            var primaryType = h.TryGetProperty("primary_type", out var pt)
                ? pt.GetString() ?? string.Empty
                : string.Empty;

            result.Add(new HolidayDto(name, description, dateIso, primaryType));
        }

        return result;
    }
}


