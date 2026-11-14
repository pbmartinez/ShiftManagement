using Application.Abstractions;
using Domain.Dtos;
using Domain.IRepositories;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace Api.Controllers
{
    [Route("api/Shift")]
    public class ShiftController : ApiBaseController<ShiftDto>
    {
        private readonly ILoadShifts _loadShifts;
        private readonly IShiftImportService _shiftImportService;

        public ShiftController(
            IShiftRepository repository,
            IEntityWriter<ShiftDto> writer,
            ILoadShifts loadShifts,
            IShiftImportService shiftImportService)
            : base(repository, writer)
        {
            _loadShifts = loadShifts;
            _shiftImportService = shiftImportService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
                return BadRequest("A non-empty Excel file is required");

            var shifts = await _loadShifts.LoadShiftsFromFile(file);
            var errors = await _shiftImportService.ImportAsync(shifts, cancellationToken);

            return errors.Count == 0
                ? Ok(new { imported = shifts.Count })
                : BusinessErrors(errors);
        }
    }
}
