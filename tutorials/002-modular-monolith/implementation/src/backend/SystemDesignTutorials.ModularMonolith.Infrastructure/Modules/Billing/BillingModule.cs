using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Billing;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Billing;

public sealed class BillingModule(ModularMonolithDbContext dbContext) : IBillingModule
{
    public async Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken)
    {
        return await ProjectInvoicesAsync(dbContext.Invoices.AsQueryable(), cancellationToken);
    }

    public async Task<InvoiceDto> CreateDraftAsync(CreateInvoiceDraftCommand command, string actor, CancellationToken cancellationToken)
    {
        if (await dbContext.Invoices.AnyAsync(x => x.OrderId == command.OrderId, cancellationToken))
        {
            throw new InvalidOperationException("An invoice already exists for this order.");
        }

        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{await dbContext.Invoices.CountAsync(cancellationToken) + 1:D4}";
        var invoice = new Invoice(command.OrderId, command.CustomerId, invoiceNumber, command.TotalAmount, actor);
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await ProjectInvoicesAsync(dbContext.Invoices.Where(x => x.Id == invoice.Id), cancellationToken)).Single();
    }

    public async Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string actor, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");

        invoice.Issue(actor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await ProjectInvoicesAsync(dbContext.Invoices.Where(x => x.Id == invoiceId), cancellationToken)).Single();
    }

    public async Task<InvoiceDto> MarkPaidAsync(Guid invoiceId, string actor, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");

        invoice.MarkPaid(actor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await ProjectInvoicesAsync(dbContext.Invoices.Where(x => x.Id == invoiceId), cancellationToken)).Single();
    }

    public async Task<InvoiceStatusSnapshotDto?> GetInvoiceStatusAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return await dbContext.Invoices
            .Where(x => x.Id == invoiceId)
            .Select(x => new InvoiceStatusSnapshotDto(x.Id, x.Status.ToString(), x.Status == Domain.Enums.InvoiceStatus.Paid))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<InvoiceDto>> ProjectInvoicesAsync(IQueryable<Invoice> query, CancellationToken cancellationToken)
    {
        return (await (
                from invoice in query
                join customer in dbContext.Customers on invoice.CustomerId equals customer.Id
                select new InvoiceDto(
                    invoice.Id,
                    invoice.OrderId,
                    invoice.CustomerId,
                    customer.Name,
                    invoice.InvoiceNumber,
                    invoice.Status.ToString(),
                    invoice.TotalAmount,
                    invoice.CreatedBy,
                    invoice.CreatedAt,
                    invoice.IssuedAt,
                    invoice.IssuedBy,
                    invoice.PaidAt,
                    invoice.PaidBy))
            .ToListAsync(cancellationToken)).OrderByDescending(x => x.CreatedAt).ToList();
    }
}
