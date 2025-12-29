using FlightSearch.Api.Models.Requests;
using FlightSearch.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightSearchAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightSearchService _flightService;

        public FlightsController(IFlightSearchService flightService)
        {
            _flightService = flightService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchFlights([FromBody] FlightSearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var flights = await _flightService.SearchFlightsAsync(request);

                return Ok(new
                {
                    success = true,
                    count = flights.Count,
                    data = flights
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}