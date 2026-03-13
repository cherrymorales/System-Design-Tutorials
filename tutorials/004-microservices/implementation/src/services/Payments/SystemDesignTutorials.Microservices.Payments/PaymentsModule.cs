using MassTransit;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Payments;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.HasIndex(payment => payment.OrderId).IsUnique();
            entity.Property(payment => payment.AuthorizationReference).HasMaxLength(120);
            entity.Property(payment => payment.FailureReason).HasMaxLength(500);
        });
    }
}

public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string AuthorizationReference { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PaymentsSeeder : IDatabaseSeeder<PaymentsDbContext>
{
    public Task SeedAsync(PaymentsDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PaymentsService(PaymentsDbContext dbContext)
{
    public async Task<object?> GetPaymentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments.AsNoTracking()
            .Where(payment => payment.OrderId == orderId)
            .Select(payment => new
            {
                payment.OrderId,
                payment.Status,
                payment.Amount,
                payment.AuthorizationReference,
                payment.FailureReason,
                payment.UpdatedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task HandleOrderSubmittedAsync(OrderSubmittedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var existingPayment = await dbContext.Payments.SingleOrDefaultAsync(payment => payment.OrderId == message.OrderId, cancellationToken);
        if (existingPayment is not null)
        {
            return;
        }

        var shouldFail = message.CustomerReference.Contains("FAIL-PAYMENT", StringComparison.OrdinalIgnoreCase)
            || message.TotalAmount > 4_000m;

        var payment = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.TotalAmount,
            Status = shouldFail ? PaymentStatus.Failed : PaymentStatus.Authorized,
            AuthorizationReference = shouldFail ? string.Empty : $"AUTH-{message.OrderNumber}",
            FailureReason = shouldFail ? "Payment authorization scenario failed." : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (shouldFail)
        {
            await publishEndpoint.Publish(
                new PaymentAuthorizationFailedIntegrationEvent(message.OrderId, payment.Amount, payment.FailureReason!, payment.UpdatedAt),
                cancellationToken);
        }
        else
        {
            await publishEndpoint.Publish(
                new PaymentAuthorizedIntegrationEvent(message.OrderId, payment.Id, payment.Amount, payment.AuthorizationReference, payment.UpdatedAt),
                cancellationToken);
        }
    }

    public async Task HandleVoidRequestedAsync(PaymentVoidRequestedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Authorized)
        {
            return;
        }

        payment.Status = PaymentStatus.Voided;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(
            new PaymentVoidedIntegrationEvent(message.OrderId, payment.Id, payment.UpdatedAt),
            cancellationToken);
    }
}
