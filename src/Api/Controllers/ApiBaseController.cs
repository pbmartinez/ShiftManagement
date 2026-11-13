using Domain.Dtos;
using Domain.IRepositories;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class ApiBaseController<TEntityDto> : ControllerBase
        where TEntityDto : DtoBase
    {
        protected readonly IRepository<TEntityDto> Repository;

        protected ApiBaseController(IRepository<TEntityDto> repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [HttpHead]
        public virtual async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            var items = await Repository.GetAllAsync(cancellationToken: cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id}")]
        [HttpHead("{id}")]
        public virtual async Task<ActionResult<TEntityDto>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == default)
                return BadRequest();
            var item = await Repository.GetAsync(id, cancellationToken: cancellationToken);

            if (item == null)
                return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Post(TEntityDto item, CancellationToken cancellationToken = default)
        {
            if (item.IsTransient())
                item.GenerateIdentity();
            await Repository.AddAsync(item, cancellationToken);
            await Repository.CommitChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Put(Guid id, TEntityDto item, CancellationToken cancellationToken = default)
        {
            if (id == default)
                return BadRequest();
            var itemTarget = await Repository.GetAsync(id, cancellationToken: cancellationToken);
            if (itemTarget == null)
                return NotFound();
            await Repository.UpdateAsync(item, cancellationToken);
            await Repository.CommitChangesAsync(cancellationToken);
            return NoContent();
        }
        [HttpPatch("{id}")]
        public virtual async Task<IActionResult> Patch(Guid id, JsonPatchDocument<TEntityDto> patchDocument, CancellationToken cancellationToken = default)
        {
            if (id == default)
                return BadRequest();
            var item = await Repository.GetAsync(id, cancellationToken: cancellationToken);
            if (item == null)
                return NotFound();
            //TODO: Check client errors vs server error response
            patchDocument.ApplyTo(item);
            if (!TryValidateModel(item))
                return ValidationProblem(ModelState);
            await Repository.UpdateAsync(item, cancellationToken);
            await Repository.CommitChangesAsync(cancellationToken);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == default)
                return NotFound();
            var item = await Repository.GetAsync(id, cancellationToken: cancellationToken);
            if (item == null)
                return NotFound();
            await Repository.DeleteAsync(id, cancellationToken);
            await Repository.CommitChangesAsync(cancellationToken);
            return NoContent();
        }

        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options = HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>();
            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
        }
    }
}
