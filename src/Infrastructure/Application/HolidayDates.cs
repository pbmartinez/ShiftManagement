using Application.Abstractions;
using Domain.Dtos;
using Microsoft.Extensions.Options;
using Nager.Holiday;
using System.Net.Http.Json;

namespace Infrastructure.Application
{
    public class HolidayDates : IHolidayDates
    {
        private readonly HolidaySettings _settings;

        public HolidayDates(IOptions<HolidaySettings> options)
        {
            _settings = options.Value;
        }

        public async Task<List<DateOnly>> GetOfficialPlannedHolidaysAsync(int year)
        {
            using var httpClient = new HttpClient();
            var holidays = await httpClient.GetFromJsonAsync<PublicHoliday[]>($"{_settings.NagerEndpoint}/{_settings.CountryCode}/{year}");
            var holidayDates = new List<DateOnly>();
            foreach (var holiday in holidays ?? [])
            {
                //
            }

            return holidayDates;
        }


        public async Task<List<DateOnly>> GetCustomHolidaysAsync(int year)
        {
            return [];
        }
    }
}
