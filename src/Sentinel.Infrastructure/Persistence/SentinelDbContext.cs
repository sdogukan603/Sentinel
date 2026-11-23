
using Microsoft.EntityFrameworkCore;
using Sentinel.Core.Entities;
using Sentinel.Core.Enums;

namespace Sentinel.Infrastructure.Persistence;

public sealed class SentinelDbContext : DbContext
{
    public SentinelDbContext(DbContextOptions<SentinelDbContext> options)
        : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("jobs");

            entity.HasKey(job => job.Id);

            entity.Property(job => job.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(job => job.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(job => job.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(job => job.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(job => job.StartedAtUtc)
                .HasColumnName("started_at_utc")
                .HasColumnType("timestamp with time zone");

            entity.Property(job => job.CompletedAtUtc)
                .HasColumnName("completed_at_utc")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(job => job.Status);

            entity.HasIndex(job => job.CreatedAtUtc);
        });
    }
}
