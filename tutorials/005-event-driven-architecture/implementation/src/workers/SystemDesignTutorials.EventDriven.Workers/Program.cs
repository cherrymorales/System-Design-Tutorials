using MassTransit;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Core;
using SystemDesignTutorials.EventDriven.Workers;

var builder = Host.CreateApplicationBuilder(args);
var messagingOptions = builder.Configuration.GetSection("Messaging").Get<MessagingOptions>() ?? new MessagingOptions();

builder.Services.AddDbContext<EventDrivenDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5437;Database=eventdriven;Username=postgres;Password=postgres";

    if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddScoped<ProjectionService>();
builder.Services.AddSingleton<IDatabaseSeeder, NoOpSeeder>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);

    if (messagingOptions.UseInMemory)
    {
        x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        return;
    }

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(messagingOptions.Host, "/", host =>
        {
            host.Username(messagingOptions.Username);
            host.Password(messagingOptions.Password);
        });
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
await host.RunAsync();
