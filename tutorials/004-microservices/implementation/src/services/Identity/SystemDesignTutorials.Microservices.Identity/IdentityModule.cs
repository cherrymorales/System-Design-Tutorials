using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Identity;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(200);
            entity.Property(user => user.DisplayName).HasMaxLength(200);
            entity.Property(user => user.Role).HasMaxLength(120);
        });
    }
}

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed class IdentitySeeder(IPasswordHasher<UserAccount> passwordHasher)
    : IDatabaseSeeder<IdentityDbContext>
{
    public async Task SeedAsync(IdentityDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var users = new[]
        {
            Create("catalog@microservices.local", "Catalog Manager", ServiceRoles.CatalogManager),
            Create("orders@microservices.local", "Order Operations Agent", ServiceRoles.OrderOpsAgent),
            Create("inventory@microservices.local", "Inventory Coordinator", ServiceRoles.InventoryCoordinator),
            Create("finance@microservices.local", "Finance Reviewer", ServiceRoles.FinanceReviewer),
            Create("fulfillment@microservices.local", "Fulfillment Operator", ServiceRoles.FulfillmentOperator),
            Create("manager@microservices.local", "Operations Manager", ServiceRoles.OperationsManager),
        };

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private UserAccount Create(string email, string displayName, string role)
    {
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = role,
        };

        user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
        return user;
    }
}

public sealed class IdentityService(IdentityDbContext dbContext, IPasswordHasher<UserAccount> passwordHasher)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == request.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (user is null)
        {
            return new LoginResponse(false, null, "Invalid credentials.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new LoginResponse(false, null, "Invalid credentials.");
        }

        return new LoginResponse(
            true,
            new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.Role),
            null);
    }

    public async Task<IReadOnlyCollection<SeedUserDto>> GetSeedUsersAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .OrderBy(user => user.Role)
            .Select(user => new SeedUserDto(user.Email, user.DisplayName, user.Role))
            .ToListAsync(cancellationToken);
    }
}
