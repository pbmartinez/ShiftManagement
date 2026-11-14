using Domain.Dtos;

namespace Application.Abstractions
{
    public interface IExportShifts
    {
        byte[] ExportShiftsToFile(IEnumerable<ShiftDto> shifts);
    }
}
