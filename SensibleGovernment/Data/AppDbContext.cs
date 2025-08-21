namespace SensibleGovernment.Data;

using Microsoft.EntityFrameworkCore;
using SensibleGovernment.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<PostSource> PostSources => Set<PostSource>();
    public DbSet<UserReport> UserReports => Set<UserReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Post relationships
        modelBuilder.Entity<Post>()
            .HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment relationships
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Like relationships
        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // PostSource relationships
        modelBuilder.Entity<PostSource>()
            .HasOne(ps => ps.Post)
            .WithMany(p => p.Sources)
            .HasForeignKey(ps => ps.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserReport relationships
        modelBuilder.Entity<UserReport>()
            .HasOne(ur => ur.ReportingUser)
            .WithMany(u => u.ReportsSubmitted)
            .HasForeignKey(ur => ur.ReportingUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserReport>()
            .HasOne(ur => ur.ReportedUser)
            .WithMany(u => u.ReportsAgainst)
            .HasForeignKey(ur => ur.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserReport>()
            .HasOne(ur => ur.Comment)
            .WithMany()
            .HasForeignKey(ur => ur.CommentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}