using Domain.Dtos;
using Domain.IRepositories;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace Api.Controllers
{
    [Route("api/collaborator")]
    public class CollaboratorController : ApiBaseController<CollaboratorDto>
    {
        public CollaboratorController(ICollaboratorRepository repository) : base(repository)
        {
        }
    }
}
