using FlightSearch.Api.Models;
using FlightSearch.Api.Models.Responses;
using Microsoft.VisualBasic.FileIO;

namespace FlightSearch.Api.Services
{
    //  Remplace l'autocomplete Amadeus par une recherche locale sur airports.csv
    public class LocalAirportSearchService : IAirportSearchService
    {
        private readonly IWebHostEnvironment _env;

        // Cache mémoire (chargé 1 seule fois)
        private static List<AirportRecord>? _cache;
        private static readonly object _lock = new();

        public LocalAirportSearchService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public Task<List<AirportResponse>> SearchAirportsAsync(string keyword)
        {
            EnsureLoaded();

            var q = (keyword ?? "").Trim();
            if (q.Length < 2) return Task.FromResult(new List<AirportResponse>());

            var qUpper = q.ToUpperInvariant();
            const int defaultLimit = 10;
            int limit = defaultLimit;

            // 1) Résultats StartsWith (meilleur pour autocomplete)
            var starts = _cache!
                .Select(a => new { a, score = StartsWithScore(a, qUpper) })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Select(x => ToResponse(x.a))
                .ToList();

            // 2) Si pas assez de résultats, compléter avec Contains (moins prioritaire)
            if (starts.Count < limit)
            {
                var startIatas = new HashSet<string>(starts.Select(s => s.IataCode ?? ""));

                var contains = _cache!
                    .Select(a => new { a, score = ContainsScore(a, qUpper) })
                    .Where(x => x.score > 0)
                    .OrderByDescending(x => x.score)
                    .Select(x => ToResponse(x.a))
                    .Where(r => !startIatas.Contains(r.IataCode ?? ""))
                    .ToList();

                starts.AddRange(contains);
            }

            // 3) Dé-doublonnage + limiter
            var result = starts
                .Where(x => !string.IsNullOrWhiteSpace(x.IataCode))
                .GroupBy(x => x.IataCode)
                .Select(g => g.First())
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        // ---------- SCORING ----------

        private static int StartsWithScore(AirportRecord a, string qUpper)
        {
            if (string.IsNullOrWhiteSpace(a.IataCode)) return 0;

            var iata = a.IataCode.ToUpperInvariant();
            var city = (a.City ?? "").ToUpperInvariant();
            var name = (a.Name ?? "").ToUpperInvariant();

            // exact IATA (ex: CMN)
            if (iata == qUpper) return 2000;

            int score = 0;

            // StartsWith
            if (city.StartsWith(qUpper)) score += 800;
            if (name.StartsWith(qUpper)) score += 650;
            if (iata.StartsWith(qUpper)) score += 600;

            // Bonus léger Maroc (ne doit pas dominer)
            if (a.CountryCode.Equals("MA", StringComparison.OrdinalIgnoreCase)) score += 20;

            return score;
        }

        private static int ContainsScore(AirportRecord a, string qUpper)
        {
            if (string.IsNullOrWhiteSpace(a.IataCode)) return 0;

            var city = (a.City ?? "").ToUpperInvariant();
            var name = (a.Name ?? "").ToUpperInvariant();

            int score = 0;

            if (city.Contains(qUpper)) score += 250;
            if (name.Contains(qUpper)) score += 200;

            if (a.CountryCode.Equals("MA", StringComparison.OrdinalIgnoreCase)) score += 10;

            return score;
        }

        private static AirportResponse ToResponse(AirportRecord a) => new AirportResponse
        {
            IataCode = a.IataCode,
            Name = a.Name,
            CityName = a.City,
            CountryCode = a.CountryCode,
            //  CountryName : si tu veux "Morocco" au lieu de "MA", on ajoutera countries.csv
            CountryName = a.CountryCode
        };

        // ---------- CSV LOADER ----------

        private void EnsureLoaded()
        {
            if (_cache != null) return;

            lock (_lock)
            {
                if (_cache != null) return;

                var path = Path.Combine(_env.ContentRootPath, "Data", "airports.csv");
                if (!File.Exists(path))
                    throw new FileNotFoundException("airports.csv introuvable: " + path);

                _cache = LoadCsv(path);
            }
        }

        private static List<AirportRecord> LoadCsv(string path)
        {
            var list = new List<AirportRecord>();

            using var parser = new TextFieldParser(path);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;

            // Skip header
            if (!parser.EndOfData) parser.ReadFields();

            while (!parser.EndOfData)
            {
                var f = parser.ReadFields();
                if (f == null || f.Length < 14) continue;

                // OurAirports columns:
                // name[3], iso_country[8], municipality(city)[10], iata_code[13]
                var name = f[3] ?? "";
                var country = f[8] ?? "";
                var city = f[10] ?? "";
                var iata = f[13] ?? "";

                if (string.IsNullOrWhiteSpace(iata)) continue;

                list.Add(new AirportRecord
                {
                    IataCode = iata.Trim().ToUpperInvariant(),
                    Name = name.Trim(),
                    City = city.Trim(),
                    CountryCode = country.Trim().ToUpperInvariant()
                });
            }

            // Enlever doublons par IATA
            return list
                .GroupBy(x => x.IataCode)
                .Select(g => g.First())
                .ToList();
        }
    }
}
