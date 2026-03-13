using MassTransit;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Payments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<PaymentsDbContext>(builder.Configuration, "Payments");
builder.Services.AddScoped<PaymentsSeeder>();
builder.Services.AddScoped<PaymentsService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<PaymentsDbContext, PaymentsSeeder>>();

builder.Services.AddMassTransit(registration =>
{
    registration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payments", false));
    registration.AddConsumer<OrderSubmittedConsumer>();
    registration.AddConsumer<PaymentVoidRequestedConsumer>();
    registration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "payments" }));

app.MapGroup("/internal").RequireInternalApi(app.Configuration)
    .MapGet("/payments/{orderId:guid}", async (Guid orderId, PaymentsService service, CancellationToken cancellationToken) =>
    {
        var payment = await service.GetPaymentAsync(orderId, cancellationToken);
        return payment is null ? Results.NotFound() : Results.Ok(payment);
    });

app.Run();

public partial class Program;
