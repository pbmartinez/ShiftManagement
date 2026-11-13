using Domain.Dtos;

namespace Domain.IRepositories
{
    public interface IShiftRepository : IRepository<ShiftDto>
    {
        Task<List<ShiftDto>> GetShiftsByCollaboratorAndDate(string email, DateOnly date);
    }
}
