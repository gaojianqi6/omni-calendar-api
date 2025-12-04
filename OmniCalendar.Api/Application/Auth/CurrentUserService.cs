using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OmniCalendar.Api.Domain.Entities;
using OmniCalendar.Api.Infrastructure;

namespace OmniCalendar.Api.Application.Auth;

public interface ICurrentUserService
{
    Task<User> GetOrCreateUserAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the current Clerk user from the JWT and ensures a corresponding row exists in the users table.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OmniCalendarDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, OmniCalendarDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<User> GetOrCreateUserAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("No active HTTP context.");

        var user = httpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        // Clerk typically uses `sub` as the stable user ID (e.g., user_123)
        var clerkId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub")
                     ?? throw new InvalidOperationException("Clerk user id (sub) not found in token.");

        var email = user.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com";
        var nickname = user.FindFirstValue("name");
        var avatarUrl = user.FindFirstValue("picture");

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.ClerkId == clerkId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            ClerkId = clerkId,
            Email = email,
            Nickname = nickname,
            AvatarUrl = avatarUrl,
            ExperiencePoints = 0,
            CurrentRank = "Junior",
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(cancellationToken);

        return newUser;
    }
}


