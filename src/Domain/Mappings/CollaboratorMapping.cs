using AutoMapper;
using Domain.Dtos;
using Domain.Entities;

namespace Domain.Mappings
{
    public class CollaboratorMapping : Profile
    {
        public CollaboratorMapping()
        {
            CreateMap<Collaborator, CollaboratorDto>()
                .ForMember(g => g.Shifts, options => options.MapFrom(f => f.Shifts))
                .ReverseMap();
        }
    }
}
