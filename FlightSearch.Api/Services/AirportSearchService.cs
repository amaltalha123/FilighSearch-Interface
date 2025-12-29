using FlightSearch.api.Services;
using FlightSearch.Api.Models.Responses;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace FlightSearch.Api.Services
{
    public interface IAirportSearchService
    {
        Task<List<AirportResponse>> SearchAirportsAsync(string keyword);
    }

    public class AirportSearchService : IAirportSearchService
    {
        private readonly IAmadeusAuthService _authService;
        private readonly IConfiguration _configuration;

        public AirportSearchService(IAmadeusAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<List<AirportResponse>> SearchAirportsAsync(string keyword)
        {
            var token = await _authService.GetAccessTokenAsync();
            var baseUrl = _configuration["Amadeus:BaseUrl"];

            var client = new RestClient(baseUrl);
            var request = new RestRequest("/v1/reference-data/locations", Method.Get);

            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddParameter("subType", "AIRPORT,CITY");
            request.AddParameter("keyword", keyword);

            request.AddParameter("page[limit]", 30);

            Console.WriteLine("AMADEUS REQUEST => " + client.BuildUri(request));

            var response = await client.ExecuteAsync(request);

            Console.WriteLine("AMADEUS STATUS => " + response.StatusCode);
            Console.WriteLine("AMADEUS BODY => " + response.Content);


            if (!response.IsSuccessful)
            {
                throw new Exception($"Erreur de recherche d'aéroports: {response.Content}");
            }

            var json = JObject.Parse(response.Content);
            var data = json["data"];

            var airports = new List<AirportResponse>();

            foreach (var item in data)
            {
                airports.Add(new AirportResponse
                {
                    IataCode = item["iataCode"]?.ToString(),
                    Name = item["name"]?.ToString(),
                    CityName = item["address"]?["cityName"]?.ToString(),
                    CountryCode = item["address"]?["countryCode"]?.ToString(),
                    CountryName = item["address"]?["countryName"]?.ToString()
                });
            }

            return airports;
        }
    }
}