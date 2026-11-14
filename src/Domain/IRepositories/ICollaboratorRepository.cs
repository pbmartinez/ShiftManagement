using Domain.Dtos;

namespace Domain.IRepositories
{
    public interface ICollaboratorRepository : IRepository<CollaboratorDto>
    {
        Task<List<CollaboratorDto>> GetByEmailsAsync(IReadOnlyCollection<string> emails, CancellationToken cancellationToken = default);
    }
}
