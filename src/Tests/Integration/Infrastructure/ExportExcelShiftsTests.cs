using Infrastructure.Application;

namespace Tests.Integration
{
    public class ExportExcelShiftsTests
    {
        [TestCase("ana@x.com|2026-11-13|09:00|17:30", "bob@x.com|2026-11-14|22:00|23:45", TestName = "export then import gives the same shifts")]
        [TestCase(TestName = "no shifts: only the header row")]
        public async Task ExportedFile_CanBeImportedBack(params string[] rows)
        {
            var bytes = new ExportExcelShifts().ExportShiftsToFile(rows.Select(r => TestData.Shift(r)));

            var shifts = await new LoadExcelShifts().LoadShiftsFromFile(TestData.FormFile(bytes));

            shifts.Select(TestData.Row).Should().Equal(rows);
        }
    }
}
