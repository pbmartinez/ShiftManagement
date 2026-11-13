
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class ShiftDto : DtoBase
    {
        public Guid CollaboratorId { get; set; }

        public CollaboratorDto? Collaborator { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        public bool IntersectsWith(ShiftDto anotherShift)
        {
            if (CollaboratorId != anotherShift.CollaboratorId || Date != anotherShift.Date)
                return false;
            return (anotherShift.StartTime >= StartTime && anotherShift.StartTime <= EndTime) ||
                (anotherShift.EndTime >= StartTime && anotherShift.EndTime <= EndTime);
        }
    }
}
