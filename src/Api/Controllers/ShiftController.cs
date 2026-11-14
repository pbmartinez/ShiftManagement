using Application.Abstractions;
using Domain.Dtos;
using Domain.IRepositories;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace Api.Controllers
{
    [Route("api/shift")]
    public class ShiftController : ApiBaseController<ShiftDto>
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly ILoadShifts _loadShifts;
        private readonly IShiftImportService _shiftImportService;
        private readonly IExportShifts _exportShifts;

        public ShiftController(
            IShiftRepository repository,
            IEntityWriter<ShiftDto> writer,
            ILoadShifts loadShifts,
            IShiftImportService shiftImportService,
            IExportShifts exportShifts)
            : base(repository, writer)
        {
            _loadShifts = loadShifts;
            _shiftImportService = shiftImportService;
            _exportShifts = exportShifts;
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

        [HttpGet("export")]
        public async Task<IActionResult> Export(CancellationToken cancellationToken = default)
        {
            var shifts = await Repository.GetAllAsync(
                includes: ["Collaborator"],
                order: new() { ["Date"] = true, ["StartTime"] = true },
                cancellationToken: cancellationToken);

            return File(_exportShifts.ExportShiftsToFile(shifts), ExcelContentType, "shifts.xlsx");
        }
    }
}
