using System.Collections.Concurrent;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace Tests
{
    /// <summary>
    /// Rows are written as "email|date|start|end" (or "date|start|end" for ana@x.com),
    /// the same shape as a row in the Excel file, so a [TestCase] stays one readable line.
    /// </summary>
    public static class TestData
    {
        public const string Ana = "ana@x.com";

        private static readonly ConcurrentDictionary<string, Guid> CollaboratorIds = new();

        // Same email -> same id in every test, so shifts and seeded collaborators line up.
        public static Guid IdOf(string email) => CollaboratorIds.GetOrAdd(email, _ => Guid.NewGuid());

        public static ShiftDto Shift(string row, Guid? id = null)
        {
            var parts = row.Split('|');
            if (parts.Length == 3)
                parts = [Ana, .. parts];

            return new ShiftDto
            {
                Id = id ?? Guid.NewGuid(),
                CollaboratorId = IdOf(parts[0]),
                Collaborator = Collaborator(parts[0]),
                Date = DateOnly.Parse(parts[1]),
                StartTime = TimeOnly.Parse(parts[2]),
                EndTime = TimeOnly.Parse(parts[3])
            };
        }

        public static CollaboratorDto Collaborator(string email) => new()
        {
            Id = IdOf(email),
            Name = email.Split('@')[0],
            Email = email,
            Country = "UY"
        };

        /// <summary>
        /// Builds an .xlsx with a header row and one row per shift.
        /// cellKind: "Text"      -> every cell is text ("2026-11-13", "09:00")
        ///           "ExcelTime" -> real date cell, times as Excel stores a typed "09:00" (a number formatted h:mm)
        ///           "TimeSpan"  -> real date cell, times as duration values
        /// </summary>
        public static byte[] ExcelBytes(string cellKind, params string[] rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("Shifts");
            sheet.Cell(1, 1).Value = "Email";
            sheet.Cell(1, 2).Value = "Date";
            sheet.Cell(1, 3).Value = "Start";
            sheet.Cell(1, 4).Value = "End";

            for (int i = 0; i < rows.Length; i++)
            {
                var parts = rows[i].Split('|');
                var row = sheet.Row(i + 2);
                row.Cell(1).Value = parts[0];
                if (cellKind == "Text")
                {
                    row.Cell(2).Value = parts[1];
                    row.Cell(3).Value = parts[2];
                    row.Cell(4).Value = parts[3];
                    continue;
                }

                row.Cell(2).Value = DateTime.Parse(parts[1]);
                for (int column = 3; column <= 4; column++)
                {
                    var time = TimeSpan.Parse(parts[column - 1]);
                    if (cellKind == "TimeSpan")
                    {
                        row.Cell(column).Value = time;
                    }
                    else
                    {
                        row.Cell(column).Value = time.TotalDays;
                        row.Cell(column).Style.NumberFormat.NumberFormatId = 20; // built-in "h:mm"
                    }
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static IFormFile ExcelFile(string cellKind, params string[] rows)
        {
            var bytes = ExcelBytes(cellKind, rows);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "shifts.xlsx");
        }
    }
}
