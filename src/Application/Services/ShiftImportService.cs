using System.ComponentModel.DataAnnotations;
using System.Data;
using Application.Abstractions;
using Domain.Dtos;
using Domain.IRepositories;

namespace Application.Services
{
    public class ShiftImportService(
        IShiftRepository shiftRepository,
        ICollaboratorRepository collaboratorRepository,
        IUnitOfWork unitOfWork) : IShiftImportService
    {
        public async Task<List<string>> ImportAsync(List<ShiftDto> shifts, CancellationToken cancellationToken = default)
        {
            if (shifts.Count == 0)
                return ["The file contains no shifts"];

            // Checks that need no database: run them first and skip the transaction if they fail.
            var errors = ValidateEachShift(shifts);
            errors.AddRange(FindOverlapsInsideFile(shifts));
            if (errors.Count > 0)
                return errors;

            // Serializable: the overlap check against the database and the inserts happen as one unit,
            // so two imports running at the same time can't both pass the check and save overlapping shifts.
            // SQL Server will make one of them fail with a deadlock error instead.
            return await unitOfWork.ExecuteInTransactionAsync(
                ct => CheckAgainstDatabaseAndSaveAsync(shifts, ct),
                IsolationLevel.Serializable,
                cancellationToken);
        }

        // Shifts from a file skip ASP.NET model validation, so run the DataAnnotations/IValidatableObject rules here.
        private static List<string> ValidateEachShift(List<ShiftDto> shifts)
        {
            var errors = new List<string>();
            foreach (var shift in shifts)
            {
                if (string.IsNullOrWhiteSpace(EmailOf(shift)))
                {
                    errors.Add($"{Describe(shift)}: collaborator email is missing");
                    continue;
                }

                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(shift, new ValidationContext(shift), results, validateAllProperties: true))
                    errors.AddRange(results.Select(r => $"{Describe(shift)}: {r.ErrorMessage}"));
            }
            return errors;
        }

        private static List<string> FindOverlapsInsideFile(List<ShiftDto> shifts)
        {
            var errors = new List<string>();
            var groups = shifts.GroupBy(s => (Email: EmailOf(s).ToLowerInvariant(), s.Date));

            foreach (var group in groups)
            {
                // Sorted by start time, a shift that overlaps any earlier one also overlaps the one right
                // before it, so comparing neighbours is enough.
                var ordered = group.OrderBy(s => s.StartTime).ToList();
                for (int i = 1; i < ordered.Count; i++)
                    if (ordered[i - 1].OverlapsWith(ordered[i]))
                        errors.Add($"{Describe(ordered[i])}: overlaps {TimeRange(ordered[i - 1])} in the same file");
            }
            return errors;
        }

        private async Task<List<string>> CheckAgainstDatabaseAndSaveAsync(List<ShiftDto> shifts, CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            // 1. Look up the existing collaborators by email (one query) instead of creating new ones.
            var emails = shifts.Select(EmailOf).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var collaborators = await collaboratorRepository.GetByEmailsAsync(emails, cancellationToken);

            var collaboratorIdByEmail = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var collaborator in collaborators)
                collaboratorIdByEmail.TryAdd(collaborator.Email, collaborator.Id);

            foreach (var email in emails.Where(e => !collaboratorIdByEmail.ContainsKey(e)))
                errors.Add($"No collaborator found with email {email}");
            if (errors.Count > 0)
                return errors;

            // Point each shift at the existing collaborator. Clearing the navigation stops EF from
            // inserting the CollaboratorDto that came from the file as a new collaborator.
            foreach (var shift in shifts)
            {
                shift.CollaboratorId = collaboratorIdByEmail[EmailOf(shift)];
                shift.Collaborator = null;
            }

            // 2. Load every existing shift for these collaborators and dates (one query), then check in memory.
            var existing = await shiftRepository.GetShiftsByCollaboratorsAndDateRangeAsync(
                collaboratorIdByEmail.Values.ToList(),
                shifts.Min(s => s.Date),
                shifts.Max(s => s.Date),
                cancellationToken);
            var existingByCollaboratorAndDate = existing.ToLookup(s => (s.CollaboratorId, s.Date));

            foreach (var shift in shifts)
                foreach (var conflict in existingByCollaboratorAndDate[(shift.CollaboratorId, shift.Date)].Where(shift.OverlapsWith))
                    errors.Add($"{Describe(shift)}: overlaps existing shift {TimeRange(conflict)}");
            if (errors.Count > 0)
                return errors;

            // 3. All checks passed: save everything.
            foreach (var shift in shifts)
            {
                shift.GenerateIdentity();
                await shiftRepository.AddAsync(shift, cancellationToken);
            }
            await shiftRepository.CommitChangesAsync(cancellationToken);

            return errors;
        }

        private static string EmailOf(ShiftDto shift) => shift.Collaborator?.Email?.Trim() ?? string.Empty;

        private static string TimeRange(ShiftDto shift) => $"{shift.StartTime:HH\\:mm}-{shift.EndTime:HH\\:mm}";

        private static string Describe(ShiftDto shift) => $"{EmailOf(shift)} {shift.Date:yyyy-MM-dd} {TimeRange(shift)}";
    }
}
