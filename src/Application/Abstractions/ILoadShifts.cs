using Domain.Dtos;
using Microsoft.AspNetCore.Http;

namespace Application.Abstractions
{
    public interface ILoadShifts
    {
        Task<List<ShiftDto>> LoadShiftsFromFile(IFormFile file);
    }
}
