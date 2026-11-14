using Application.Abstractions;
using Domain.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Infrastructure.Application
{
    /// <summary>
    /// Holidays from the public Nager.Date API. The HttpClient comes configured from Program.cs
    /// (base address + Polly retries), and every answer is kept in memory, as it rarely changes.
    /// </summary>
    public class HolidayDates : IHolidayDates
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly HolidaySettings _settings;

        public HolidayDates(HttpClient httpClient, IMemoryCache cache, IOptions<HolidaySettings> options)
        {
            _httpClient = httpClient;
            _cache = cache;
            _settings = options.Value;
        }

        public Task<List<HolidayDto>> GetOfficialPlannedHolidaysAsync(string countryCode, int year) =>
            CachedAsync<HolidayDto>($"holidays:{countryCode}:{year}", $"PublicHolidays/{year}/{countryCode}");

        public Task<List<CountryDto>> GetCountriesAsync() =>
            CachedAsync<CountryDto>("countries", "AvailableCountries");

        public async Task<List<DateOnly>> GetCustomHolidaysAsync(int year)
        {
            return [];
        }

        private async Task<List<T>> CachedAsync<T>(string key, string url)
        {
            if (_cache.TryGetValue(key, out List<T>? cached))
                return cached!;
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return [];
                var result = await response.Content.ReadFromJsonAsync<List<T>>() ?? [];
                return _cache.Set(key, result, _settings.CacheDuration);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Nager is down even after the retries: no holidays this time, and nothing cached so the next request tries again.
                return [];
            }
        }
    }
}
