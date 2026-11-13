using AutoMapper;
using Domain.Dtos;
using Domain.Entities;
using Domain.IRepositories;
using Infrastructure.Domain.UnitOfWork;

namespace Infrastructure.Domain.Repositories
{
    public class ShiftRepository : Repository<ShiftDto, Shift>, IShiftRepository
    {
        public ShiftRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        public async Task<List<ShiftDto>> GetShiftsByCollaboratorAndDate(string email, DateOnly date)
        {
            return await GetByQueryAsync(["Collaborator"], null, (a) => a.Collaborator.Email == email && a.Date == date);
        }
    }
}
