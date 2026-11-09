using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Domain.UnitOfWork
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Collaborator> Collaborators { get; set; }
        public DbSet<Shift> Shifts { get; set; }
    }
}
