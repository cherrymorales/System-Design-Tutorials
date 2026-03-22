using Microsoft.EntityFrameworkCore;

namespace SystemDesignTutorials.EventDriven.Core;

public sealed class EventDrivenDbContext(DbContextOptions<EventDrivenDbContext> options) : DbContext(options)
{
    public DbSet<AssetRecord> Assets => Set<AssetRecord>();
    public DbSet<AssetProjection> AssetProjections => Set<AssetProjection>();
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetRecord>(entity =>
        {
            entity.HasKey(x => x.AssetId);
            entity.HasIndex(x => x.AssetKey).IsUnique();
            entity.Property(x => x.AssetKey).HasMaxLength(200);
            entity.Property(x => x.Title).HasMaxLength(250);
            entity.Property(x => x.SubmittedBy).HasMaxLength(200);
            entity.Property(x => x.FailureReason).HasMaxLength(500);
        });

        modelBuilder.Entity<AssetProjection>(entity =>
        {
            entity.HasKey(x => x.AssetId);
            entity.HasIndex(x => x.AssetKey).IsUnique();
            entity.Property(x => x.AssetKey).HasMaxLength(200);
            entity.Property(x => x.Title).HasMaxLength(250);
            entity.Property(x => x.SubmittedBy).HasMaxLength(200);
            entity.Property(x => x.FailureReason).HasMaxLength(500);
        });

        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.HasKey(x => x.NotificationId);
            entity.HasIndex(x => new { x.AssetId, x.Message }).IsUnique();
            entity.Property(x => x.AssetTitle).HasMaxLength(250);
            entity.Property(x => x.Message).HasMaxLength(500);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.OutboxMessageId);
            entity.HasIndex(x => x.PublishedAt);
            entity.Property(x => x.Type).HasMaxLength(512);
            entity.Property(x => x.Payload).HasColumnType("text");
        });
    }
}
