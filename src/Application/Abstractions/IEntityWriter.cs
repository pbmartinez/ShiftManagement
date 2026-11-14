using Domain.Dtos;

namespace Application.Abstractions
{    
    public interface IEntityWriter<TDto> where TDto : DtoBase
    {
        Task<List<string>> AddAsync(IReadOnlyList<TDto> items, CancellationToken cancellationToken = default);
        Task<List<string>> UpdateAsync(TDto item, CancellationToken cancellationToken = default);
    }
}
