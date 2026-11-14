using System.Net;
using FluentAssertions.Execution;
using Infrastructure.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Tests.Unit
{
    public class HolidayDatesTests
    {
        // Stands in for Nager: answers with the given status and counts the calls.
        private class FakeNager(HttpStatusCode status) : HttpMessageHandler
        {
            public int Calls;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent("""[{ "date": "2026-01-01", "name": "New Year's Day", "localName": "Año Nuevo" }]""")
                });
            }
        }

        private static HolidayDates Create(FakeNager nager) => new(
            new HttpClient(nager) { BaseAddress = new Uri("https://nager.test/api/v3/") },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new HolidaySettings { CacheDuration = TimeSpan.FromDays(30) }));

        [Test]
        public async Task SameCountryAndYear_IsFetchedOnceThenCached()
        {
            var nager = new FakeNager(HttpStatusCode.OK);
            var holidayDates = Create(nager);

            await holidayDates.GetOfficialPlannedHolidaysAsync("UY", 2026);
            var holidays = await holidayDates.GetOfficialPlannedHolidaysAsync("UY", 2026);

            using (new AssertionScope())
            {
                holidays.Should().Equal(new HolidayDto(new DateOnly(2026, 1, 1), "New Year's Day"));
                nager.Calls.Should().Be(1);
            }
        }

        [Test]
        public async Task AFailure_ReturnsNoHolidaysAndIsNotCached()
        {
            var nager = new FakeNager(HttpStatusCode.ServiceUnavailable);
            var holidayDates = Create(nager);

            var holidays = await holidayDates.GetOfficialPlannedHolidaysAsync("UY", 2026);
            await holidayDates.GetOfficialPlannedHolidaysAsync("UY", 2026);

            using (new AssertionScope())
            {
                holidays.Should().BeEmpty();
                nager.Calls.Should().Be(2, "a failed answer must not be cached");
            }
        }
    }
}
