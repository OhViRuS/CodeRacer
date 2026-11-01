using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Data;

public class CodeRacerDbContext(DbContextOptions<CodeRacerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<CodeSnippet> CodeSnippets => Set<CodeSnippet>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.UserName).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.UserName).IsRequired().HasMaxLength(50);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(320);
            entity.Property(user => user.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<CodeSnippet>(entity =>
        {
            entity.HasKey(snippet => snippet.Id);
            entity.Property(snippet => snippet.CodeText).IsRequired();
            entity.Property(snippet => snippet.ProgrammingConcept).IsRequired().HasMaxLength(100);
            entity.Property(snippet => snippet.Language).HasConversion<string>().HasMaxLength(50);
            entity.Property(snippet => snippet.Difficulty).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.HasKey(result => result.Id);
            entity.Property(result => result.CompletionTime).IsRequired();
            entity.Property(result => result.TypingSpeed).IsRequired();
            entity.Property(result => result.Accuracy).IsRequired();
            entity.Property(result => result.CreatedAt).IsRequired();

            entity.HasOne(result => result.User)
                .WithMany(user => user.RaceResults)
                .HasForeignKey(result => result.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(result => result.CodeSnippet)
                .WithMany(snippet => snippet.RaceResults)
                .HasForeignKey(result => result.CodeSnippetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(result => new { result.UserId, result.CreatedAt });
        });
    }
}
