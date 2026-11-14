using Application.Abstractions;
using Application.Services;
using Domain.IRepositories;

namespace Tests.Unit
{
    public class ShiftImportServiceTests
    {
        private Mock<ICollaboratorRepository> _collaborators = null!;
        private Mock<IEntityWriter<ShiftDto>> _writer = null!;
        private ShiftImportService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _collaborators = new Mock<ICollaboratorRepository>();
            _collaborators
                .Setup(r => r.GetByEmailsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([TestData.Collaborator(TestData.Ana)]);

            _writer = new Mock<IEntityWriter<ShiftDto>>();
            _writer
                .Setup(w => w.AddAsync(It.IsAny<IReadOnlyList<ShiftDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _service = new ShiftImportService(_collaborators.Object, _writer.Object);
        }

        [TestCase("The file contains no shifts")]
        [TestCase("collaborator email is missing", "|2026-11-13|09:00|10:00")]
        [TestCase("No collaborator found with email bob@x.com", "ana@x.com|2026-11-13|09:00|10:00", "bob@x.com|2026-11-13|09:00|10:00")]
        public async Task InvalidFile_ReturnsErrorWithoutSaving(string expectedError, params string[] rows)
        {
            var errors = await _service.ImportAsync(rows.Select(r => TestData.Shift(r)).ToList());

            errors.Should().ContainSingle().Which.Should().Contain(expectedError);
            _writer.VerifyNoOtherCalls();
        }

        [TestCase("ana@x.com")]
        [TestCase("ANA@X.COM")]
        [TestCase("  ana@x.com  ")]
        public async Task Email_IsResolvedToTheExistingCollaborator(string emailInFile)
        {
            var shifts = new List<ShiftDto>
            {
                TestData.Shift($"{emailInFile}|2026-11-13|09:00|10:00"),
                TestData.Shift($"{emailInFile}|2026-11-14|09:00|10:00")
            };

            var errors = await _service.ImportAsync(shifts);

            errors.Should().BeEmpty();
            _writer.Verify(w => w.AddAsync(
                It.Is<IReadOnlyList<ShiftDto>>(saved => saved.Count == 2 && saved.All(s => s.CollaboratorId == TestData.IdOf(TestData.Ana))),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
