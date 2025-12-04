using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniCalendar.Api.Application.Dashboard;

namespace OmniCalendar.Api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Summary statistics for the Todo Dashboard (rank, today stats, totals).
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }
}


