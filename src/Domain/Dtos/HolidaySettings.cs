namespace Domain.Dtos
{
    public class HolidaySettings
    {
        public const string SectionName = "HolidaySettings";

        public string NagerEndpoint { get; set; }
        public string LicenseKey { get; set; }
        public string CountryCode { get; set; }

    }
}
