using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Collaborator : Entity
    {
        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Country { get; set; }

        public ICollection<Shift> Shifts { get; set; } = [];
    }
}
