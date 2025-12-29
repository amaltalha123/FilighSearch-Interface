using FlightSearch.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirportsController : ControllerBase
    {
        private readonly IAirportSearchService _airportService;

        public AirportsController(IAirportSearchService airportService)
        {
            _airportService = airportService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAirports([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                {
                    return BadRequest("Le mot-clé doit contenir au moins 2 caractères");
                }

                var airports = await _airportService.SearchAirportsAsync(q);
                return Ok(airports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}