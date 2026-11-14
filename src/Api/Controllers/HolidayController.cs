using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/holiday")]
    public class HolidayController : ControllerBase
    {
        private readonly IHolidayDates _holidayDates;

        public HolidayController(IHolidayDates holidayDates)
        {
            _holidayDates = holidayDates;
        }

        [HttpGet("countries")]
        public async Task<IActionResult> Countries() =>
            Ok(await _holidayDates.GetCountriesAsync());

        [HttpGet("{country}/{year:int}")]
        public async Task<IActionResult> Get(string country, int year) =>
            Ok(await _holidayDates.GetOfficialPlannedHolidaysAsync(country, year));
    }
}
