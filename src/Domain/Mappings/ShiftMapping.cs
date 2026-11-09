using AutoMapper;
using Domain.Dtos;
using Domain.Entities;

namespace Domain.Mappings
{
    public class ShiftMapping : Profile
    {
        public ShiftMapping()
        {
            CreateMap<Shift, ShiftDto>()
                .ForMember(g => g.Collaborator, options => options.MapFrom(f => f.Collaborator))
                .ReverseMap();
        }
    }
}
