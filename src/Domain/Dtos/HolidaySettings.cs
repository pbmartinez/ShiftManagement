namespace Domain.Dtos
{
    public class HolidaySettings
    {
        public const string SectionName = "HolidaySettings";

        public string NagerEndpoint { get; set; }

        /// <summary>How long a Nager response (holidays of a country/year, the country list) stays in memory.</summary>
        public TimeSpan CacheDuration { get; set; }
    }
}
