using Domain.Dtos;

namespace Domain.IRepositories
{
    public interface IShiftRepository : IRepository<ShiftDto>
    {
        Task<List<ShiftDto>> GetShiftsByCollaboratorAndDate(string email, DateOnly date);

        Task<List<ShiftDto>> GetShiftsByCollaboratorsAndDateRangeAsync(
            IReadOnlyCollection<Guid> collaboratorIds,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default);
    }
}
