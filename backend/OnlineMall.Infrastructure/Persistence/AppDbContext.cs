using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineMall.Domain.Entities;
using OnlineMall.Infrastructure.Identity;

namespace OnlineMall.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options) 
        { 
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all configurations from this assembly
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Rename Identity tables to be more meaningful
            builder.Entity<AppUser>().ToTable("Users");
        }
    }
}
