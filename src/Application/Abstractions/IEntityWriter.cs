using System.ComponentModel.DataAnnotations;
using Domain.Dtos;

namespace Application.Abstractions
{    
    public interface IEntityWriter<TDto> where TDto : DtoBase
    {
        Task<List<ValidationResult>> AddAsync(IReadOnlyList<TDto> items, CancellationToken cancellationToken = default);
        Task<List<ValidationResult>> UpdateAsync(TDto item, CancellationToken cancellationToken = default);
    }
}
