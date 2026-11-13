
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class ShiftDto : DtoBase, IValidatableObject
    {
        public Guid CollaboratorId { get; set; }

        public CollaboratorDto? Collaborator { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        public bool OverlapsWith(ShiftDto other) =>
            Date == other.Date &&
            other.StartTime < EndTime &&
            StartTime < other.EndTime;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
                yield return new ValidationResult("EndTime must be after StartTime",[nameof(StartTime), nameof(EndTime)]);
        }
    }
}
