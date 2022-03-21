using Microsoft.EntityFrameworkCore;

namespace PasswordsAPI.Models
{
    public class PasswordsDbContext : DbContext
    {
        public PasswordsDbContext( DbContextOptions<PasswordsDbContext> options )
            : base( options )
        {
            
        }

        public virtual DbSet<PasswordUsers> PasswordUsers { get; set; }
        public virtual DbSet<UserPasswords> UserPasswords { get; set; }
        public virtual DbSet<UserLocations> UserLocations { get; set; }
    }
}
