using Domain.IRepositories;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.WithDatabase
{
    [NonParallelizable]
    public class RepositoryTests
    {
        private IServiceScope _scope = null!;

        [SetUp]
        public async Task SetUp()
        {
            await TestDatabase.ResetAsync();
            await TestDatabase.AddCollaboratorsAsync("ana@x.com", "bob@x.com");
            await TestDatabase.AddShiftsAsync(
                "ana@x.com|2026-11-13|09:00|10:00",
                "ana@x.com|2026-11-14|09:00|10:00",
                "bob@x.com|2026-11-13|09:00|10:00");
            _scope = TestDatabase.Factory.Services.CreateScope();
        }

        [TearDown]
        public void TearDown() => _scope.Dispose();

        private T Get<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

        [TestCase("2026-11-13", "2026-11-13", 1)]
        [TestCase("2026-11-12", "2026-11-14", 2)]
        [TestCase("2026-11-15", "2026-11-20", 0)]
        public async Task GetShiftsByCollaboratorsAndDateRange_ReturnsOnlyThatCollaboratorInRange(string from, string to, int expectedCount)
        {
            var shifts = await Get<IShiftRepository>().GetShiftsByCollaboratorsAndDateRangeAsync(
                [TestData.IdOf("ana@x.com")], DateOnly.Parse(from), DateOnly.Parse(to));

            using (new AssertionScope())
            {
                shifts.Should().HaveCount(expectedCount);
                shifts.Should().NotContain(s => s.Collaborator == null || s.Collaborator.Email != "ana@x.com", "the collaborator must be loaded for error messages");
            }
        }

        [TestCase("ana@x.com")]
        [TestCase("ANA@X.COM")]
        public async Task GetByEmails_IgnoresCase(string email)
        {
            var collaborators = await Get<ICollaboratorRepository>().GetByEmailsAsync([email]);

            collaborators.Should().ContainSingle().Which.Id.Should().Be(TestData.IdOf("ana@x.com"));
        }
    }
}
