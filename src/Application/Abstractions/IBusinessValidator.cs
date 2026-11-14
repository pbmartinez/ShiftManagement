using System.ComponentModel.DataAnnotations;
using Domain.Dtos;

namespace Application.Abstractions
{    
    public interface IBusinessValidator<TDto> where TDto : DtoBase
    {
        Task<List<ValidationResult>> ValidateAsync(IReadOnlyList<TDto> items, CancellationToken cancellationToken);
    }
}
