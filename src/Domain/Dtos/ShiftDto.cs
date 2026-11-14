
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

        /// <summary>Output only: the holiday's name when the shift falls on one in the collaborator's country.</summary>
        public string? Holiday { get; set; }

        public bool OverlapsWith(ShiftDto other) =>
            Date == other.Date &&
            other.StartTime < EndTime &&
            StartTime < other.EndTime;

        public override string ToString() =>
            $"{Collaborator?.Email ?? CollaboratorId.ToString()} {Date:yyyy-MM-dd} {StartTime:HH\\:mm}-{EndTime:HH\\:mm}";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
                yield return new ValidationResult("EndTime must be after StartTime",[nameof(StartTime), nameof(EndTime)]);
        }
    }
}
