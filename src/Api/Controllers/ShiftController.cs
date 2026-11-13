using Domain.Dtos;
using Domain.IRepositories;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace Api.Controllers
{
    [Route("api/Shift")]
    public class ShiftController : ApiBaseController<ShiftDto>
    {
        public ShiftController(IShiftRepository repository) : base(repository)
        {
        }
    }
}
