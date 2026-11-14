using Application.Abstractions;
using ClosedXML.Excel;
using Domain.Dtos;

namespace Infrastructure.Application
{
    public class ExportExcelShifts : IExportShifts
    {
        public byte[] ExportShiftsToFile(IEnumerable<ShiftDto> shifts)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Shifts");

            worksheet.Cell(1, 1).Value = "Email";
            worksheet.Cell(1, 2).Value = "Date";
            worksheet.Cell(1, 3).Value = "Start";
            worksheet.Cell(1, 4).Value = "End";
            worksheet.Row(1).Style.Font.Bold = true;

            var row = 2;
            foreach (var shift in shifts)
            {
                worksheet.Cell(row, 1).Value = shift.Collaborator?.Email ?? string.Empty;
                worksheet.Cell(row, 2).Value = shift.Date.ToDateTime(TimeOnly.MinValue);
                worksheet.Cell(row, 3).Value = shift.StartTime.ToTimeSpan();
                worksheet.Cell(row, 4).Value = shift.EndTime.ToTimeSpan();
                row++;
            }

            worksheet.Column(2).Style.DateFormat.Format = "yyyy-mm-dd";
            worksheet.Columns(3, 4).Style.DateFormat.Format = "hh:mm";
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
