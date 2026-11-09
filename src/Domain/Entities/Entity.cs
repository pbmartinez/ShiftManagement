using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Entity
    {
        [Key]
        public required Guid Id { get; set; }
        public virtual bool IsTransient() => Id == Guid.Empty;

        public virtual void GenerateIdentity()
        {
            if (IsTransient())
                Id = Guid.NewGuid();
        }
    }
}
