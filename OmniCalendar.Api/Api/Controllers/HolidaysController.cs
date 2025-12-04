using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniCalendar.Api.Application.Holidays;

namespace OmniCalendar.Api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidaysController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    /// <summary>
    /// Get holidays for a given country and year.
    /// Used by Home page: this year and next year holidays for NZ / CN.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HolidayDto>>> GetAsync(
        [FromQuery] string countryCode,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await _holidayService.GetHolidaysAsync(countryCode, year, cancellationToken);
        return Ok(result);
    }
}


