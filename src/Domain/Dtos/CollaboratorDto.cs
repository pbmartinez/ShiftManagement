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
        [RegularExpression("^[A-Z]{2}$", ErrorMessage = "Country must be a 2-letter code, e.g. UY")]
        public string Country { get; set; }

        public ICollection<ShiftDto> Shifts { get; set; } = [];
    }
}
