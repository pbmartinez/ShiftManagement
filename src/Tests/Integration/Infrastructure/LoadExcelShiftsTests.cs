using Infrastructure.Application;

namespace Tests.Integration
{
    public class LoadExcelShiftsTests
    {
        [TestCase("Text", TestName = "cells typed as text")]
        [TestCase("ExcelTime", TestName = "real date cell and times typed in Excel (h:mm)")]
        [TestCase("TimeSpan", TestName = "real date cell and duration time cells")]
        public async Task ReadsShiftsFromEveryCellFormat(string cellKind)
        {
            string[] rows = ["ana@x.com|2026-11-13|09:00|17:30", "bob@x.com|2026-11-14|22:00|23:45"];

            var shifts = await new LoadExcelShifts().LoadShiftsFromFile(TestData.ExcelFile(cellKind, rows));

            // Header row skipped, and every value comes back as it was written.
            shifts.Select(TestData.Row).Should().Equal(rows);
        }
    }
}
