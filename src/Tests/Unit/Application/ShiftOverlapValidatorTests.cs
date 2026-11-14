using System.ComponentModel.DataAnnotations;
using Application.Validators;
using Domain.IRepositories;

namespace Tests.Unit
{
    public class ShiftOverlapValidatorTests
    {
        private Mock<IShiftRepository> _repository = null!;
        private ShiftOverlapValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IShiftRepository>();
            _validator = new ShiftOverlapValidator(_repository.Object);
            GivenExistingShifts();
        }

        private void GivenExistingShifts(params ShiftDto[] shifts) =>
            _repository
                .Setup(r => r.GetShiftsByCollaboratorsAndDateRangeAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(shifts.ToList());

        private Task<List<ValidationResult>> Validate(params ShiftDto[] shifts) =>
            _validator.ValidateAsync(shifts, CancellationToken.None);

        [TestCase(1, "ana@x.com|2026-11-13|09:00|11:00", "ana@x.com|2026-11-13|10:00|12:00", TestName = "two overlapping shifts")]
        [TestCase(0, "ana@x.com|2026-11-13|09:00|10:00", "bob@x.com|2026-11-13|09:00|10:00", TestName = "same times, different collaborators")]
        [TestCase(0, "ana@x.com|2026-11-13|09:00|10:00", "ana@x.com|2026-11-13|10:00|11:00", TestName = "touching shifts")]
        [TestCase(0, "ana@x.com|2026-11-13|09:00|10:00", "ana@x.com|2026-11-14|09:00|10:00", TestName = "same times, different dates")]
        [TestCase(2, "ana@x.com|2026-11-13|09:00|12:00", "ana@x.com|2026-11-13|10:00|11:00", "ana@x.com|2026-11-13|11:00|12:00", TestName = "overlap hidden behind a shorter shift")]
        public async Task ReportsOverlapsInsideTheBatch(int expectedErrors, params string[] rows)
        {
            var errors = await Validate(rows.Select(r => TestData.Shift(r)).ToArray());

            // NotContain(not ...) instead of OnlyContain: OnlyContain fails on an empty list.
            errors.Should().HaveCount(expectedErrors)
                .And.NotContain(e => !e.ErrorMessage!.EndsWith("in the same request"));
        }

        [TestCase("ana@x.com", "09:30", "10:30", false, 1, TestName = "overlaps an existing shift")]
        [TestCase("ana@x.com", "10:00", "11:00", false, 0, TestName = "touching an existing shift is fine")]
        [TestCase("bob@x.com", "09:30", "10:30", false, 0, TestName = "another collaborator's shift is not a conflict")]
        [TestCase("ana@x.com", "09:30", "10:30", true, 0, TestName = "an update does not conflict with its own old version")]
        public async Task ReportsOverlapsWithExistingShifts(string email, string start, string end, bool sameId, int expectedErrors)
        {
            var existing = TestData.Shift("ana@x.com|2026-11-13|09:00|10:00");
            GivenExistingShifts(existing);
            var incoming = TestData.Shift($"{email}|2026-11-13|{start}|{end}", sameId ? existing.Id : null);

            var errors = await Validate(incoming);

            errors.Select(e => e.ErrorMessage).Should().Equal(Enumerable.Repeat($"{incoming}: overlaps existing shift {existing}", expectedErrors));
            // Keyed to the time fields, so the UI can show it under Start and End.
            errors.Should().NotContain(e => !e.MemberNames.SequenceEqual(new[] { "StartTime", "EndTime" }));
        }

        [Test]
        public async Task EmptyBatch_DoesNotQueryTheDatabase()
        {
            var errors = await Validate();

            errors.Should().BeEmpty();
            _repository.VerifyNoOtherCalls();
        }
    }
}
