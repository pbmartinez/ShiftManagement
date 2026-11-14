using System.ComponentModel.DataAnnotations;
using FluentAssertions.Execution;

namespace Tests.Unit
{
    public class ShiftDtoTests
    {
        [TestCase("2026-11-13|09:00|11:00", "2026-11-13|10:00|12:00", true, TestName = "partial overlap")]
        [TestCase("2026-11-13|09:00|17:00", "2026-11-13|10:00|11:00", true, TestName = "one shift inside the other")]
        [TestCase("2026-11-13|09:00|10:00", "2026-11-13|09:00|10:00", true, TestName = "identical shifts")]
        [TestCase("2026-11-13|09:00|10:00", "2026-11-13|10:00|11:00", false, TestName = "touching edges do not overlap")]
        [TestCase("2026-11-13|09:00|10:00", "2026-11-14|09:00|10:00", false, TestName = "same times on another date")]
        public void OverlapsWith(string first, string second, bool expected)
        {
            var a = TestData.Shift(first);
            var b = TestData.Shift(second);

            using (new AssertionScope())
            {
                a.OverlapsWith(b).Should().Be(expected);
                b.OverlapsWith(a).Should().Be(expected, "overlapping must not depend on the order");
            }
        }

        [TestCase("09:00", "10:00", true)]
        [TestCase("10:00", "10:00", false)]
        [TestCase("11:00", "10:00", false)]
        [TestCase("22:00", "06:00", false, TestName = "overnight shift is rejected (current rule)")]
        public void Validate_EndMustBeAfterStart(string start, string end, bool expectedValid)
        {
            var shift = TestData.Shift($"2026-11-13|{start}|{end}");
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(shift, new ValidationContext(shift), results, validateAllProperties: true);

            using (new AssertionScope())
            {
                isValid.Should().Be(expectedValid);
                results.Should().HaveCount(expectedValid ? 0 : 1);
            }
        }

        [TestCase(true, TestName = "ToString uses the collaborator email")]
        [TestCase(false, TestName = "ToString falls back to the CollaboratorId")]
        public void ToString_DescribesWhoAndWhen(bool withCollaborator)
        {
            var shift = TestData.Shift("ana@x.com|2026-11-13|09:00|10:00");
            if (!withCollaborator)
                shift.Collaborator = null;

            var who = withCollaborator ? "ana@x.com" : shift.CollaboratorId.ToString();
            shift.ToString().Should().Be($"{who} 2026-11-13 09:00-10:00");
        }
    }
}
