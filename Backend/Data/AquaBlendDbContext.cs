using Microsoft.EntityFrameworkCore;
using AquaBlend.Entities;

namespace AquaBlend.Data;

public class AquaBlendDbContext : DbContext
{
    public AquaBlendDbContext(DbContextOptions<AquaBlendDbContext> options) : base(options)
    {
    }

    public DbSet<WaterSource> WaterSources => Set<WaterSource>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<OptimisationResult> OptimisationResults => Set<OptimisationResult>();
    public DbSet<OptimisationRun> OptimisationRuns => Set<OptimisationRun>();

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var currentTime = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Metadata.FindProperty("CreatedAt") is not null)
                {
                    entry.Property("CreatedAt").CurrentValue = currentTime;
                }

                if (entry.Metadata.FindProperty("UpdatedAt") is not null)
                {
                    entry.Property("UpdatedAt").CurrentValue = currentTime;
                }
            }

            if (entry.State == EntityState.Modified &&
                entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = currentTime;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OptimisationResult>()
            .Property(r => r.ResultJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<OptimisationResult>()
            .Property(r => r.TotalCost)
            .HasColumnType("numeric(18,2)");

        modelBuilder.Entity<OptimisationResult>()
            .HasIndex(r => r.ScenarioId);

        modelBuilder.Entity<OptimisationResult>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<OptimisationResult>()
            .HasIndex(r => r.SolvedAt);

        modelBuilder.Entity<OptimisationResult>()
            .HasIndex(r => r.RunId)
            .IsUnique();

        modelBuilder.Entity<Scenario>()
            .HasIndex(s => s.ExternalId)
            .IsUnique();

        modelBuilder.Entity<OptimisationResult>()
            .HasOne(r => r.Scenario)
            .WithMany(s => s.OptimisationResults)
            .HasForeignKey(r => r.ScenarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OptimisationResult>()
            .HasOne(res => res.Run)
            .WithOne(run => run.Result)
            .HasForeignKey<OptimisationResult>(res => res.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OptimisationRun>()
            .Property(r => r.ScenarioSnapshotJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<OptimisationRun>()
            .HasIndex(r => new { r.ScenarioId, r.CreatedAt });

        modelBuilder.Entity<OptimisationRun>()
            .HasOne(r => r.Scenario)
            .WithMany(s => s.OptimisationRuns)
            .HasForeignKey(r => r.ScenarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}