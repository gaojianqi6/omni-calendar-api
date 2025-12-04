using OmniCalendar.Api.Application.Dashboard;
using Xunit;

namespace OmniCalendar.Api.Tests;

public class DashboardServiceTests
{
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
}


