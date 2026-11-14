using System.ComponentModel.DataAnnotations;
using System.Data;
using Application.Abstractions;
using Application.Services;
using Domain.IRepositories;
using FluentAssertions.Execution;

namespace Tests.Unit
{
    public class EntityWriterTests
    {
        private Mock<IRepository<ShiftDto>> _repository = null!;
        private Mock<IBusinessValidator<ShiftDto>> _validator = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private EntityWriter<ShiftDto> _writer = null!;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IRepository<ShiftDto>>();
            _validator = new Mock<IBusinessValidator<ShiftDto>>();
            GivenValidatorErrors();

            // Run the operation the writer passes in, so the code inside the "transaction" really executes.
            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork
                .Setup(u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<List<ValidationResult>>>>(), It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
                .Returns((Func<CancellationToken, Task<List<ValidationResult>>> operation, IsolationLevel _, CancellationToken ct) => operation(ct));

            _writer = new EntityWriter<ShiftDto>(_repository.Object, [_validator.Object], _unitOfWork.Object);
        }

        private void GivenValidatorErrors(params string[] errors) =>
            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<IReadOnlyList<ShiftDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errors.Select(e => new ValidationResult(e)).ToList());

        private static ShiftDto[] Shifts(int count, string times = "09:00|10:00") =>
            Enumerable.Range(1, count).Select(day => TestData.Shift($"2026-11-{day:00}|{times}")).ToArray();

        [TestCase(1)]
        [TestCase(3)]
        public async Task Add_ValidItems_SavesAndCommitsOnceInASerializableTransaction(int count)
        {
            var errors = await _writer.AddAsync(Shifts(count));

            errors.Should().BeEmpty();
            _repository.Verify(r => r.AddAsync(It.IsAny<ShiftDto>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
            _repository.Verify(r => r.CommitChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<List<ValidationResult>>>>(), IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Update_ValidItem_UpdatesAndCommits()
        {
            var errors = await _writer.UpdateAsync(Shifts(1)[0]);

            errors.Should().BeEmpty();
            _repository.Verify(r => r.UpdateAsync(It.IsAny<ShiftDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _repository.Verify(r => r.CommitChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestCase(1, "EndTime must be after StartTime", TestName = "one item: plain message")]
        [TestCase(2, "ana@x.com 2026-11-01 10:00-09:00: EndTime must be after StartTime", TestName = "several items: message says which one")]
        public async Task InvalidDataAnnotations_ReturnErrorsBeforeOpeningATransaction(int count, string expectedFirstError)
        {
            var errors = await _writer.AddAsync(Shifts(count, "10:00|09:00"));

            using (new AssertionScope())
            {
                errors.Select(e => e.ErrorMessage).Should().HaveCount(count).And.StartWith(expectedFirstError);
                errors[0].MemberNames.Should().Equal("StartTime", "EndTime");
            }
            _unitOfWork.VerifyNoOtherCalls();
            _repository.VerifyNoOtherCalls();
        }

        [Test]
        public async Task BusinessRuleErrors_NothingIsSaved()
        {
            GivenValidatorErrors("rule broken");

            var errors = await _writer.AddAsync(Shifts(1));

            errors.Select(e => e.ErrorMessage).Should().Equal("rule broken");
            _repository.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Add_GeneratesAnIdForNewItems()
        {
            var shift = TestData.Shift("2026-11-13|09:00|10:00", id: Guid.Empty);

            await _writer.AddAsync([shift]);

            shift.Id.Should().NotBeEmpty();
        }
    }
}
