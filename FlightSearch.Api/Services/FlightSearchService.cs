using FlightSearch.api.Services;
using FlightSearch.Api.Models.Requests;
using FlightSearch.Api.Models.Responses;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace FlightSearch.Api.Services
{
    public interface IFlightSearchService
    {
        Task<List<FlightOfferResponse>> SearchFlightsAsync(FlightSearchRequest searchRequest);
    }

    public class FlightSearchService : IFlightSearchService
    {
        private readonly IAmadeusAuthService _authService;
        private readonly IConfiguration _configuration;

        public FlightSearchService(IAmadeusAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<List<FlightOfferResponse>> SearchFlightsAsync(FlightSearchRequest searchRequest)
        {
            var token = await _authService.GetAccessTokenAsync();
            var baseUrl = _configuration["Amadeus:BaseUrl"];

            var client = new RestClient(baseUrl);
            var request = new RestRequest("/v2/shopping/flight-offers", Method.Get);

            request.AddHeader("Authorization", $"Bearer {token}");

            // Paramètres de recherche
            request.AddParameter("originLocationCode", searchRequest.OriginLocationCode);
            request.AddParameter("destinationLocationCode", searchRequest.DestinationLocationCode);
            request.AddParameter("departureDate", searchRequest.DepartureDate);

            if (!string.IsNullOrEmpty(searchRequest.ReturnDate))
            {
                request.AddParameter("returnDate", searchRequest.ReturnDate);
            }

            request.AddParameter("adults", searchRequest.Adults);

            if (searchRequest.Children > 0)
                request.AddParameter("children", searchRequest.Children);

            if (searchRequest.Infants > 0)
                request.AddParameter("infants", searchRequest.Infants);

            request.AddParameter("travelClass", searchRequest.TravelClass);
            request.AddParameter("currencyCode", searchRequest.CurrencyCode);
            request.AddParameter("nonStop", searchRequest.NonStop.ToString().ToLower());
            request.AddParameter("max", searchRequest.MaxResults);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Erreur de recherche de vols: {response.Content}");
            }

            return ParseFlightOffers(response.Content);
        }

        private List<FlightOfferResponse> ParseFlightOffers(string jsonContent)
        {
            var json = JObject.Parse(jsonContent);
            var data = json["data"];
            var flights = new List<FlightOfferResponse>();

            foreach (var offer in data)
            {
                var flight = new FlightOfferResponse
                {
                    Id = offer["id"]?.ToString(),
                    Price = decimal.Parse(offer["price"]?["total"]?.ToString() ?? "0"),
                    Currency = offer["price"]?["currency"]?.ToString(),
                    NumberOfBookableSeats = int.Parse(offer["numberOfBookableSeats"]?.ToString() ?? "0"),
                    ValidatingAirlineCodes = offer["validatingAirlineCodes"]?[0]?.ToString(),
                    Itineraries = new List<Itinerary>()
                };

                foreach (var itinerary in offer["itineraries"])
                {
                    var duration = itinerary["duration"]?.ToString();

                    var itineraryObj = new Itinerary
                    {
                        Duration = duration,
                        DurationInMinutes = ParseDuration(duration),
                        Segments = new List<Segment>()
                    };

                    foreach (var segment in itinerary["segments"])
                    {
                        itineraryObj.Segments.Add(new Segment
                        {
                            Departure = new Departure
                            {
                                IataCode = segment["departure"]?["iataCode"]?.ToString(),
                                Terminal = segment["departure"]?["terminal"]?.ToString(),
                                At = DateTime.Parse(segment["departure"]?["at"]?.ToString())
                            },
                            Arrival = new Arrival
                            {
                                IataCode = segment["arrival"]?["iataCode"]?.ToString(),
                                Terminal = segment["arrival"]?["terminal"]?.ToString(),
                                At = DateTime.Parse(segment["arrival"]?["at"]?.ToString())
                            },
                            CarrierCode = segment["carrierCode"]?.ToString(),
                            FlightNumber = segment["number"]?.ToString(),
                            Duration = segment["duration"]?.ToString(),
                            NumberOfStops = int.Parse(segment["numberOfStops"]?.ToString() ?? "0"),
                            Aircraft = new Aircraft
                            {
                                Code = segment["aircraft"]?["code"]?.ToString()
                            }
                        });
                    }

                    flight.Itineraries.Add(itineraryObj);
                }

                flights.Add(flight);
            }

            return flights;
        }

        private int ParseDuration(string duration)
        {
            // Convertir PT2H30M en minutes (150)
            if (string.IsNullOrEmpty(duration)) return 0;

            duration = duration.Replace("PT", "");
            int hours = 0, minutes = 0;

            if (duration.Contains("H"))
            {
                var parts = duration.Split('H');
                hours = int.Parse(parts[0]);
                duration = parts.Length > 1 ? parts[1] : "";
            }

            if (duration.Contains("M"))
            {
                minutes = int.Parse(duration.Replace("M", ""));
            }

            return (hours * 60) + minutes;
        }
    }
}