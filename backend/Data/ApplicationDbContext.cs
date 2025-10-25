using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Password)
                .IsRequired();

            // Create unique indexes
            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasIndex(e => e.Username)
                .IsUnique();
        });

        // Configure Quiz entity
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.ID);

            entity.Property(e => e.ID)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CreatorID)
                .IsRequired();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000);

            // Store Questions as JSON
            entity.Property(e => e.Questions)
                .HasColumnType("jsonb")
                .IsRequired();

            // Create index on CreatorID for efficient filtering
            entity.HasIndex(e => e.CreatorID);
        });
    }
}
