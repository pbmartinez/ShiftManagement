using Application.Abstractions;
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
        protected readonly IEntityWriter<TEntityDto> Writer;

        protected ApiBaseController(IRepository<TEntityDto> repository, IEntityWriter<TEntityDto> writer)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
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
            var errors = await Writer.AddAsync([item], cancellationToken);
            if (errors.Count > 0)
                return BusinessErrors(errors);
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
            item.Id = id;
            var errors = await Writer.UpdateAsync(item, cancellationToken);
            if (errors.Count > 0)
                return BusinessErrors(errors);
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
            var errors = await Writer.UpdateAsync(item, cancellationToken);
            if (errors.Count > 0)
                return BusinessErrors(errors);
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

        protected ActionResult BusinessErrors(List<string> errors)
        {
            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);
            return ValidationProblem(ModelState);
        }

        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options = HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>();
            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
        }
    }
}
