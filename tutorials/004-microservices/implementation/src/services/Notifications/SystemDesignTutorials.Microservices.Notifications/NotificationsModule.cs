using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Notifications;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.HasIndex(notification => notification.DeduplicationKey).IsUnique();
            entity.Property(notification => notification.Category).HasMaxLength(120);
            entity.Property(notification => notification.Message).HasMaxLength(500);
            entity.Property(notification => notification.DeduplicationKey).HasMaxLength(200);
        });
    }
}

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string DeduplicationKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class NotificationsSeeder : IDatabaseSeeder<NotificationsDbContext>
{
    public Task SeedAsync(NotificationsDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class NotificationsService(NotificationsDbContext dbContext)
{
    public async Task<IReadOnlyCollection<NotificationDto>> GetNotificationsAsync(Guid? orderId, CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications.AsNoTracking().AsQueryable();
        if (orderId is not null)
        {
            query = query.Where(notification => notification.OrderId == orderId.Value);
        }

        return await query.OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.OrderId,
                notification.Category,
                notification.Message,
                notification.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task RecordAsync(NotificationRequestedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var deduplicationKey = $"{message.OrderId}:{message.Category}:{message.Message}";
        if (await dbContext.Notifications.AnyAsync(notification => notification.DeduplicationKey == deduplicationKey, cancellationToken))
        {
            return;
        }

        dbContext.Notifications.Add(new NotificationRecord
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Category = message.Category,
            Message = message.Message,
            DeduplicationKey = deduplicationKey,
            CreatedAt = message.RequestedAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
