using FlightSearch.Api.Models.Responses;

namespace FlightSearch.Api.Helpers
{
    public static class FlightFilterHelper
    {
        public static List<FlightOfferResponse> SortFlights(
            List<FlightOfferResponse> flights,
            string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "price_asc" => flights.OrderBy(f => f.Price).ToList(),
                "price_desc" => flights.OrderByDescending(f => f.Price).ToList(),
                "duration_asc" => flights.OrderBy(f => f.Itineraries[0].DurationInMinutes).ToList(),
                "duration_desc" => flights.OrderByDescending(f => f.Itineraries[0].DurationInMinutes).ToList(),
                "departure_asc" => flights.OrderBy(f => f.Itineraries[0].Segments[0].Departure.At).ToList(),
                "departure_desc" => flights.OrderByDescending(f => f.Itineraries[0].Segments[0].Departure.At).ToList(),
                _ => flights
            };
        }

        public static List<FlightOfferResponse> FilterFlights(
            List<FlightOfferResponse> flights,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? directOnly = null,
            int? maxStops = null,
            List<string> airlines = null,
            int? maxDuration = null)
        {
            var filtered = flights.AsEnumerable();

            if (minPrice.HasValue)
                filtered = filtered.Where(f => f.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                filtered = filtered.Where(f => f.Price <= maxPrice.Value);

            if (directOnly.HasValue && directOnly.Value)
                filtered = filtered.Where(f => f.Itineraries[0].Segments.Count == 1);

            if (maxStops.HasValue)
                filtered = filtered.Where(f => f.Itineraries[0].Segments.Count - 1 <= maxStops.Value);

            if (airlines != null && airlines.Any())
                filtered = filtered.Where(f => airlines.Contains(f.ValidatingAirlineCodes));

            if (maxDuration.HasValue)
                filtered = filtered.Where(f => f.Itineraries[0].DurationInMinutes <= maxDuration.Value);

            return filtered.ToList();
        }
    }
}