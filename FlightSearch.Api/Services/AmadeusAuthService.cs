using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RestSharp;

namespace FlightSearch.api.Services
{
    public interface IAmadeusAuthService
    {
        Task<string> GetAccessTokenAsync();
    }

    public class AmadeusAuthService : IAmadeusAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "AmadeusAccessToken";

        public AmadeusAuthService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Vérifier si le token est en cache
            if (_cache.TryGetValue(CacheKey, out string cachedToken))
            {
                return cachedToken;
            }

            // Sinon, obtenir un nouveau token
            var apiKey = _configuration["Amadeus:ApiKey"];
            var apiSecret = _configuration["Amadeus:ApiSecret"];
            var tokenUrl = _configuration["Amadeus:TokenUrl"];

            var client = new RestClient(tokenUrl);
            var request = new RestRequest("", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("client_id", apiKey);
            request.AddParameter("client_secret", apiSecret);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Erreur d'authentification Amadeus: {response.Content}");
            }

            var tokenResponse = JsonConvert.DeserializeObject<AmadeusTokenResponse>(response.Content);

            // Mettre en cache pour 25 minutes (le token expire après 30 min)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));

            _cache.Set(CacheKey, tokenResponse.AccessToken, cacheOptions);

            return tokenResponse.AccessToken;
        }
    }

    public class AmadeusTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}