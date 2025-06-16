using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersService.Consumers;
using OrdersService.Data;
using OrdersService.Services;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// ──────────────── DI ────────────────
builder.Services.AddDbContext<OrdersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit + RabbitMQ + консьюмер
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentResultConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("payments.result", e =>
        {
            e.ConfigureConsumer<PaymentResultConsumer>(context);
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });
    });
});

builder.Services.AddHostedService<OutboxPublisherService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ──────────────── build ────────────────
var app = builder.Build();

// миграции с retry (BD может быть не готова)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            logger.LogInformation("Applying migrations {Attempt}/{Max}", attempt, maxAttempts);
            db.Database.Migrate();
            logger.LogInformation("Database migrated successfully.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Migration failed, retrying in 5 s…");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

// ──────────────── HTTP-pipeline ────────────────
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
