using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Moq;
using OmniCalendar.Api.Application.Holidays;
using OmniCalendar.Api.Domain.Entities;
using OmniCalendar.Api.Infrastructure;
using Xunit;

namespace OmniCalendar.Api.Tests;

public class HolidayServiceTests : IDisposable
{
    private readonly OmniCalendarDbContext _db;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly TestHttpMessageHandler _testHttpHandler;
    private readonly HolidayService _service;

    public HolidayServiceTests()
    {
        var options = new DbContextOptionsBuilder<OmniCalendarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new OmniCalendarDbContext(options);
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IConfiguration>();
        _testHttpHandler = new TestHttpMessageHandler();

        _mockConfiguration.Setup(c => c["Calendarific:ApiKey"]).Returns("test-api-key");
        _mockConfiguration.Setup(c => c["Calendarific:BaseUrl"]).Returns("https://calendarific.com/api/v2");

        var httpClient = new HttpClient(_testHttpHandler)
        {
            BaseAddress = new Uri("https://calendarific.com/api/v2")
        };
        _mockHttpClientFactory.Setup(f => f.CreateClient("calendarific")).Returns(httpClient);

        _service = new HolidayService(_db, _mockHttpClientFactory.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task GetHolidaysAsync_FetchesFromApiAndCaches()
    {
        // Arrange
        var mockResponse = new
        {
            meta = new { code = 200 },
            response = new
            {
                holidays = new[]
                {
                    new
                    {
                        name = "New Year's Day",
                        description = "New Year's Day is a public holiday",
                        date = new { iso = "2026-01-01" },
                        primary_type = "National holiday"
                    },
                    new
                    {
                        name = "Waitangi Day",
                        description = "Waitangi Day commemorates the Treaty of Waitangi",
                        date = new { iso = "2026-02-06" },
                        primary_type = "National holiday"
                    }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _testHttpHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await _service.GetHolidaysAsync("NZ", 2026);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("New Year's Day", result[0].Name);
        Assert.Equal("Waitangi Day", result[1].Name);
        Assert.Equal("2026-01-01", result[0].DateIso);
        Assert.Equal("National holiday", result[0].PrimaryType);

        // Verify it was cached
        var cached = await _db.HolidayCache
            .FirstOrDefaultAsync(h => h.CountryCode == "NZ" && h.Year == 2026);
        Assert.NotNull(cached);
        Assert.NotNull(cached.DataJson);
        Assert.NotEmpty(cached.DataJson);
        
        // Verify the cached JSON is valid and contains the holiday data
        using var doc = JsonDocument.Parse(cached.DataJson);
        var holidays = doc.RootElement.GetProperty("response").GetProperty("holidays");
        Assert.True(holidays.GetArrayLength() >= 1);
        var firstHoliday = holidays[0];
        Assert.Equal("New Year's Day", firstHoliday.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetHolidaysAsync_ReturnsFromCacheWhenAvailable()
    {
        // Arrange - Pre-populate cache
        var cachedJson = JsonSerializer.Serialize(new
        {
            meta = new { code = 200 },
            response = new
            {
                holidays = new[]
                {
                    new
                    {
                        name = "Cached Holiday",
                        description = "This is from cache",
                        date = new { iso = "2026-03-15" },
                        primary_type = "Observance"
                    }
                }
            }
        });

        var cache = new HolidayCache
        {
            CountryCode = "NZ",
            Year = 2026,
            DataJson = cachedJson,
            FetchedAt = DateTimeOffset.UtcNow
        };
        _db.HolidayCache.Add(cache);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetHolidaysAsync("NZ", 2026);

        // Assert
        Assert.Single(result);
        Assert.Equal("Cached Holiday", result[0].Name);

        // Verify HTTP client was NOT called (cache was used)
        Assert.Equal(0, _testHttpHandler.CallCount);
    }

    [Fact]
    public async Task GetHolidaysAsync_NormalizesCountryCodeToUpper()
    {
        // Arrange
        var mockResponse = new
        {
            meta = new { code = 200 },
            response = new { holidays = Array.Empty<object>() }
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _testHttpHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        // Act - Pass lowercase country code
        await _service.GetHolidaysAsync("nz", 2026);

        // Assert - Should be stored as uppercase in cache
        var cached = await _db.HolidayCache
            .FirstOrDefaultAsync(h => h.CountryCode == "NZ" && h.Year == 2026);
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task GetHolidaysAsync_HandlesEmptyHolidaysList()
    {
        // Arrange
        var mockResponse = new
        {
            meta = new { code = 200 },
            response = new { holidays = Array.Empty<object>() }
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _testHttpHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await _service.GetHolidaysAsync("CN", 2026);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHolidaysAsync_ThrowsWhenApiKeyMissing()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Calendarific:ApiKey"]).Returns((string?)null);

        var service = new HolidayService(_db, _mockHttpClientFactory.Object, _mockConfiguration.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetHolidaysAsync("NZ", 2026));
    }

    public void Dispose()
    {
        _db.Dispose();
        _testHttpHandler.Dispose();
    }
}

// Simple test HTTP handler for mocking HttpClient
public class TestHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage? _response;
    public int CallCount { get; private set; }

    public void SetResponse(HttpResponseMessage response)
    {
        _response = response;
        CallCount = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

