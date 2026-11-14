namespace Domain.Dtos
{
    /// <summary>A public holiday, as Nager.Date returns it (only the fields we use).</summary>
    public record HolidayDto(DateOnly Date, string Name);
}
