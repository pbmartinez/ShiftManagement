using System.ComponentModel.DataAnnotations;
using Domain.Dtos;

namespace Application.Abstractions
{
    public interface IShiftImportService
    {
        Task<List<ValidationResult>> ImportAsync(List<ShiftDto> shifts, CancellationToken cancellationToken = default);
    }
}
