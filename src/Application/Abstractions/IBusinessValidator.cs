using Domain.Dtos;

namespace Application.Abstractions
{    
    public interface IBusinessValidator<TDto> where TDto : DtoBase
    {
        Task<List<string>> ValidateAsync(IReadOnlyList<TDto> items, CancellationToken cancellationToken);
    }
}
