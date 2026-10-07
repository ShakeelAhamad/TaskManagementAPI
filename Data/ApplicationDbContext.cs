using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }

        public DbSet<Role> Roles { get; set; } //New DbSet
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configure Users-Task relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.Tasks)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //Configure Role-Users relationship
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Cascade); //Don`t allow deleting role if users exit

            //Configure indexs for better performance
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name) //Role name must be unique
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token) 
                .IsUnique();

            //Seed Default roles
            modelBuilder.Entity<Role>().HasData(
                    new Role
                    {
                        Id = 1,
                        Name = "Admin",
                        Description = "Full system access - can manage users and all tasks",
                        CreatedAt = new DateTime(2026, 9, 12)
                    },
                    new Role
                    {
                        Id = 2,
                        Name = "Manager",
                        Description = "Can view all task but only modify on task",
                        CreatedAt = new DateTime(2026, 9, 12)
                    },
                    new Role
                    {
                        Id=3,
                        Name = "User",
                        Description = "Can only manage own tasks",
                        CreatedAt = new DateTime(2026, 9, 12)
                    }
                );


        }
    }
}
