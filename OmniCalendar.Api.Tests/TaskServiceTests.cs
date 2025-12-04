using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniCalendar.Api.Application.Auth;
using OmniCalendar.Api.Application.Tasks;
using OmniCalendar.Api.Domain.Entities;
using OmniCalendar.Api.Infrastructure;
using Xunit;

namespace OmniCalendar.Api.Tests;

public class TaskServiceTests : IDisposable
{
    private readonly OmniCalendarDbContext _db;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly TaskService _service;
    private readonly User _testUser;

    public TaskServiceTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<OmniCalendarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new OmniCalendarDbContext(options);
        _mockCurrentUser = new Mock<ICurrentUserService>();

        // Create a test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            ClerkId = "user_test123",
            Email = "test@example.com",
            ExperiencePoints = 0,
            CurrentRank = "Junior",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(_testUser);
        _db.SaveChanges();

        _mockCurrentUser.Setup(x => x.GetOrCreateUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _service = new TaskService(_db, _mockCurrentUser.Object);
    }

    [Fact]
    public async Task CreateAsync_CreatesTaskSuccessfully()
    {
        // Arrange
        var request = new TaskCreateRequest(
            Title: "Test Task",
            Description: "Test Description",
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Priority: 1,
            RecurrenceRule: null,
            CategoryId: null,
            TagIds: null,
            IsAllDay: false
        );

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Task", result.Title);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(1, result.Priority);
        Assert.False(result.IsCompleted);

        var taskInDb = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == result.Id);
        Assert.NotNull(taskInDb);
        Assert.Equal(_testUser.Id, taskInDb.UserId);
        Assert.Equal("Pending", taskInDb.Status);
    }

    [Fact]
    public async Task CreateAsync_CreatesTaskWithTags()
    {
        // Arrange
        var tag1 = new Tag { Id = 1, UserId = _testUser.Id, Name = "Urgent", ColorHex = "#FF0000" };
        var tag2 = new Tag { Id = 2, UserId = _testUser.Id, Name = "Work", ColorHex = "#0000FF" };
        _db.Tags.AddRange(tag1, tag2);
        await _db.SaveChangesAsync();

        var request = new TaskCreateRequest(
            Title: "Task with Tags",
            Description: null,
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Priority: 2,
            RecurrenceRule: null,
            CategoryId: null,
            TagIds: new List<int> { 1, 2 },
            IsAllDay: false
        );

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        var taskTags = await _db.TaskTags
            .Where(tt => tt.TaskId == result.Id)
            .ToListAsync();
        Assert.Equal(2, taskTags.Count);
        Assert.Contains(taskTags, tt => tt.TagId == 1);
        Assert.Contains(taskTags, tt => tt.TagId == 2);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsTasksInRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(7);
        var nextMonth = today.AddDays(30);

        var tasks = new[]
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Today Task",
                DueDate = today,
                Priority = 1,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Tomorrow Task",
                DueDate = tomorrow,
                Priority = 2,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Next Week Task",
                DueDate = nextWeek,
                Priority = 3,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Out of Range",
                DueDate = nextMonth,
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
        var result = await _service.GetByDateRangeAsync(today, nextWeek);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, t => Assert.True(t.DueDate >= today && t.DueDate <= nextWeek));
        // Should be ordered by DueDate, then Priority, then Title
        Assert.Equal("Today Task", result[0].Title);
        Assert.Equal("Tomorrow Task", result[1].Title);
        Assert.Equal("Next Week Task", result[2].Title);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ExcludesTasksWithoutDueDate()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var taskWithDate = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Title = "Has Due Date",
            DueDate = today,
            Priority = 1,
            Status = "Pending",
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var taskWithoutDate = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Title = "No Due Date",
            DueDate = null,
            Priority = 1,
            Status = "Pending",
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Tasks.AddRange(taskWithDate, taskWithoutDate);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByDateRangeAsync(today, today.AddDays(7));

        // Assert
        Assert.Single(result);
        Assert.Equal("Has Due Date", result[0].Title);
    }

    [Fact]
    public async Task GetTodayAsync_ReturnsOnlyTodayTasks()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);

        var tasks = new[]
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Yesterday Task",
                DueDate = yesterday,
                Priority = 1,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Today Task 1",
                DueDate = today,
                Priority = 2,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Today Task 2",
                DueDate = today,
                Priority = 1,
                Status = "Pending",
                IsCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Title = "Tomorrow Task",
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
        var result = await _service.GetTodayAsync();

        // Assert
        Assert.Equal(2, result.Count);
        // Should be ordered by Priority (ascending), then Title
        Assert.Equal("Today Task 2", result[0].Title); // Priority 1
        Assert.Equal("Today Task 1", result[1].Title); // Priority 2
    }

    [Fact]
    public async Task GetByDateRangeAsync_OnlyReturnsCurrentUserTasks()
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
            Status = "Pending",
            IsCompleted = false,
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
            Status = "Pending",
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Tasks.AddRange(myTask, otherTask);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByDateRangeAsync(today, today);

        // Assert
        Assert.Single(result);
        Assert.Equal("My Task", result[0].Title);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

