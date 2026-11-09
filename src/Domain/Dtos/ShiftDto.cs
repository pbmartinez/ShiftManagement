using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class ShiftDto : DtoBase
    {
        public Guid CollaboratorId { get; set; }

        public Collaborator Collaborator { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }
    }
}
