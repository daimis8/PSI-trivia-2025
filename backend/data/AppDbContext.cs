using System.Text.Json;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<UserStats> UserStats => Set<UserStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureQuizzes(modelBuilder);
        ConfigureQuizQuestions(modelBuilder);
        ConfigureUserStats(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(u => u.Password)
                .IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }

    private static void ConfigureQuizzes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(q => q.ID);
            entity.Property(q => q.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(q => q.Description)
                .HasMaxLength(2000);
            entity.Property(q => q.TimesPlayed);

            entity.HasOne(q => q.Creator)
                .WithMany()
                .HasForeignKey(q => q.CreatorID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(q => q.Questions)
                .WithOne(q => q.Quiz)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureQuizQuestions(ModelBuilder modelBuilder)
    {
        var comparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c == null ? new List<string>() : c.ToList()
        );

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.QuestionText)
                .IsRequired();
            entity.Property(q => q.CorrectOptionIndex)
                .IsRequired();
            entity.Property(q => q.TimeLimit)
                .IsRequired();
            entity.Property(q => q.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(comparer);
        });
    }

    private static void ConfigureUserStats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserStats>(entity =>
        {
            entity.HasKey(s => s.UserId);
            entity.Property(s => s.GamesPlayed);
            entity.Property(s => s.GamesWon);
            entity.Property(s => s.QuizzesCreated);
            entity.Property(s => s.QuizPlays);

            entity.HasOne(s => s.User)
                .WithOne(u => u.Stats)
                .HasForeignKey<UserStats>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
