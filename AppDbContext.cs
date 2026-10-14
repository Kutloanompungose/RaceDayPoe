using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RouteInfo> RouteInfos => Set<RouteInfo>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    // Mirrors the Part 1 SQL script (table names, keys, FKs, unique constraints, cascade rules).
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(x => x.Email).IsUnique();
        });

        mb.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasOne(x => x.Organiser).WithMany()
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.EntryFee).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(x => x.Categories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<RouteInfo>(e =>
        {
            e.ToTable("RouteInfo");
            e.HasIndex(x => x.CategoryID).IsUnique();
            e.HasOne(x => x.Category).WithOne(x => x.Route)
                .HasForeignKey<RouteInfo>(x => x.CategoryID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany()
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasIndex(x => x.EnrolmentID).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CapturedByOrganiser).WithMany()
                .HasForeignKey(x => x.CapturedByOrganiserID).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
