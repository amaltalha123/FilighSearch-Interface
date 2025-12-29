using System.ComponentModel.DataAnnotations;

namespace FlightSearch.Api.Models.Requests
{
    public class FlightSearchRequest
    {
        [Required]
        public string OriginLocationCode { get; set; } // Ex: "CMN"

        [Required]
        public string DestinationLocationCode { get; set; } // Ex: "CDG"

        [Required]
        public string DepartureDate { get; set; } // Format: YYYY-MM-DD

        public string ReturnDate { get; set; } // Optionnel pour aller simple

        [Required]
        [Range(1, 9)]
        public int Adults { get; set; } = 1;

        public int Children { get; set; } = 0;

        public int Infants { get; set; } = 0;

        [Required]
        public string TravelClass { get; set; } = "ECONOMY"; // ECONOMY, PREMIUM_ECONOMY, BUSINESS, FIRST

        public string CurrencyCode { get; set; } = "EUR";

        public bool NonStop { get; set; } = false;

        public int MaxResults { get; set; } = 50;
    }
}