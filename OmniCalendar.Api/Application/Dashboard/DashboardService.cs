using Microsoft.EntityFrameworkCore;
using OmniCalendar.Api.Application.Auth;
using OmniCalendar.Api.Infrastructure;

namespace OmniCalendar.Api.Application.Dashboard;

public record DashboardSummaryResponse(
    string Rank,
    int ExperiencePoints,
    int TodayDone,
    int TodayLeft,
    int TotalDone,
    int TotalLeft);

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public class DashboardService : IDashboardService
{
    private readonly OmniCalendarDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(OmniCalendarDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var user = await _currentUser.GetOrCreateUserAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todayDone = await _db.Tasks.CountAsync(
            t => t.UserId == user.Id && t.DueDate == today && t.IsCompleted,
            cancellationToken);

        var todayLeft = await _db.Tasks.CountAsync(
            t => t.UserId == user.Id && t.DueDate == today && !t.IsCompleted,
            cancellationToken);

        var totalDone = await _db.Tasks.CountAsync(
            t => t.UserId == user.Id && t.IsCompleted,
            cancellationToken);

        var totalLeft = await _db.Tasks.CountAsync(
            t => t.UserId == user.Id && !t.IsCompleted,
            cancellationToken);

        var rank = GetRankForExperience(user.ExperiencePoints);

        return new DashboardSummaryResponse(
            rank,
            user.ExperiencePoints,
            todayDone,
            todayLeft,
            totalDone,
            totalLeft);
    }

    public static string GetRankForExperience(int xp)
    {
        // (0, junior, 1, beginner, 10, 100, 1000, 10000)
        if (xp < 1) return "Junior";
        if (xp < 10) return "Beginner";
        if (xp < 100) return "Intermediate";
        if (xp < 1000) return "Advanced";
        if (xp < 10000) return "Master";
        return "Legend";
    }
}


