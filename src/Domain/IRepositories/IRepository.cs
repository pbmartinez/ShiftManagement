using Domain.Dtos;
using System.Linq.Expressions;

namespace Domain.IRepositories
{
    public interface IRepository<TDto> where TDto : DtoBase
    {
        TDto? Get(Guid id, List<string>? includes = null);
        Task<List<TDto>> GetAllAsync(List<string>? includes = null, Dictionary<string, bool>? order = null, CancellationToken cancellationToken = default);
        Task<TDto?> GetAsync(Guid id, List<string>? includes = null, CancellationToken cancellationToken = default);
        Task AddAsync(TDto item, CancellationToken cancellationToken = default);
        Task UpdateAsync(TDto item, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<int> CommitChangesAsync(CancellationToken cancellationToken = default);

        Task<List<TDto>> GetByQueryAsync(List<string>? includes = null, Dictionary<string, bool>? order = null, Expression<Func<TDto, bool>>? predicate = null, CancellationToken cancellationToke = default);
        

    }
}
