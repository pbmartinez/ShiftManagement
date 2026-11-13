namespace Application.Abstractions
{
    public interface IHolidayDates
    {
        Task<List<DateOnly>> GetOfficialPlannedHolidaysAsync(int year);
        Task<List<DateOnly>> GetCustomHolidaysAsync(int year);
    }
}
