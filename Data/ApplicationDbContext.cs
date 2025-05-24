using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PC3.Models;

namespace PC3.Data;

public class ApplicationDbContext : IdentityDbContext
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
  {
  }

  public DbSet<Feedback> Feedbacks { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);


    modelBuilder.Entity<Feedback>(entity =>
    {

      entity.HasIndex(f => new { f.userId, f.PostId })
                .IsUnique()
                .HasDatabaseName("IX_Feedback_UserId_PostId");


    });
  }
}