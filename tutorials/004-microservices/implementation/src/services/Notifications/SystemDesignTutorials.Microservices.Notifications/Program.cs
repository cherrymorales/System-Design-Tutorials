using MassTransit;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<NotificationsDbContext>(builder.Configuration, "Notifications");
builder.Services.AddScoped<NotificationsSeeder>();
builder.Services.AddScoped<NotificationsService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<NotificationsDbContext, NotificationsSeeder>>();

builder.Services.AddMassTransit(registration =>
{
    registration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notifications", false));
    registration.AddConsumer<NotificationRequestedConsumer>();
    registration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "notifications" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);
internalApi.MapGet("/notifications", async (Guid? orderId, NotificationsService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetNotificationsAsync(orderId, cancellationToken)));

app.Run();

public partial class Program;
