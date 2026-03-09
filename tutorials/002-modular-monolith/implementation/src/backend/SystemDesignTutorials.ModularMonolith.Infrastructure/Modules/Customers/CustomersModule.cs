using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Customers;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Customers;

public sealed class CustomersModule(ModularMonolithDbContext dbContext) : ICustomersModule
{
    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .OrderBy(x => x.Name)
            .Select(x => new CustomerDto(x.Id, x.AccountCode, x.Name, x.Status.ToString(), x.BillingContactName, x.BillingContactEmail, x.ShippingContactName, x.ShippingContactEmail, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .Where(x => x.Id == customerId)
            .Select(x => new CustomerDto(x.Id, x.AccountCode, x.Name, x.Status.ToString(), x.BillingContactName, x.BillingContactEmail, x.ShippingContactName, x.ShippingContactEmail, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await dbContext.Customers.AnyAsync(x => x.AccountCode == command.AccountCode, cancellationToken))
        {
            throw new InvalidOperationException("Customer account code already exists.");
        }

        var customer = new Customer(command.AccountCode, command.Name, command.BillingContactName, command.BillingContactEmail, command.ShippingContactName, command.ShippingContactEmail);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCustomerAsync(customer.Id, cancellationToken) ?? throw new InvalidOperationException("Customer was not persisted.");
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid customerId, UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer not found.");

        customer.Update(command.Name, command.BillingContactName, command.BillingContactEmail, command.ShippingContactName, command.ShippingContactEmail, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCustomerAsync(customerId, cancellationToken) ?? throw new InvalidOperationException("Customer update failed.");
    }

    public async Task<CustomerValidationDto> GetCustomerValidationAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer not found.");

        return new CustomerValidationDto(customer.Id, customer.IsActive, customer.Name, customer.AccountCode);
    }
}
