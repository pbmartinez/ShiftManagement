using Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.WithDatabase
{
    /// <summary>Runs the real API in memory (no port), pointed at the test container's database.</summary>
    public class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            // No calls to the real Nager API: every country has New Year's Day as its only holiday.
            builder.ConfigureTestServices(services => services.AddSingleton(HolidayDatesStub()));
        }

        private static IHolidayDates HolidayDatesStub()
        {
            var holidays = new Mock<IHolidayDates>();
            holidays.Setup(h => h.GetOfficialPlannedHolidaysAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((string _, int year) => [new HolidayDto(new DateOnly(year, 1, 1), "New Year's Day")]);
            holidays.Setup(h => h.GetCountriesAsync()).ReturnsAsync([new CountryDto("UY", "Uruguay")]);
            return holidays.Object;
        }
    }
}
