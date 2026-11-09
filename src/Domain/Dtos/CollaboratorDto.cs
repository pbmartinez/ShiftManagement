using System.ComponentModel.DataAnnotations;

namespace Domain.Dtos
{
    public class CollaboratorDto : DtoBase
    {
        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Country { get; set; }

        public ICollection<ShiftDto> Shifts { get; set; } = [];
    }
}
