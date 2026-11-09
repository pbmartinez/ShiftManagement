using AutoMapper;
using Domain.Dtos;
using Domain.Entities;
using Domain.IRepositories;
using Infrastructure.Domain.UnitOfWork;

namespace Infrastructure.Domain.Repositories
{
    public class CollaboratorRepository : Repository<CollaboratorDto, Collaborator>, ICollaboratorRepository
    {
        public CollaboratorRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
