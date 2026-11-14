using Application.Abstractions;
using Domain.Dtos;
using Domain.IRepositories;

namespace Application.Validators
{
    public class ShiftOverlapValidator(IShiftRepository shiftRepository) : IBusinessValidator<ShiftDto>
    {
        public async Task<List<string>> ValidateAsync(IReadOnlyList<ShiftDto> shifts, CancellationToken cancellationToken)
        {
            if (shifts.Count == 0)
                return [];

            var errors = FindOverlapsInsideBatch(shifts);

            var existing = await shiftRepository.GetShiftsByCollaboratorsAndDateRangeAsync(
                shifts.Select(s => s.CollaboratorId).Distinct().ToList(),
                shifts.Min(s => s.Date),
                shifts.Max(s => s.Date),
                cancellationToken);

            var batchIds = shifts.Select(s => s.Id).ToHashSet();
            var existingByCollaboratorAndDate = existing
                .Where(e => !batchIds.Contains(e.Id))
                .ToLookup(e => (e.CollaboratorId, e.Date));

            foreach (var shift in shifts)
                foreach (var conflict in existingByCollaboratorAndDate[(shift.CollaboratorId, shift.Date)].Where(shift.OverlapsWith))
                    errors.Add($"{shift}: overlaps existing shift {conflict}");

            return errors;
        }

        private static List<string> FindOverlapsInsideBatch(IReadOnlyList<ShiftDto> shifts)
        {
            var errors = new List<string>();
            foreach (var group in shifts.GroupBy(s => (s.CollaboratorId, s.Date)))
            {
                var ordered = group.OrderBy(s => s.StartTime).ToList();
                // Compare with the latest-ending shift so far, not just the previous one:
                // 09-12, 10-11, 11-12 -> 11-12 overlaps 09-12 even though it only touches 10-11.
                var latestEnding = ordered[0];
                for (int i = 1; i < ordered.Count; i++)
                {
                    if (latestEnding.OverlapsWith(ordered[i]))
                        errors.Add($"{ordered[i]}: overlaps {latestEnding} in the same request");
                    if (ordered[i].EndTime > latestEnding.EndTime)
                        latestEnding = ordered[i];
                }
            }
            return errors;
        }
    }
}
