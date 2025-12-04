using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniCalendar.Api.Application.Auth;
using OmniCalendar.Api.Application.Dashboard;
using OmniCalendar.Api.Domain.Entities;
using OmniCalendar.Api.Infrastructure;
using Xunit;

namespace OmniCalendar.Api.Tests;

public class DashboardServiceTests : IDisposable
{
    private readonly OmniCalendarDbContext _db;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly DashboardService _service;
    private readonly User _testUser;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<OmniCalendarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new OmniCalendarDbContext(options);
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            ClerkId = "user_test123",
            Email = "test@example.com",
            ExperiencePoints = 100,
            CurrentRank = "Intermediate",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(_testUser);
        _db.SaveChanges();

        _mockCurrentUser.Setup(x => x.GetOrCreateUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _service = new DashboardService(_db, _mockCurrentUser.Object);
    }

    [Theory]
    [InlineData(0, "Junior")]
    [InlineData(1, "Beginner")]
    [InlineData(5, "Beginner")]
    [InlineData(10, "Intermediate")]
    [InlineData(50, "Intermediate")]
    [InlineData(100, "Advanced")]
    [InlineData(500, "Advanced")]
    [InlineData(1000, "Master")]
    [InlineData(5000, "Master")]
    [InlineData(10000, "Legend")]
    [InlineData(50000, "Legend")]
    public void GetRankForExperience_ReturnsExpectedRank(int xp, string expectedRank)
    {
        var rank = DashboardService.GetRankForExperience(xp);
        Assert.Equal(expectedRank, rank);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectStats()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);

        var tasks = new[]
        {
            // Today - completed
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Today Done",
                DueDate = today,
                Priority = 1,
                Status = "Completed",
                IsCompleted = true,
                CompletedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            // Today - not completed
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Today Left",
                DueDate = today,
                Priority = 1,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            // Yesterday - completed (counts in total)
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Yesterday Done",
                DueDate = yesterday,
                Priority = 1,
                Status = "Completed",
                IsCompleted = true,
                CompletedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            // Tomorrow - not completed (counts in total)
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Tomorrow Left",
                DueDate = tomorrow,
                Priority = 1,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
        _db.Tasks.AddRange(tasks);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal("Advanced", result.Rank); // 100 XP = Advanced
        Assert.Equal(100, result.ExperiencePoints);
        Assert.Equal(1, result.TodayDone);
        Assert.Equal(1, result.TodayLeft);
        Assert.Equal(2, result.TotalDone); // Today + Yesterday
        Assert.Equal(2, result.TotalLeft); // Today + Tomorrow
    }

    [Fact]
    public async Task GetSummaryAsync_OnlyCountsCurrentUserTasks()
    {
        // Arrange
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            ClerkId = "user_other",
            Email = "other@example.com",
            ExperiencePoints = 0,
            CurrentRank = "Junior",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(otherUser);
        await _db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var myTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Title = "My Task",
            DueDate = today,
            Priority = 1,
            Status = "Completed",
            IsCompleted = true,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var otherTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            Title = "Other User Task",
            DueDate = today,
            Priority = 1,
            Status = "Completed",
            IsCompleted = true,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Tasks.AddRange(myTask, otherTask);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(1, result.TodayDone);
        Assert.Equal(1, result.TotalDone);
    }

    [Fact]
    public async Task GetSummaryAsync_HandlesEmptyTasks()
    {
        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(0, result.TodayDone);
        Assert.Equal(0, result.TodayLeft);
        Assert.Equal(0, result.TotalDone);
        Assert.Equal(0, result.TotalLeft);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}


