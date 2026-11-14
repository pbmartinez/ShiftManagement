using Application.Abstractions;
using ClosedXML.Excel;
using Domain.Dtos;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Application
{
    public class LoadExcelShifts : ILoadShifts
    {
        public async Task<List<ShiftDto>> LoadShiftsFromFile(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            memoryStream.Position = 0;

            var extractedData = new List<ShiftDto>();

            using (var workbook = new XLWorkbook(memoryStream))
            {
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RangeUsed()!.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    extractedData.Add(new ShiftDto
                    {
                        Id = Guid.NewGuid(),
                        Collaborator = new CollaboratorDto() { Id = Guid.NewGuid(), Email = row.Cell(1).GetString() },
                        Date = DateOnly.FromDateTime(ReadDateTime(row.Cell(2))),
                        StartTime = TimeOnly.FromDateTime(ReadDateTime(row.Cell(3))),
                        EndTime = TimeOnly.FromDateTime(ReadDateTime(row.Cell(4)))
                    });
                }
            }
            return extractedData;
        }

        private static DateTime ReadDateTime(IXLCell cell) =>
            cell.Value.IsDateTime ? cell.GetDateTime() : DateTime.Parse(cell.GetString());
    }
}
