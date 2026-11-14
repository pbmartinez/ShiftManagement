using Domain.Dtos;

namespace Application.Abstractions
{
    public interface IShiftImportService
    {
        Task<List<string>> ImportAsync(List<ShiftDto> shifts, CancellationToken cancellationToken = default);
    }
}
