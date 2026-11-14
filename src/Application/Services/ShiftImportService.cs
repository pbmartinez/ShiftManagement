using Application.Abstractions;
using Domain.Dtos;
using Domain.IRepositories;

namespace Application.Services
{   
    public class ShiftImportService(
        ICollaboratorRepository collaboratorRepository,
        IEntityWriter<ShiftDto> writer) : IShiftImportService
    {
        public async Task<List<string>> ImportAsync(List<ShiftDto> shifts, CancellationToken cancellationToken = default)
        {
            if (shifts.Count == 0)
                return ["The file contains no shifts"];

            var errors = shifts
                .Where(s => string.IsNullOrWhiteSpace(EmailOf(s)))
                .Select(s => $"{s}: collaborator email is missing")
                .ToList();
            if (errors.Count > 0)
                return errors;

            var emails = shifts.Select(EmailOf).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var collaborators = await collaboratorRepository.GetByEmailsAsync(emails, cancellationToken);

            var collaboratorIdByEmail = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var collaborator in collaborators)
                collaboratorIdByEmail.TryAdd(collaborator.Email, collaborator.Id);

            errors.AddRange(emails
                .Where(e => !collaboratorIdByEmail.ContainsKey(e))
                .Select(e => $"No collaborator found with email {e}"));
            if (errors.Count > 0)
                return errors;

            foreach (var shift in shifts)
                shift.CollaboratorId = collaboratorIdByEmail[EmailOf(shift)];

            return await writer.AddAsync(shifts, cancellationToken);
        }

        private static string EmailOf(ShiftDto shift) => shift.Collaborator?.Email?.Trim() ?? string.Empty;
    }
}
