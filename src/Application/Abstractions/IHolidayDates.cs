using Domain.Dtos;

namespace Application.Abstractions
{
    public interface IHolidayDates
    {
        Task<List<HolidayDto>> GetOfficialPlannedHolidaysAsync(string countryCode, int year);
        Task<List<CountryDto>> GetCountriesAsync();
        Task<List<DateOnly>> GetCustomHolidaysAsync(int year);
    }
}
