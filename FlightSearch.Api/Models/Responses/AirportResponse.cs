namespace FlightSearch.Api.Models.Responses
{
    public class AirportResponse
    {
        public string IataCode { get; set; }
        public string Name { get; set; }
        public string CityName { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
    }
}