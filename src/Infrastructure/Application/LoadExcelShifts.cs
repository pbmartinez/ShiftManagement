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
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    extractedData.Add(new ShiftDto
                    {
                        Id = Guid.NewGuid(),
                        Collaborator = new CollaboratorDto() { Id = Guid.NewGuid(), Email = row.Cell(1).GetValue<string>() },
                        Date = row.Cell(2).GetValue<DateOnly>(),
                        StartTime = row.Cell(3).GetValue<TimeOnly>(),
                        EndTime = row.Cell(4).GetValue<TimeOnly>()
                    });
                }
            }
            return extractedData;
        }

    }
}
