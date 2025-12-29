namespace FlightSearch.Api.Models.Responses
{
    public class FlightOfferResponse
    {
        public string Id { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public int NumberOfBookableSeats { get; set; }
        public List<Itinerary> Itineraries { get; set; }
        public string ValidatingAirlineCodes { get; set; }
    }

    public class Itinerary
    {
        public string Duration { get; set; } // Format: PT2H30M
        public int DurationInMinutes { get; set; }
        public List<Segment> Segments { get; set; }
    }

    public class Segment
    {
        public Departure Departure { get; set; }
        public Arrival Arrival { get; set; }
        public string CarrierCode { get; set; }
        public string FlightNumber { get; set; }
        public Aircraft Aircraft { get; set; }
        public string Duration { get; set; }
        public int NumberOfStops { get; set; }
    }

    public class Departure
    {
        public string IataCode { get; set; }
        public string Terminal { get; set; }
        public DateTime At { get; set; }
    }

    public class Arrival
    {
        public string IataCode { get; set; }
        public string Terminal { get; set; }
        public DateTime At { get; set; }
    }

    public class Aircraft
    {
        public string Code { get; set; }
    }
}